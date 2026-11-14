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
* `var expected = "1.1.0"; Assert.StartsWith(expected, text)` hides the same
  literal behind a local, so arguments that are plain identifiers assigned in
  the same file are replaced by their assigned expression (one level, no
  recursion) before the literal checks.
* `#if false` / `#if DEBUG` around the comparisons keeps every statement
  textually present while none of them compile, so preprocessor-conditional
  regions are blanked before any structural credit: a constant the raw file
  mentions but the compiled view does not compare is reported.
* `Assert.StartsWith("\\u0031.1.0", text)` spells the literal as a unicode
  escape, so string-literal bodies are escape-decoded before the shape checks.
* Comparisons moved into an uncalled helper still look live to a text scan,
  so only statements inside a `[Fact]`/`[Theory]` method body credit a
  constant; anything else that mentions one is reported.

So the checks are structural. Comments are removed first, then each assertion
statement is read as an argument list:

1. Each of MAJOR, MINOR, PATCH must be compared with a value expression (an
   identifier or member access), never a literal or another constant. That is
   either a direct `Assert.Equal((nuint)CONST, value)` or a comparison of a
   variable that was built from all three constants in live code.
2. No `Assert.True`/`Assert.False` may contain a version constant.
3. No bare version literal (`1.1.0`, `"1" + "." + "1"`, `$"1.{x}.0"`) may be
   compared, whatever the other operand is named. Concatenation, interpolation,
   `string.Concat`, `string.Join`, and `new Version(...)` are all rejected.

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

# Unicode escapes inside a string literal can spell digits a regex would not
# see: `"\u0031.1.0"` is `1.1.0`. Decode `\uXXXX` and `\xXX` escapes when the
# result is printable ASCII so shape checks read what the compiler reads.
ESCAPE = re.compile(r"\\(?:u[0-9a-fA-F]{4}|x[0-9a-fA-F]{2})")


def decode_escapes(text: str) -> str:
    def replace(match: re.Match) -> str:
        hex_digits = match.group(0)[2:].lstrip("u").lstrip("x")
        try:
            code_point = int(hex_digits, 16)
        except ValueError:
            return match.group(0)
        return chr(code_point) if 0x20 <= code_point < 0x7F else match.group(0)

    return ESCAPE.sub(replace, text)


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


def blank_preprocessor_blocks(text: str) -> str:
    """Blank the characters of `#if`-conditional regions, keeping newlines.

    A comparison inside `#if false` (or any other condition the gate cannot
    evaluate) is textually present but never compiles. Rather than model C#
    preprocessor expressions, every `#if … #endif` region is blanked: nothing
    inside one can credit a structural comparison. The directive lines
    themselves are blanked too, so a `#if` cannot smuggle a `#else` half-open.

    Verbatim strings cannot span lines and `#` does not start a string, so
    scanning line-wise after comment removal is safe.
    """
    lines = text.split("\n")
    out: list[str] = []
    depth = 0
    for line in lines:
        stripped = line.strip()
        if depth > 0:
            if stripped.startswith("#endif"):
                depth -= 1
            elif stripped.startswith("#if"):
                depth += 1
            out.append("")
            continue
        if stripped.startswith("#if"):
            depth += 1
            out.append("")
            continue
        out.append(line)
    return "\n".join(out)


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
        decode_escapes(piece[piece.index('"') + 1 : piece.rindex('"')])
        for piece in pieces
    )


def interpolated_version_literal(expression: str) -> str | None:
    """Return an interpolated string that embeds literal version components."""
    for literal in re.findall(r'\$"(?:[^"\\]|\\.)*"', expression):
        without_expressions = re.sub(r"\{[^}]*\}", "", decode_escapes(literal))
        if re.search(r"\d+\s*\.|\.\s*\d+", without_expressions):
            return literal
    return None


def version_literal(statement: str, resolve=None) -> str | None:
    """Return the version-shaped literal an assertion compares, if any.

    `resolve` substitutes an argument that is a bare local with the expression
    it was assigned, so a literal hidden behind `var expected = "1.1.0";` is
    read the same as an inline one.
    """
    if resolve is None:
        resolve = lambda argument: argument  # noqa: E731
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
            argument = resolve(argument)
            joined = literal_value(argument)
            if joined is not None and VERSION_SHAPE.search(joined):
                return joined
            interpolated = interpolated_version_literal(argument)
            if interpolated is not None:
                return interpolated
            if STRING_LITERAL.fullmatch(argument.strip() or ""):
                text = argument.strip()
                body = decode_escapes(text[text.index('"') + 1 : text.rindex('"')])
                if VERSION_SHAPE.search(body):
                    return text
    return None


def bare_version_literal(expression: str) -> str | None:
    """Return a bare version a string expression hardcodes, if any.

    `"1.1.0"`, `"1" + "." + "1" + "." + "0"`, and `$"1.{x}.0"` are all bare
    versions: the whole expression is the version, so it re-couples the test to
    a literal. `"PowerShell 7.5.5"` is not, because it is prose that happens to
    contain a version, so the gate must not reject it.
    """
    joined = literal_value(expression)
    if joined is not None:
        joined = joined.strip()
        if VERSION_SHAPE.fullmatch(joined) and joined != "0.0.0":
            return joined
        return None
    for literal in re.findall(r'\$@?"(?:[^"\\]|\\.)*"', expression):
        body = literal[literal.index('"') + 1 : literal.rindex('"')]
        # A hole could stand for any component, so treat it as a digit. The
        # constants inside the holes are the legitimate shape, and that shape
        # is credited from the assignment, not from this literal.
        normalised = re.sub(r"\{[^}]*\}", "0", decode_escapes(body)).strip()
        if VERSION_SHAPE.fullmatch(normalised) and normalised != "0.0.0":
            return literal
    return None


def check_file(path: Path) -> tuple[list[str], set[str]]:
    raw = path.read_text(encoding="utf-8")
    source = strip_comments(raw)
    # The compiled view: preprocessor-conditional regions blanked. A statement
    # the compiler never sees cannot compare anything.
    compiled = blank_preprocessor_blocks(source)
    problems: list[str] = []
    compared: set[str] = set()

    # A variable assigned from an interpolated string that names all three
    # constants is the other legitimate shape: the expected version is built
    # from the generated constants, then compared with the library's value.
    # Only live code counts, so `_ = CONST;` three times binds nothing.
    bindings: dict[str, set[str]] = {}
    # Every simple local assignment, for one-level substitution: the literal
    # checks must see through `var expected = "1.1.0";`, which the statement
    # scan alone would read as an opaque identifier.
    assignments: dict[str, str] = {}
    for match in re.finditer(
        r"\b(?:var\s+)?([A-Za-z_]\w*)\s*=(?!=)\s*([^;]*);", compiled
    ):
        target = match.group(1)
        if target == "_":
            continue
        assignments[target] = match.group(2).strip()
        names = set(VERSION_CONST.findall(match.group(2)))
        if names == set(CONSTANTS):
            bindings[target] = names

    def resolve(argument: str) -> str:
        name = argument.strip()
        # One level, no recursion: a self-referential or chained local cannot
        # hide more than its own assignment, and the statement checks still
        # inspect every operand they can read.
        return assignments.get(name, name)

    # Live assertion sites only: an assert inside an uncalled helper has never
    # run in any test pass, so crediting it would let a gutted test satisfy
    # the gate. Attribute each `Assert.` match to the nearest preceding
    # `[Fact]`/`[Theory]` attribute; matches before any attribute (asserts in
    # helper classes, top-level) are not live test bodies.
    live_spans: list[tuple[int, int]] = []
    for attribute in re.finditer(
        r"\[(Fact|Theory)[^\]]*\]", compiled
    ):
        start = attribute.end()
        # The method body's closing brace: attribute, signature, then `{ … }`.
        # Track braces from the method's opening brace.
        open_brace = compiled.find("{", start)
        if open_brace == -1:
            continue
        depth = 0
        index = open_brace
        while index < len(compiled):
            if compiled[index] == "{":
                depth += 1
            if compiled[index] == "}":
                depth -= 1
                if depth == 0:
                    live_spans.append((open_brace, index))
                    break
            index += 1

    def is_live(position: int) -> bool:
        return any(start <= position < end for start, end in live_spans)

    for match in re.finditer(
        r"\bAssert\.(Equal|True|False|StartsWith|Contains)\s*\(", compiled
    ):
        if not is_live(match.start()):
            continue
        statement = statement_at(compiled, match.start())
        function = match.group(1)

        literal = version_literal(statement, resolve)
        if literal is not None and literal != "0.0.0" and VERSION_CONTEXT.search(statement):
            problems.append(
                f"{path}: assertion re-couples to a version literal {literal!r}: "
                f"{statement.strip()}"
            )

        if function in ("Equal", "StartsWith", "Contains"):
            for argument in split_arguments(statement) or []:
                bare = bare_version_literal(resolve(argument))
                if bare is not None:
                    problems.append(
                        f"{path}: assertion compares a bare version literal "
                        f"{bare!r}, whatever the other operand is called: "
                        f"{statement.strip()}"
                    )

        constants = VERSION_CONST.findall(statement)

        # Credit a comparison whose expected value was built from the three
        # constants. The other operands must still be values, not literals.
        if function in ("Equal", "StartsWith"):
            args = split_arguments(statement) or []
            if len(args) >= 2:
                bound = bindings.get(args[0].strip())
                rest = [argument.strip() for argument in args[1:]]
                if bound and rest and all(VALUE_EXPR.fullmatch(a) for a in rest):
                    compared.update(bound)

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
        # A "value" operand that resolves to the constant itself (through a
        # local: `var mirror = CONST; Assert.Equal(CONST, mirror)`) is
        # trivially true and compares the library against nothing. Resolve
        # one level and reject a constant on the library side of the assert.
        mirrored = [
            argument
            for argument in others
            if VERSION_CONST.search(resolve(argument))
        ]
        if mirrored:
            problems.append(
                f"{path}: `Assert.Equal` compares a version constant with a "
                f"local assigned from a version constant (trivially true): "
                f"{statement.strip()}"
            )
            continue
        compared.update(matched)

    # Every constant the file text mentions must be checked structurally here.
    # This is what catches a test gutted to `Assert.True(true); // CONST`: the
    # comment still names the constant, but the stripped body compares nothing.
    mentioned = {name for name in CONSTANTS if f"GHOSTTY_VT_VERSION_{name}" in raw}
    for name in sorted(mentioned - compared):
        problems.append(
            f"{path}: mentions GHOSTTY_VT_VERSION_{name} but does not compare it "
            f"structurally (a comment or trivial assertion is not enough)"
        )

    return problems, compared & mentioned


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
