# CLAUDE.md

## Repository Status
Minimal Semantic Kernel POC repository - initial state, ready for implementation.

## Technology Options
- **.NET/C#**: Microsoft.SemanticKernel NuGet packages
- **Python**: semantic-kernel package
- **TypeScript/Node.js**: @microsoft/semantic-kernel npm package

## Configuration
Claude Code settings configured in `.claude/settings.local.json`

## Workflow Rules
- All requirements start in plan mode for discussion
- Auto-select appropriate sub-agents for planning and coding tasks
- Create TODO.md to manage tasks with priorities (exclude from git via .gitignore)
- TODO includes detailed requirements and specifications for each task
- Delete completed tasks from TODO.md to minimize token consumption
- Minimize design complexity to meet task requirements only
- Execute one task at a time with user confirmation
- Self-test and verify completion against task requirements
- Create new branch for each task with meaningful commits and PR descriptions