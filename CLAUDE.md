# BranikBot — Claude Code Configuration

## Project

.NET 10.0 Discord bot (C#). Clean Architecture: Domain → Application → Infrastructure → ConsoleApp.

## Build & Test

```bash
dotnet build src/
dotnet test src/BranikBot.Tests/
```

## Obsidian Vault Integration

This project uses an Obsidian vault as a persistent context map across Claude Code sessions.

**Vault location**: `~/Documents/Obsidian Vaults/branik-bot/`

### Slash Commands

- `/obsidian-load` — Load vault context at the start of a session
- `/obsidian-save` — Save session learnings back to the vault
- `/obsidian-status` — View vault contents and active tasks
- `/obsidian-search <query>` — Search the vault for specific topics

### When to use

- **Start of session**: Run `/obsidian-load` to get context from prior sessions
- **End of session**: Run `/obsidian-save` to persist what was learned/decided/done
- **During work**: Reference vault notes when making decisions that relate to prior context

### Vault structure

- `Architecture/` — System design, patterns, tech stack
- `Code/` — Notes about specific modules, gotchas, integration points
- `Decisions/` — Architecture Decision Records (ADRs)
- `Tasks/` — Work items with status tracking
- `Sessions/` — Session summaries linking everything together
- `Templates/` — Note templates for consistency

### Conventions

- Use `[[wiki-links]]` to connect related notes
- Include YAML frontmatter with tags, created/updated dates
- Keep notes concise and actionable
