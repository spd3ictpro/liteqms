---
name: git-commit-push
description: End-of-session git commit and push with auto-generated commit messages based on changes made during the session.
---

# Git Commit & Push

## When to Use
Use when the user says: "end session", "commit", "push", "wrap up", "save changes", or similar.

## Workflow

### Step 1 — Review Changes
Run `git diff --stat` and `git diff` to capture all unstaged changes. Identify:

- **New files** (`Untracked` in `git status`)
- **Modified files** (CSS, CSHTML, JS, etc.)
- **Files to exclude** (`.db` files under `Data/`, `bin/`, `obj/` — already in `.gitignore`)

### Step 2 — Categorize Changes
Classify by type:

| Category | Prefix | Examples |
|----------|--------|----------|
| New feature | `feat` | new files, new functionality |
| Bug fix | `fix` | readability, layout fixes |
| Style/UI | `style` | spacing, sizing, borders, theme colors |
| Refactor | `refactor` | moving inline CSS, cleanup |
| Chore | `chore` | config files, skill files |

### Step 3 — Generate Commit Message
Format: `<type>: <summary>

- <detail 1>
- <detail 2>
- <detail 3>`

Keep summary under 72 chars. List key changes as bullet points.

**Examples:**
```
feat: implement color theme system with teal, blue, and dark modes

- Add theme switcher dropdown on Doctor and History pages
- Add theme dot selector on Index page
- Persist theme choice via localStorage
```

```
style: increase navbar sizing and adjust card spacing

- Double navbar height and scale all inner elements
- Add spacing between form label and digit inputs
- Make preview card border visible in dark mode
```

### Step 4 — Present to User
Show the generated message and ask:
> "Commit with this message? (y/n)"

If the user says no, ask what to change and regenerate.

### Step 5 — Commit & Push
```
git add -A
git commit -m "<generated message>"
git push
```

Report the result with commit hash and branch name.

## Important Notes
- Always run `dotnet build` before committing to verify nothing is broken
- Ensure the working directory is `D:\Programs\liteqms`
- The `.gitignore` already excludes `bin/`, `obj/`, `*.db` — no manual exclusion needed
