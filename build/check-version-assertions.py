#!/usr/bin/env python3
"""Assert the version tests compare the native read against the generated constants.

`GHOSTTY_VT_VERSION_MAJOR/MINOR/PATCH` are generated from the pinned headers.
The tests exist to prove the loaded native library and the bindings come from
the same upstream commit. A grep for the constant name proves none of that:

* `Assert.True(GHOSTTY_VT_VERSION_MAJOR >= 0)` mentions the constant and
  asserts nothing about the library.
* `Assert.True(true); // GHOSTTY_VT_VERSION_MAJOR` keeps the name on an
  `Assert.` line inside a comment.
* `Assert.Equal("1" + "." + "1" + "." + "0", ...)` re-couples the test to a
  literal, which is the #92 failure mode spelled so a whole-literal pattern
  misses it.

So the checks are structural. Comments are removed first, then each assertion
statement is read as an argument list:

1. Each of MAJOR, MINOR, PATCH must appear as the direct first argument of an
   `Assert.Equal(...)` whose other argument is a value expression (an
   identifier or member access), never a literal or another constant.
2. No `Assert.True`/`Assert.False` may contain a version constant.
3. No version-shaped literal may be assembled from string concatenation,
   interpolation, `string.Concat`, `string.Join`, or `new Version(...)`.

Usage: check-version-assertions.py [test-root]
Exit 0 when every requirement holds; 1 with one line per failure.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

CONSTANTS = ("MAJOR", "MINOR", "PATCH")
VERSION_CONST = re.compile(r"\bGhosttyVtNative\.GHOSTTY_VT_VERSION_(MAJOR|MINOR|PATCH)\b")
DIRECT_CONST = re.compile(
    r"^(?:\(\s*nuint\s*\)\s*)?GhosttyVtNative\.GHOSTTY_VT_VERSION_(MAJOR|MINOR|PATCH)$"
)
VALUE_EXPR = re.compile(r"^[A-Za-z_][\w.]*$")
VERSION_SHAPE = re.compile(r"\d+\.\d+\.\d+")
# Only assertions that are about the library version are checked for a
# literal re-coupling. `Assert.Contains("PowerShell 7.5.5", rows[0])` is not a
# version test, and treating every digit-dot-digit literal as one would make
# the gate reject ordinary tests.
VERSION_CONTEXT = re.compile(
    r"GHOSTTY_VT_VERSION|VersionString|Version(?:Major|Minor|Patch)|"
    r"ghostty_version|ghostty_build_info|\bexpected\b|\bversion\b|"
    r"\b(?:major|minor|patch)\b",
    re.IGNORECASE,
)
STRING_LITERAL = re.compile(r'\$?@?"(?:[^"\\]|\\.)*"')


def strip_comments(text: str) -> str:
    """Remove // and /* */ comments without touching string or char literals."""
    out: list[str] = []
    index = 0
    length = len(text)
    string_quote: str | None = None
    verbatim = False
    while index < length:
        char = text[index]
        if string_quote is not None:
            out.append(char)
            if verbatim and char == '"' and index + 1 < length and text[index + 1] == '"':
                out.append('"')
                index += 2
                continue
            if not verbatim and char == "\\" and index + 1 < length:
                out.append(text[index + 1])
                index += 2
                continue
            if char == string_quote:
                string_quote = None
            index += 1
            continue
        if char == '"':
            string_quote = '"'
            verbatim = index > 0 and text[index - 1] == "@"
            out.append(char)
            index += 1
            continue
        if char == "'":
            # Char literal. Scan to the closing quote; escaped quotes are rare
            # in these tests but handled for completeness.
            out.append(char)
            index += 1
            while index < length:
                out.append(text[index])
                if text[index] == "\\" and index + 1 < length:
                    out.append(text[index + 1])
                    index += 2
                    continue
                if text[index] == "'":
                    index += 1
                    break
                index += 1
            continue
        if text.startswith("//", index):
            while index < length and text[index] != "\n":
                index += 1
            continue
        if text.startswith("/*", index):
            end = text.find("*/", index + 2)
            index = length if end == -1 else end + 2
            continue
        out.append(char)
        index += 1
    return "".join(out)


def split_arguments(statement: str) -> list[str] | None:
    """Return the top-level arguments of `Assert.X(...)`, or None."""
    statement = statement.rstrip().rstrip(";").rstrip()
    open_paren = statement.find("(")
    if open_paren == -1 or not statement.endswith(")"):
        return None
    body = statement[open_paren + 1 :].rstrip()
    body = body[: body.rfind(")")]
    args: list[str] = []
    depth = 0
    current = ""
    for char in body:
        if char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        if char == "," and depth == 0:
            args.append(current.strip())
            current = ""
            continue
        current += char
    args.append(current.strip())
    return args


def statement_at(text: str, start: int) -> str:
    """Read an assertion statement from `start` to its closing semicolon."""
    depth = 0
    index = start
    while index < len(text):
        char = text[index]
        if char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        elif char == ";" and depth <= 0:
            return text[start : index + 1]
        index += 1
    return text[start:]


def literal_value(expression: str) -> str | None:
    """Concatenate a `+` chain of string literals, or return None."""
    pieces = STRING_LITERAL.findall(expression)
    if not pieces:
        return None
    remainder = STRING_LITERAL.sub("", expression)
    if re.search(r"[^+\s()]", remainder):
        return None
    return "".join(
        piece[piece.index('"') + 1 : piece.rindex('"')] for piece in pieces
    )


def interpolated_version_literal(expression: str) -> str | None:
    """Return an interpolated string that embeds literal version components."""
    for literal in re.findall(r'\$"(?:[^"\\]|\\.)*"', expression):
        without_expressions = re.sub(r"\{[^}]*\}", "", literal)
        if re.search(r"\d+\s*\.|\.\s*\d+", without_expressions):
            return literal
    return None


def version_literal(statement: str) -> str | None:
    """Return the version-shaped literal an assertion compares, if any."""
    if "new Version(" in statement:
        return "new Version(...)"
    if "string.Concat(" in statement:
        return "string.Concat(...)"
    if "string.Join(" in statement:
        return "string.Join(...)"
    function = statement[: statement.find("(")]
    if function.strip() in ("Assert.Equal", "Assert.StartsWith", "Assert.Contains"):
        args = split_arguments(statement) or []
        for argument in args:
            joined = literal_value(argument)
            if joined is not None and VERSION_SHAPE.search(joined):
                return joined
            interpolated = interpolated_version_literal(argument)
            if interpolated is not None:
                return interpolated
            if STRING_LITERAL.fullmatch(argument.strip() or ""):
                text = argument.strip()
                if VERSION_SHAPE.search(text[text.index('"') + 1 : text.rindex('"')]):
                    return text
    return None


def check_file(path: Path) -> tuple[list[str], set[str]]:
    raw = path.read_text(encoding="utf-8")
    source = strip_comments(raw)
    problems: list[str] = []
    compared: set[str] = set()

    for match in re.finditer(
        r"\bAssert\.(Equal|True|False|StartsWith|Contains)\s*\(", source
    ):
        statement = statement_at(source, match.start())
        function = match.group(1)

        literal = version_literal(statement)
        if literal is not None and literal != "0.0.0" and VERSION_CONTEXT.search(statement):
            problems.append(
                f"{path}: assertion re-couples to a version literal {literal!r}: "
                f"{statement.strip()}"
            )

        constants = VERSION_CONST.findall(statement)
        if not constants:
            continue

        if function in ("True", "False"):
            problems.append(
                f"{path}: `Assert.{function}` asserts a version constant without "
                f"comparing it to the library: {statement.strip()}"
            )
            continue
        if function != "Equal":
            continue

        args = split_arguments(statement) or []
        direct = [DIRECT_CONST.match(argument) for argument in args]
        matched = [item.group(1) for item in direct if item]
        if not matched:
            continue
        others = [
            argument
            for argument, item in zip(args, direct)
            if not item
        ]
        if not others or not all(VALUE_EXPR.fullmatch(argument) for argument in others):
            problems.append(
                f"{path}: `Assert.Equal` compares a version constant with a "
                f"non-value expression: {statement.strip()}"
            )
            continue
        compared.update(matched)

    # An interpolated `expected` string built from all three constants and
    # compared with the library's version string is the other legitimate shape.
    if re.search(r"Assert\.(?:StartsWith|Contains|Equal)\s*\(\s*expected", source) and all(
        f"GHOSTTY_VT_VERSION_{name}" in source for name in CONSTANTS
    ):
        compared.update(CONSTANTS)

    # Every constant the file text mentions must be checked structurally here.
    # This is what catches a test gutted to `Assert.True(true); // CONST`: the
    # comment still names the constant, but the stripped body compares nothing.
    mentioned = {name for name in CONSTANTS if f"GHOSTTY_VT_VERSION_{name}" in raw}
    for name in sorted(mentioned - compared):
        problems.append(
            f"{path}: mentions GHOSTTY_VT_VERSION_{name} but does not compare it "
            f"structurally (a comment or trivial assertion is not enough)"
        )

    return problems, compared


def main(argv: list[str]) -> int:
    root = Path(argv[1] if len(argv) > 1 else "tests")
    if not root.is_dir():
        print(f"check-version-assertions: not a directory: {root}", file=sys.stderr)
        return 2
    files = sorted(
        path
        for path in root.rglob("*.cs")
        if "obj" not in path.parts and "bin" not in path.parts
    )
    if not files:
        print(f"check-version-assertions: no C# test files under {root}", file=sys.stderr)
        return 2

    problems: list[str] = []
    compared: set[str] = set()
    for path in files:
        file_problems, file_compared = check_file(path)
        problems.extend(file_problems)
        compared.update(file_compared)

    if not problems:
        problems.extend(
            f"no `Assert.Equal` compares the loaded library against "
            f"GHOSTTY_VT_VERSION_{name}"
            for name in CONSTANTS
            if name not in compared
        )
    if problems:
        for problem in problems:
            print(problem)
        return 1
    print(f"version assertions compare the constants in {len(files)} test file(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
