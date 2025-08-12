# CLAUDE.md

## Technology Options
- **.NET/C#**: `Microsoft.SemanticKernel`, `Microsoft.KernelMemory.Core` NuGet packages

## Configuration
Claude Code settings are configured in `.claude/settings.local.json`

## Workflow Rules (STRICT ENFORCEMENT)
- **Design Principle**: 
  - Minimize complexity - meet requirements only
  - Avoid over-engineering
  - Keep solutions simple and focused

- **Planning & Task Management**: 
  - All requirements must start in plan mode for discussion
  - Consider design principles during planning phase
  - Break down complex tasks into manageable steps
  - Record all planned tasks and steps into `TODO.md` (create file if it doesn't exist)
  - `TODO.md` is excluded from git
  - Tasks must be clearly defined and actionable

- **One Task Rule**: 
  - Read `TODO.md` file before starting work
  - Select appropriate sub-agent for the task
  - Execute ONE task at a time with user confirmation
  - No parallel task execution

- **Branch Management**: 
  1. Switch to `main` branch and pull latest changes before starting a new task
  2. Create a new branch per task using format: `feature/task{number}-{description}`  
  3. Make meaningful commits for each step - avoid single commits with multiple changes
  4. Write clear PR descriptions summarizing all commits

- **Testing**: 
  - Self-test and verify completion against requirements
  - Ensure all functionality works as expected
  - Run relevant test suites if available

- **Cleanup**: 
  - After testing is complete, terminate all processes started during the testing phase
  - Mark the completed task as done in `TODO.md`
  - Clean up any temporary files or resources