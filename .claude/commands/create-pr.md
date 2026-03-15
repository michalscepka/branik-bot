# Command: Create Pull Request

Create a pull request for the current branch following project conventions.

## Execution Steps

1. **Clean Workspace Check**
   - Run `git status`.
   - If there are uncommitted changes, ask the user to commit or stash before proceeding.

2. **Branch Naming Check**
   - Ensure current branch name starts with `feat/`, `fix/`, or `refactor/`.
   - If not, warn the user but proceed if they confirm.

3. **Branch Review**
   - Review all commits on the current branch compared to main:
     ```bash
     git log main..HEAD --oneline
     git diff main...HEAD --stat
     ```

4. **Sync with Remote**
   - Ask the user for confirmation before pushing.
   - Push the current branch:
     ```bash
     git push -u origin $(git branch --show-current)
     ```

5. **PR Creation**
   - Use `gh pr create` with:
     - **Title**: Conventional Commit format (e.g., `feat: add cooldown config`). Keep under 70 characters.
     - **Base**: `main`.
     - **Labels**: Apply from existing repo labels: `enhancement`, `bug`, `documentation`. Only add labels that match the change.
     - **Body** (use HEREDOC for formatting):
       ```
       ## Summary
       <1-3 bullet points describing the high-level goal>

       ## Key Changes
       <bulleted list of modifications>

       ## Test Plan
       <how the changes were tested>

       Closes #N (only if an issue exists)

       🤖 Generated with [Claude Code](https://claude.com/claude-code)
       ```

6. **Final Report**
   - Report the PR URL to the user.
