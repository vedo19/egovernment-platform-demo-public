# Contributing Guide

## Commit Messages

We use **Conventional Commits** to keep our git history clean and enable automated versioning.

### Format

```
<type>(<scope>): <description>

[optional body]
[optional footer]
```

### Types

| Type       | Description                               |
| ---------- | ----------------------------------------- |
| `feat`     | New feature                               |
| `fix`      | Bug fix                                   |
| `docs`     | Documentation only                        |
| `style`    | Code style (formatting, semicolons, etc.) |
| `refactor` | Code refactoring                          |
| `test`     | Adding/updating tests                     |
| `chore`    | Maintenance (deps, configs, tooling)      |

### Examples

```bash
# Good commit messages
git commit -m "feat(dashboard): add stats cards to admin page"
git commit -m "fix: resolve lint errors in AuthContext"
git commit -m "docs: update README with setup instructions"
git commit -m "chore: add husky for pre-commit hooks"

# Bad commit messages
git commit -m "fixed stuff"
git commit -m "WIP"
git commit -m "asdfgh"
```

### Rules

- Use lowercase for type and description
- Description: max 50 characters
- Body: wrap at 72 characters
- Use imperative mood ("add" not "added")
- Reference issues in footer: `Closes #123`

## Pre-commit Hooks

Before each commit, these checks run automatically:

1. **Prettier** - Formats JS/JSX/CSS files
2. **ESLint** - Validates code quality

If either fails, the commit is blocked. Fix the issues and try again.

## Quick Start

```bash
# Clone the repo
git clone https://github.com/vedo19/egovernment-platform-demo-public.git
cd egovernment-platform-demo-public

# Install dependencies
npm install
cd src/frontend && npm install && cd ../..

# Make changes, then commit
git add .
git commit -m "feat: your feature here"
git push origin develop
```

## Need Help?

- Check [Conventional Commits](https://www.conventionalcommits.org/)
- Ask in the team chat
