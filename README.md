# LocationAR_PoC

Unity-based civic infrastructure reporting proof of concept.

## Demo Webhooks

Use these temporary webhook URLs for demo screenshots and payload verification.

- CV analysis upload webhook: https://webhook.site/474a07a2-86d1-4bc6-8452-9bb1f56bea10
- Grievance submission webhook: https://webhook.site/b6bdfc05-add8-4a7c-a074-fca4b5ead106

## Git Workflow

### Main branch
- `main` is always the stable branch.
- Only tested and reviewed work should be merged into `main`.

### Feature branches
- Create a feature branch from `main` for each focused task.
- Branch name format: `feature/<short-name>`.
- Example (first feature): `feature/failure-states`.

### Daily flow
1. Start from latest `main`.
2. Create a feature branch.
3. Commit small logical changes.
4. Push branch to origin.
5. Open a Pull Request to `main`.
6. Review and test.
7. Merge when stable.

## First Feature Branch: Failure States

Use branch `feature/failure-states` for work related to failure-state handling, including:
- No road detected
- Road not in government dataset
- Multiple matching projects
- CV service failure fallback to complaint submission

Use the exact user-facing messages required by project instructions.

## Useful Commands

```bash
# from main
git checkout main
git pull origin main

# create feature branch
git checkout -b feature/failure-states

# push branch
git push -u origin feature/failure-states

# open PR in browser (after pushing)
gh pr create --base main --head feature/failure-states --fill
```