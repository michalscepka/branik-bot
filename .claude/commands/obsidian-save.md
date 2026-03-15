Save the current session's context back to the Obsidian vault at `~/Documents/Obsidian Vaults/branik-bot/`.

## Instructions

Review the full conversation and save relevant information:

### 1. Session Summary
Create a new file in `Sessions/` named `Session-{YYYY-MM-DD}.md` (use today's date). If one already exists for today, append a letter suffix (e.g., `Session-2026-03-15b.md`). Include:
- Goal of the session
- What was accomplished
- Decisions made
- Open questions
- Next steps
- Links to related vault notes using `[[wiki-links]]`

### 2. Update Existing Notes
If any information in `Architecture/`, `Code/`, or `Decisions/` has changed due to this session's work, update those notes. Always update the `updated:` field in frontmatter.

### 3. New Notes
If new code areas, patterns, or components were introduced, create appropriate notes in `Code/` or `Architecture/`. Use the templates in `Templates/` as a guide for structure.

### 4. Task Updates
- Update status of any tasks that were worked on
- Create new task notes for any identified follow-up work

### 5. Decision Records
If any significant technical decisions were made, create an ADR in `Decisions/` using the template.

### General Rules
- Always use `[[wiki-links]]` to connect related notes
- Always include appropriate tags in frontmatter
- Keep notes concise and actionable
- Update the `updated:` date in frontmatter of any modified files

Confirm what was saved when done.
