---
applyTo: "**"
---

# Development Workflow Guidelines

These workflow guidelines apply to all development work in this repository.

## RED-GREEN-REFACTOR Cycle

**THE FUNDAMENTAL PRACTICE - NON-NEGOTIABLE**

### Step 1: RED - Write Failing Test

```typescript
// TypeScript example
describe('PaymentProcessor', () => {
  it('should reject negative amounts', () => {
    const payment = getMockPayment({ amount: -100 });
    const processor = new PaymentProcessor();
    
    const result = processor.process(payment);
    
    expect(result.success).toBe(false);
    expect(result.error.message).toBe('Amount must be positive');
  });
});

// Run test - it should FAIL because processPayment doesn't exist yet
// ❌ FAIL: Cannot find name 'processPayment'
```

```csharp
// C# example
[Fact]
public void Should_Reject_Negative_Amounts()
{
    var payment = GetMockPayment(amount: -100m);
    var processor = new PaymentProcessor();
    
    var result = processor.Process(payment);
    
    Assert.False(result.Success);
    Assert.Equal("Amount must be positive", result.Error?.Message);
}

// Run test - it should FAIL
// ❌ FAIL: 'PaymentProcessor' does not contain a definition for 'Process'
```

**Critical Rules:**
- NO production code without a failing test
- Test must fail for the RIGHT reason
- Verify the test actually fails before proceeding

### Step 2: GREEN - Minimum Implementation

```typescript
// Write ONLY enough code to make test pass
export class PaymentProcessor {
  process(payment: Payment): Result<ProcessedPayment, PaymentError> {
    if (payment.amount < 0) {
      return {
        success: false,
        error: { message: 'Amount must be positive' }
      };
    }
    
    // Don't implement anything else yet!
    throw new Error('Not implemented');
  }
}

// Run test - it should PASS
// ✅ PASS: 1 test passing
```

**Critical Rules:**
- Write MINIMUM code to pass current test
- Don't add features not demanded by a test
- Don't optimize prematurely
- Resist urge to "while I'm here" add more functionality

### Step 3: REFACTOR - Assess and Improve

```typescript
// After GREEN, commit first
git add .
git commit -m "feat: add negative amount validation"

// THEN assess refactoring opportunities
// Current code:
export class PaymentProcessor {
  process(payment: Payment): Result<ProcessedPayment, PaymentError> {
    if (payment.amount < 0) {
      return {
        success: false,
        error: { message: 'Amount must be positive' }
      };
    }
    throw new Error('Not implemented');
  }
}

// ASSESSMENT:
// ✅ Already clean - names are clear
// ✅ No duplication
// ✅ Logic is straightforward
// Conclusion: No refactoring needed

// OR if refactoring would add value:
const MIN_PAYMENT_AMOUNT = 0;

const isValidAmount = (amount: number): boolean => {
  return amount > MIN_PAYMENT_AMOUNT;
};

export class PaymentProcessor {
  process(payment: Payment): Result<ProcessedPayment, PaymentError> {
    if (!isValidAmount(payment.amount)) {
      return {
        success: false,
        error: { message: 'Amount must be positive' }
      };
    }
    throw new Error('Not implemented');
  }
}

// Commit refactoring separately
git commit -m "refactor: extract amount validation"
```

## Refactoring Priority Framework

After every GREEN, assess whether refactoring would add value:

### 🔴 CRITICAL (Must Fix Now)
- Type safety violations (`any` types, `dynamic` without constraints)
- Data mutation (mutating arrays, objects, collections)
- Security vulnerabilities
- Performance issues affecting users

```typescript
// 🔴 CRITICAL - Must fix
const addItem = (items: Item[], newItem: Item) => {
  items.push(newItem);  // MUTATION!
  return items;
};

// Fix immediately
const addItem = (items: Item[], newItem: Item) => [...items, newItem];
```

### ⚠️ HIGH VALUE (Should Fix This Session)
- Repeated business logic (semantic duplication)
- Functions over 30 lines
- Unclear naming obscuring intent
- Missing error handling at trust boundaries
- Magic numbers/strings representing business rules

```typescript
// ⚠️ HIGH - Extract repeated business logic
if (order.total > 50) { shippingCost = 0; }
// ... elsewhere in code
const shipping = order.total > 50 ? 0 : 5.99;

// Refactor to single source of truth
const FREE_SHIPPING_THRESHOLD = 50;
const STANDARD_SHIPPING_COST = 5.99;

const calculateShipping = (total: number) =>
  total > FREE_SHIPPING_THRESHOLD ? 0 : STANDARD_SHIPPING_COST;
```

### 💡 NICE TO HAVE (Consider Later)
- Structural simplification (fewer nested ifs)
- Extract cohesive utilities
- Improve test readability
- Minor naming improvements

### ✅ SKIP (Don't Refactor)
- Code that's already clean and expressive
- Structural similarity without semantic relationship
- Premature abstraction
- Speculative generalization
- "Looks similar" but represents different concepts

```typescript
// ✅ SKIP - Different business concepts, same structure
const validatePaymentAmount = (amount: number): boolean => {
  return amount > 0 && amount <= 10000;
};

const validateTransferAmount = (amount: number): boolean => {
  return amount > 0 && amount <= 10000;
};

// DO NOT abstract - these might have same structure today
// but represent different business rules that evolve independently
```

## Understanding DRY

**DRY = Don't Repeat KNOWLEDGE, not code**

### When to Abstract (Same Knowledge)

```typescript
// Same knowledge - "how we format a person's name"
const formatUserName = (first: string, last: string) => `${first} ${last}`;
const formatCustomerName = (first: string, last: string) => `${first} ${last}`;
const formatEmployeeName = (first: string, last: string) => `${first} ${last}`;

// ✅ Abstract - same semantic meaning
const formatPersonName = (first: string, last: string) => `${first} ${last}`;
```

### When NOT to Abstract (Different Knowledge)

```typescript
// Different knowledge - similar structure
const validateUserAge = (age: number) => age >= 18 && age <= 100;
const validateProductRating = (rating: number) => rating >= 1 && rating <= 5;

// ❌ DON'T abstract - different business concepts
// User age: legal requirements
// Product rating: 5-star system
// These will evolve independently
```

## Commit Guidelines

### Commit After Each Step

```bash
# After RED (optional, helps track TDD compliance)
git add src/features/payment/payment-processor.test.ts
git commit -m "test: add test for negative amount validation"

# After GREEN (required)
git add .
git commit -m "feat: implement negative amount validation"

# After REFACTOR (required, separate commit)
git add .
git commit -m "refactor: extract amount validation constants"
```

### Conventional Commits Format

```bash
feat: add new feature
fix: correct bug
refactor: improve code structure (no behavior change)
test: add or modify tests
docs: documentation changes
chore: maintenance tasks

# Examples:
feat(payment): add amount validation
fix(user): correct email validation regex
refactor(order): extract shipping calculation
test(payment): add edge cases for validation
```

### What to Commit

```bash
# ✅ Good - Complete working change with tests
git commit -m "feat: add user authentication"

# ✅ Good - Separate refactor commit
git commit -m "refactor: extract validation helpers"

# ❌ Bad - Test and implementation together (implies no TDD)
git commit -m "add user authentication and tests"

# ❌ Bad - Multiple unrelated changes
git commit -m "add authentication, fix bug in orders, update readme"
```

## TDD Verification

### How to Verify TDD Was Followed

```bash
# Check git history for proper sequence
git log --oneline --follow src/features/payment/

# ✅ GOOD sequence shows TDD:
abc123 refactor: extract validation constants
def456 feat: implement amount validation
ghi789 test: add amount validation test

# ❌ BAD sequence shows no TDD:
abc123 feat: add payment validation with tests
```

### Pre-Commit Checklist

Before committing, verify:

- [ ] Production code has test demanding it
- [ ] Test verifies behavior, not implementation  
- [ ] Implementation is minimal for current test
- [ ] All tests pass
- [ ] TypeScript strict mode satisfied (or C# nullable enabled)
- [ ] No `any` types or unsafe casts
- [ ] No data mutations
- [ ] Refactoring assessment completed
- [ ] If refactored, changes committed separately

## Pull Request Standards

### PR Description Template

```markdown
## Description
Brief description of what this PR accomplishes

## Changes
- List key changes
- Focus on WHAT changed (behavior), not HOW

## Testing
- How was this tested?
- What scenarios were covered?

## TDD Compliance
- [ ] All production code written test-first
- [ ] Tests verify behavior through public APIs
- [ ] Refactoring commits separated from feature commits
```

### PR Requirements

- [ ] All tests passing
- [ ] All linting/quality checks passing
- [ ] Each commit follows conventional commits format
- [ ] Refactoring commits separate from feature commits
- [ ] No commented-out code
- [ ] No debug console.log/Console.WriteLine statements
- [ ] No TODO comments (create issues instead)

## Common TDD Violations to Avoid

### ❌ Writing Production Code Without Test

```typescript
// WRONG - Writing implementation first
export const processPayment = (payment: Payment) => {
  if (payment.amount < 0) {
    throw new Error('Invalid amount');
  }
  // ... more implementation
};

// THEN writing test
it('should reject negative amounts', () => {
  // Test written after implementation = NOT TDD
});
```

### ❌ Writing Multiple Tests Before Implementation

```typescript
// WRONG - Writing many tests at once
describe('PaymentProcessor', () => {
  it('should reject negative amounts', () => { /* ... */ });
  it('should reject zero amounts', () => { /* ... */ });
  it('should reject amounts over limit', () => { /* ... */ });
  it('should process valid payments', () => { /* ... */ });
});

// THEN implementing all at once
// This is NOT TDD - do ONE test at a time
```

### ❌ Writing More Code Than Needed

```typescript
// WRONG - Over-implementing for one test
it('should reject negative amounts', () => {
  const result = process({ amount: -100 });
  expect(result.success).toBe(false);
});

// Then implementing MORE than needed:
const process = (payment: Payment) => {
  // Only first check is needed for current test!
  if (payment.amount < 0) return fail('Amount must be positive');
  if (payment.amount === 0) return fail('Amount must be greater than zero');
  if (payment.amount > 10000) return fail('Amount too large');
  // ... even more code not demanded by test
};
```

### ✅ Correct TDD Flow

```typescript
// 1. ONE test
it('should reject negative amounts', () => {
  const result = process({ amount: -100 });
  expect(result.success).toBe(false);
});

// 2. MINIMUM to pass
const process = (payment: Payment) => {
  if (payment.amount < 0) {
    return { success: false, error: 'Amount must be positive' };
  }
  throw new Error('Not implemented');  // Everything else waits
};

// 3. Test passes - commit

// 4. NEXT test
it('should reject zero amounts', () => {
  const result = process({ amount: 0 });
  expect(result.success).toBe(false);
});

// 5. MINIMUM to pass new test
const process = (payment: Payment) => {
  if (payment.amount <= 0) {  // Changed < to <= 
    return { success: false, error: 'Amount must be positive' };
  }
  throw new Error('Not implemented');
};

// Continue this pattern...
```

## Planning Guardrail: Shared Contract Changes

When changing a **shared contract** (DTOs, API payloads, core interfaces/classes used across multiple areas), do a quick blast-radius scan *before* starting implementation:

- Identify the contract(s) being changed (properties added/renamed, nullability changes).
- Search for usages in tests/mocks to estimate how many files will need updates.
- If the change touches many tests, introduce or extend a shared test-data factory/builder so future changes are localized.

This guardrail complements TDD: it reduces avoidable compilation failures and repetitive mock edits, but it does **not** replace RED-GREEN-REFACTOR.

## Working with GitHub Copilot

When Copilot suggests code:

1. **Always start with test** - Ask Copilot to write the test first
2. **Review suggested implementation** - Ensure it's minimal for the test
3. **Verify immutability** - Check for any data mutations
4. **Check type safety** - No `any` types or unsafe casts
5. **Assess refactoring** - Only refactor if adds value

## Summary

- RED-GREEN-REFACTOR is the fundamental practice
- Write ONE test at a time
- Write MINIMUM code to pass test
- ALWAYS assess refactoring after green
- Commit after each step (especially after green, before refactor)
- Separate refactoring commits from feature commits
- Use conventional commit format
- Verify TDD compliance before PR