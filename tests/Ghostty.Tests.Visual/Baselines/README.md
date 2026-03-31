# Visual Test Baselines

This directory contains baseline screenshots for visual regression testing.

## Updating baselines

When a visual change is intentional (new font, theme change, etc.):

```bash
just update-baselines
```

Then review the changes in git and commit:

```bash
git diff --stat
git add tests/Ghostty.Tests.Visual/Baselines/
git commit -m "test: update visual baselines"
```

## Structure

```
Baselines/
  Win32/
  WinForms/
  WPF-Simple/
  WPF-Direct/
```

Each subdirectory contains PNG screenshots named after the test that produced them.
