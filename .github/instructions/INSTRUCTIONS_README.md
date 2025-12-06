# GitHub Copilot Instructions Structure

This repository uses path-specific GitHub Copilot instructions based on citypaul's modular structure, adapted for C# backend and TypeScript/React/Vitest frontend development.

## File Structure

```
.github/
  ├── copilot-instructions.md           # Main repository-wide instructions
  └── instructions/
      ├── backend-csharp.instructions.md      # C# specific guidelines
      ├── frontend-typescript.instructions.md # TypeScript/React guidelines
      ├── testing.instructions.md             # Testing guidelines (both languages)
      ├── code-style.instructions.md          # Code style (both languages)
      └── workflow.instructions.md            # TDD workflow (both languages)
```

## How It Works

GitHub Copilot automatically loads:

1. **Repository-wide instructions** (`.github/copilot-instructions.md`) - Always loaded for all files
2. **Path-specific instructions** (`.github/instructions/*.instructions.md`) - Loaded based on file patterns

### Path-Specific Instruction Triggers

| File Pattern | Instructions Loaded |
|-------------|-------------------|
| `**/*.cs`, `**/*.csproj` | `backend-csharp.instructions.md` |
| `**/*.ts`, `**/*.tsx`, `**/*.jsx` | `frontend-typescript.instructions.md` |
| `**/*.test.ts`, `**/*.test.tsx`, `**/*.Tests.cs` | `testing.instructions.md` |
| `**` (all files) | `code-style.instructions.md`, `workflow.instructions.md` |

## What Each File Contains

### copilot-instructions.md
- Core philosophy (TDD non-negotiable)
- Quick reference for both C# and TypeScript
- Universal principles applying to both stacks
- Overview of path-specific instructions

### backend-csharp.instructions.md
- C# project configuration (nullable reference types, warnings as errors)
- Record types for immutability
- ImmutableList/ImmutableDictionary usage
- LINQ and functional patterns
- xUnit/NUnit testing patterns
- Dependency injection patterns
- C# naming conventions

### frontend-typescript.instructions.md
- TypeScript strict mode configuration
- Schema-first development with Zod
- React component patterns
- Immutable state updates in React
- Custom hooks patterns
- Vitest and React Testing Library
- TypeScript naming conventions

### testing.instructions.md
- TDD process (RED-GREEN-REFACTOR)
- Behavior-driven testing principles
- Factory functions for test data
- Testing anti-patterns to avoid
- Achieving 100% coverage through behavior
- Testing tools for both C# and TypeScript

### code-style.instructions.md
- Immutability rules (CRITICAL - no mutations)
- Code structure rules (max 2 levels nesting)
- Self-documenting code (no comments)
- Function parameters (options objects)
- Functional programming patterns
- Naming conventions for both languages

### workflow.instructions.md
- RED-GREEN-REFACTOR cycle details
- Refactoring priority framework
- Understanding DRY (knowledge vs code)
- Commit guidelines
- TDD verification
- Pull request standards
- Common TDD violations

## Key Principles Across All Files

### 1. TDD is Non-Negotiable
Every line of production code must be written in response to a failing test. No exceptions.

### 2. Test Behavior, Not Implementation
Test what the code does (behavior through public APIs), not how it does it (implementation details).

### 3. Immutability Always
No data mutation in either C# or TypeScript. Use immutable patterns and collections.

### 4. Type Safety
- TypeScript: Strict mode, no `any` types
- C#: Nullable reference types, no unconstrained `dynamic`

### 5. Small, Focused Functions
Maximum 2 levels of nesting, single responsibility, self-documenting code.

## Using These Instructions

### VS Code
1. Place all files in `.github/` and `.github/instructions/` directories
2. GitHub Copilot automatically loads them based on the file you're working on
3. No additional configuration needed

### Verifying Instructions Are Loaded
In VS Code, check the Copilot panel to see which instruction files are currently active for your file.

## Modifying Instructions

When updating guidelines:

1. **Repository-wide changes**: Edit `.github/copilot-instructions.md`
2. **C# specific changes**: Edit `.github/instructions/backend-csharp.instructions.md`
3. **TypeScript specific changes**: Edit `.github/instructions/frontend-typescript.instructions.md`
4. **Testing changes**: Edit `.github/instructions/testing.instructions.md`
5. **Style changes**: Edit `.github/instructions/code-style.instructions.md`
6. **Workflow changes**: Edit `.github/instructions/workflow.instructions.md`

## Differences from citypaul's Original Structure

### Original (citypaul/.dotfiles)
- Designed for Claude Code with `@~/.claude/docs/...` imports
- TypeScript-only focus
- Jest testing framework
- Single global installation

### This Adaptation
- Designed for GitHub Copilot with path-specific `.instructions.md` files
- Dual stack: C# backend + TypeScript/React frontend
- xUnit/NUnit for C#, Vitest for TypeScript
- Project-specific installation
- Extended with C#-specific patterns (records, ImmutableList, LINQ)

## Credits

Based on [citypaul/.dotfiles](https://github.com/citypaul/.dotfiles) CLAUDE.md v1.0.0 and v2.0.0 structure, adapted for:
- GitHub Copilot's instruction system
- C# + TypeScript dual-stack projects
- Vitest testing framework
- Path-specific instruction loading

## Resources

- [GitHub Copilot Custom Instructions Documentation](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions)
- [citypaul/.dotfiles](https://github.com/citypaul/.dotfiles)
- [Testing Library Principles](https://testing-library.com/docs/guiding-principles)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/handbook/intro.html)