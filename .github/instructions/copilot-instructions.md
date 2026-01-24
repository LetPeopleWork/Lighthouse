---
applyTo: "**"
---

# Development Guidelines for GitHub Copilot

> **About this repository:** This project uses C# for backend services and TypeScript/React/Vitest for frontend development. These instructions ensure consistent development practices across both stacks.

## Core Philosophy

**TEST-DRIVEN DEVELOPMENT IS NON-NEGOTIABLE.** Every single line of production code must be written in response to a failing test. No exceptions. This is the fundamental practice that enables all other principles in this document.

Follow Test-Driven Development (TDD) with a strong emphasis on behavior-driven testing and functional programming principles. All work should be done in small, incremental changes that maintain a working state throughout development.

## Quick Reference

**Key Principles:**
- Write tests first (TDD is mandatory)
- Test behavior, not implementation
- No `any` types (TypeScript) or dynamic types without constraints (C#)
- Immutable data only
- Small, pure functions
- TypeScript strict mode and C# nullable reference types always enabled
- Use real schemas/types in tests, never redefine them

**Tech Stack:**
- **Backend**: C# with .NET
- **Frontend**: TypeScript (strict mode) + React + Vitest
- **Testing**: xUnit/NUnit (C#), Vitest + React Testing Library (TypeScript)
- **State Management**: Immutable patterns in both languages

## Path-Specific Instructions

This repository contains additional path-specific instructions that apply to different parts of the codebase:

- **Backend (C#)**: See `.github/instructions/backend-csharp.instructions.md`
- **Frontend (TypeScript/React)**: See `.github/instructions/frontend-typescript.instructions.md`
- **Testing Guidelines**: See `.github/instructions/testing.instructions.md`
- **Code Style**: See `.github/instructions/code-style.instructions.md`
- **Development Workflow**: See `.github/instructions/workflow.instructions.md`

## Universal Principles (Both C# and TypeScript)

### TDD Process - THE FUNDAMENTAL PRACTICE

Follow RED-GREEN-REFACTOR strictly:

1. **RED**: Write a failing test first - NO production code without a failing test
2. **GREEN**: Write MINIMUM code to pass the test
3. **REFACTOR**: Assess improvement opportunities (only refactor if adds value)

### Testing Principles

- Test behavior through public APIs, not implementation details
- 100% coverage through business behavior testing
- No 1:1 mapping between test files and implementation files
- Use factory functions for test data (no shared mutable state)
- Tests must document expected business behavior

### Type Safety

**TypeScript:**
- No `any` types - use `unknown` if type is truly unknown
- Strict mode always enabled
- Schema-first at trust boundaries (Zod)

**C#:**
- Nullable reference types enabled
- No dynamic types without constraints
- Use record types for immutable data structures

### Immutability

- No data mutation in either language
- Pure functions wherever possible
- Use immutable collections in C# (ImmutableList, ImmutableDictionary)
- Use spread operators and array methods in TypeScript

### Code Structure

- Maximum 2 levels of nesting
- Early returns instead of nested conditionals
- Functions focused on single responsibility
- Self-documenting code (no comments unless necessary)

## Working with Copilot

**Expectations:**
1. ALWAYS follow TDD - no production code without failing test
2. Assess refactoring after every green (but only if adds value)
3. Generate complete, working solutions (no placeholders)
4. Verify type safety in both languages
5. Check immutability violations
6. Test behavior, never implementation details

**When generating code:**
- Start with test describing desired behavior
- Write minimum implementation to pass test
- Suggest refactoring only if HIGH or CRITICAL priority
- Maintain immutability in all generated code
- Use schema validation at trust boundaries

**When generating tests:**
- Test what code does, not how it does it
- Use factory functions for test data
- Import schemas from codebase - never redefine
- Each test documents expected business behavior

## Error Handling

**TypeScript:**
```typescript
type Result<T, E = Error> = 
  | { success: true; value: T }
  | { success: false; error: E };
```

**C#:**
```csharp
public record Result<T, TError>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public TError? Error { get; init; }
}
```

## Summary

Write clean, testable, functional code that evolves through small, safe increments. Every change is driven by a test describing desired behavior. Implementation is the simplest thing that makes the test pass.

**Remember:** TDD is not optional. Tests come first. Always.