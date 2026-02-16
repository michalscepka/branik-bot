# Command: Create Pull Request

Create a pull request for the current branch following project conventions.

## Execution Steps

1. **Clean Workspace Check**
   - Run `git status`.
   - If there are uncommitted changes, ask the user to commit or stash before proceeding.

2. **Branch Naming Check**
   - Ensure current branch name starts with `feat/`, `fix/`, or `refactor/`.

3. **Branch Review**
   - Review all commits on the current branch compared to main:
     ```bash
     git log main..HEAD --oneline
     ```

4. **Sync with Remote**
   - Push the current branch:
     ```bash
     git push -u origin $(git branch --show-current)
     ```

5. **PR Creation**
   - Use `gh pr create` with:
     - **Title**: Conventional Commit format.
     - **Base**: `main`.
     - **Labels**: Apply relevant labels (`feature`, `bug`, `infrastructure`, `tests`, `domain`).
     - **Body**:
       - **Summary**: High-level goal.
       - **Key Changes**: List of modifications.
       - **Test Plan**: Confirmation of tests run.
       - **Closing**: `Closes #N`.

6. **Final Report**
   - Report the PR URL.

## Merge Policy
- **Squash and Merge** only: `gh pr merge <number> --squash`.
