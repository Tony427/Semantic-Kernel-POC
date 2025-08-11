# CLAUDE.md

## Repository Status
Minimal Semantic Kernel POC repository - initial state, ready for implementation.

## Technology Options
- **.NET/C#**: Microsoft.SemanticKernel NuGet packages
- **Python**: semantic-kernel package
- **TypeScript/Node.js**: @microsoft/semantic-kernel npm package

## Configuration
Claude Code settings configured in `.claude/settings.local.json`

## Workflow Rules (STRICT ENFORCEMENT)
- **Planning**: All requirements start in plan mode for discussion
- **Task Management**: Use TODO.md to track tasks (excluded from git)
- **One Task Rule**: Execute ONE task at a time with user confirmation
- **Branch Management**: 
  1. Switch to main branch and pull latest changes before new task
  2. Create new branch per task: `task/t{number}-{description}`  
  3. Meaningful commits with PR descriptions
- **Testing**: Self-test and verify completion against requirements
- **Design**: Minimize complexity - meet requirements only