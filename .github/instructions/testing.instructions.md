---
applyTo: "**/*.test.ts,**/*.test.tsx,**/*.spec.ts,**/*.Tests.cs"
---

# Testing Guidelines

These instructions apply to all test files in both C# and TypeScript/React.

## Core Testing Principles

**TEST-DRIVEN DEVELOPMENT IS NON-NEGOTIABLE**

1. Write the test FIRST - before any production code
2. See the test FAIL for the right reason
3. Write MINIMUM code to make test pass
4. Refactor if needed (only if adds value)

### Behavior-Driven Testing

- **Test WHAT, not HOW** - Test behavior through public APIs
- No testing implementation details (private methods, internal state)
- No 1:1 mapping between test files and implementation files
- Tests document expected business behavior
- 100% coverage through business behavior, not line-by-line

### Test Organization

**C# Structure:**
```
src/
  Features/
    Payment/
      PaymentProcessor.cs
      PaymentValidator.cs          # Implementation detail
      PaymentProcessor.Tests.cs    # Validator covered through processor
```

**TypeScript Structure:**
```
src/
  features/
    payment/
      payment-processor.ts
      payment-validator.ts          # Implementation detail
      payment-processor.test.ts     # Validator covered through processor
```

## Test Data Patterns

### Factory Functions (Both Languages)

**TypeScript:**
```typescript
// ✅ CORRECT - Factory with optional overrides
const getMockUser = (overrides?: Partial<User>): User => {
  const baseUser = {
    id: '550e8400-e29b-41d4-a716-446655440000',
    email: 'test@example.com',
    name: 'Test User',
    role: 'user' as const,
  };
  
  const userData = { ...baseUser, ...overrides };
  
  // Validate with real schema
  return UserSchema.parse(userData);
};

// Usage
it('should handle premium users', () => {
  const user = getMockUser({ role: 'premium' });
  // Test premium user behavior
});

// ❌ WRONG - Shared mutable state
let user: User;
beforeEach(() => {
  user = { id: '123', email: 'test@example.com', name: 'Test' };
});
```

**C#:**
```csharp
// ✅ CORRECT - Factory with optional parameters
private User GetMockUser(
    string? id = null,
    string? email = null,
    UserRole? role = null)
{
    return new User(
        Id: id ?? "550e8400-e29b-41d4-a716-446655440000",
        Email: email ?? "test@example.com",
        Name: "Test User",
        Role: role ?? UserRole.User
    );
}

// Usage
[Fact]
public void Should_Handle_Premium_Users()
{
    var user = GetMockUser(role: UserRole.Premium);
    // Test premium user behavior
}

// ❌ WRONG - Shared mutable state
private User? _user;

[SetUp]
public void Setup()
{
    _user = new User("123", "test@example.com", "Test", UserRole.User);
}
```

### Validating Test Data

**CRITICAL**: Always validate test data against real schemas/types.

**TypeScript:**
```typescript
import { UserSchema, type User } from '@/schemas/user';

// ✅ CORRECT - Validates against production schema
const getMockUser = (overrides?: Partial<User>): User => {
  const baseUser = {
    id: '550e8400-e29b-41d4-a716-446655440000',
    email: 'test@example.com',
    name: 'Test User',
    role: 'user' as const,
  };
  
  // This will throw if test data doesn't match schema
  return UserSchema.parse({ ...baseUser, ...overrides });
};

// ❌ WRONG - Redefining schema in tests
const TestUserSchema = z.object({
  id: z.string(),
  email: z.string(),
  // ... duplicating production schema
});
```

**C#:**
```csharp
// Import from production code, never redefine
using MyApp.Domain.Models;
using MyApp.Domain.Validation;

// ✅ CORRECT - Uses production types
private PaymentRequest GetMockPaymentRequest(
    decimal? amount = null)
{
    var request = new PaymentRequest(
        Amount: amount ?? 100m,
        Currency: "USD",
        CardId: "card_123",
        CustomerId: "cust_456"
    );
    
    // Optionally validate with production validator
    PaymentValidator.Validate(request);
    return request;
}
```

## Testing Anti-Patterns

### ❌ Testing Implementation Details

**Wrong:**
```typescript
// Testing HOW it works
it('should call validateAmount method', () => {
  const spy = jest.spyOn(processor, 'validateAmount');
  processor.process(payment);
  expect(spy).toHaveBeenCalled();
});
```

**Correct:**
```typescript
// Testing WHAT it does
it('should reject negative amounts', () => {
  const payment = getMockPayment({ amount: -100 });
  const result = processor.process(payment);
  
  expect(result.success).toBe(false);
  expect(result.error.message).toBe('Amount must be positive');
});
```

### ❌ Mocking What You Don't Own

**Wrong:**
```typescript
// Mocking external library
const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
  json: () => Promise.resolve({ data: 'test' })
});
```

**Correct:**
```typescript
// Test through your abstraction
interface HttpClient {
  get<T>(url: string): Promise<T>;
}

const mockHttpClient: HttpClient = {
  get: vi.fn().mockResolvedValue({ data: 'test' })
};
```

### ❌ Testing Framework Behavior

**Wrong:**
```typescript
it('should render without crashing', () => {
  render(<MyComponent />);
  // This just tests React works
});
```

**Correct:**
```typescript
it('should display user name', () => {
  const user = getMockUser({ name: 'John Doe' });
  render(<UserProfile user={user} />);
  
  expect(screen.getByText('John Doe')).toBeInTheDocument();
});
```

## Achieving 100% Coverage Through Behavior

**Example: Validation covered through business behavior**

**Implementation:**
```typescript
// payment-validator.ts (private implementation detail)
export const validateAmount = (amount: number): boolean => {
  return amount > 0 && amount <= 10000;
};

export const validateCard = (cardNumber: string): boolean => {
  return /^\d{16}$/.test(cardNumber);
};

// payment-processor.ts (public API)
export const processPayment = (
  request: PaymentRequest
): Result<Payment, PaymentError> => {
  if (!validateAmount(request.amount)) {
    return { success: false, error: new PaymentError('Invalid amount') };
  }
  
  if (!validateCard(request.cardNumber)) {
    return { success: false, error: new PaymentError('Invalid card') };
  }
  
  return { success: true, data: executePayment(request) };
};
```

**Tests (covers validator through behavior):**
```typescript
// payment-processor.test.ts
describe('Payment processing', () => {
  it('should reject negative amounts', () => {
    const payment = getMockPayment({ amount: -100 });
    const result = processPayment(payment);
    
    expect(result.success).toBe(false);
    expect(result.error.message).toBe('Invalid amount');
  });
  
  it('should reject amounts over limit', () => {
    const payment = getMockPayment({ amount: 10001 });
    const result = processPayment(payment);
    
    expect(result.success).toBe(false);
    expect(result.error.message).toBe('Invalid amount');
  });
  
  it('should reject invalid card numbers', () => {
    const payment = getMockPayment({ cardNumber: '123' });
    const result = processPayment(payment);
    
    expect(result.success).toBe(false);
    expect(result.error.message).toBe('Invalid card');
  });
  
  it('should process valid payments', () => {
    const payment = getMockPayment({
      amount: 100,
      cardNumber: '4242424242424242'
    });
    const result = processPayment(payment);
    
    expect(result.success).toBe(true);
    expect(result.data.status).toBe('completed');
  });
});

// Result: 100% coverage of validator without testing it directly
```

## Testing Tools

### TypeScript/React (Vitest + React Testing Library)

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

describe('Feature name', () => {
  it('should describe expected behavior', async () => {
    // Arrange
    const user = userEvent.setup();
    const mockFn = vi.fn();
    
    // Act
    render(<Component onAction={mockFn} />);
    await user.click(screen.getByRole('button'));
    
    // Assert
    expect(mockFn).toHaveBeenCalledWith(expectedValue);
  });
});
```

### C# (xUnit/NUnit)

```csharp
using Xunit;

public class PaymentProcessorTests
{
    [Fact]
    public void Should_Describe_Expected_Behavior()
    {
        // Arrange
        var request = GetMockPaymentRequest(amount: 100);
        var processor = new PaymentProcessor();
        
        // Act
        var result = processor.ProcessPayment(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, result.Value.Status);
    }
    
    [Theory]
    [InlineData(-100, false)]
    [InlineData(0, false)]
    [InlineData(100, true)]
    [InlineData(10000, true)]
    [InlineData(10001, false)]
    public void Should_Validate_Amount_Range(decimal amount, bool expectedValid)
    {
        var request = GetMockPaymentRequest(amount: amount);
        var processor = new PaymentProcessor();
        
        var result = processor.ProcessPayment(request);
        
        Assert.Equal(expectedValid, result.Success);
    }
}
```

## React Component Testing

### Test User Interactions, Not Implementation

```typescript
// ✅ CORRECT - Testing behavior
describe('LoginForm', () => {
  it('should show error for invalid email', async () => {
    const user = userEvent.setup();
    render(<LoginForm />);
    
    await user.type(screen.getByLabelText('Email'), 'invalid-email');
    await user.click(screen.getByRole('button', { name: 'Login' }));
    
    expect(screen.getByText('Invalid email address')).toBeInTheDocument();
  });
  
  it('should submit form with valid data', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<LoginForm onSubmit={onSubmit} />);
    
    await user.type(screen.getByLabelText('Email'), 'user@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.click(screen.getByRole('button', { name: 'Login' }));
    
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        email: 'user@example.com',
        password: 'password123'
      });
    });
  });
});

// ❌ WRONG - Testing implementation
it('should update email state on change', () => {
  const { rerender } = render(<LoginForm />);
  // Testing internal state is implementation detail
});
```

## TDD Compliance Checklist

Before committing any code, verify:

- [ ] Production code has test demanding it
- [ ] Test verifies behavior, not implementation
- [ ] Implementation is minimal (only what's needed)
- [ ] All tests pass
- [ ] Factory functions used (no shared mutable state)
- [ ] Real schemas/types imported, not redefined
- [ ] Test data validated against production schemas

## TDD Verification via Git History

```bash
# Check test was written before implementation
git log --oneline --follow src/features/payment/

# Good commit sequence:
# feat(test): add test for payment validation (RED)
# feat: implement payment validation (GREEN)
# refactor: extract validation constants (REFACTOR)

# Bad commit sequence:
# feat: add payment validation with tests (test not first!)
```

## Summary

- Write tests FIRST - always (TDD non-negotiable)
- Test behavior through public APIs
- Use factory functions with validation
- Import real schemas, never redefine
- No testing implementation details
- No shared mutable state in tests
- 100% coverage through business behavior
- Tests document expected behavior