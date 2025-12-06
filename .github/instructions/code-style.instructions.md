---
applyTo: "**"
---

# Code Style Guidelines

These style guidelines apply to all code in the repository (both C# and TypeScript).

## Immutability - CRITICAL RULE

**NO DATA MUTATION - EVER**

### Array Mutations (FORBIDDEN)

**TypeScript:**
```typescript
// ❌ WRONG - Array mutations
items.push(newItem);
items.pop();
items.splice(0, 1);
items.shift();
items.unshift(newItem);
items[0] = updatedItem;
items.sort();
items.reverse();

// ✅ CORRECT - Immutable alternatives
const withNew = [...items, newItem];
const withoutLast = items.slice(0, -1);
const withoutFirst = items.slice(1);
const withFirst = [newItem, ...items];
const updated = items.map((item, i) => i === 0 ? updatedItem : item);
const sorted = [...items].sort();
const reversed = [...items].reverse();
```

**C#:**
```csharp
using System.Collections.Immutable;

// ❌ WRONG - List mutations
var users = new List<User>();
users.Add(newUser);
users.Remove(oldUser);
users[0] = updatedUser;

// ✅ CORRECT - ImmutableList
var users = ImmutableList<User>.Empty;
var withNew = users.Add(newUser);
var withoutOld = users.Remove(oldUser);
var updated = users.SetItem(0, updatedUser);
```

### Object Mutations (FORBIDDEN)

**TypeScript:**
```typescript
// ❌ WRONG - Object mutations
user.name = "New Name";
delete user.email;
Object.assign(user, updates);

// ✅ CORRECT - Immutable updates
const updated = { ...user, name: "New Name" };
const { email, ...withoutEmail } = user;
const merged = { ...user, ...updates };
```

**C#:**
```csharp
// ❌ WRONG - Mutable properties
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
}

var user = new User { Name = "John" };
user.Name = "Jane";  // MUTATION!

// ✅ CORRECT - Immutable records with 'with' expressions
public record User(string Name, string Email);

var user = new User("John", "john@example.com");
var updated = user with { Name = "Jane" };
```

### Nested Structure Updates

**TypeScript:**
```typescript
// ❌ WRONG - Mutating nested structures
cart.items[0].quantity = 5;
order.shipping.address.city = "New York";

// ✅ CORRECT - Immutable nested update
const updatedCart = {
  ...cart,
  items: cart.items.map((item, i) =>
    i === 0 ? { ...item, quantity: 5 } : item
  ),
};

const updatedOrder = {
  ...order,
  shipping: {
    ...order.shipping,
    address: {
      ...order.shipping.address,
      city: "New York",
    },
  },
};
```

**C#:**
```csharp
// ✅ CORRECT - Nested 'with' expressions
var updatedCart = cart with 
{ 
    Items = cart.Items.SetItem(0, 
        cart.Items[0] with { Quantity = 5 }
    )
};

var updatedOrder = order with
{
    Shipping = order.Shipping with
    {
        Address = order.Shipping.Address with { City = "New York" }
    }
};
```

## Code Structure Rules

### Maximum 2 Levels of Nesting

```typescript
// ❌ WRONG - Deeply nested
if (user) {
  if (user.isActive) {
    if (user.hasPermission) {
      if (resource.isAvailable) {
        // 4 levels deep!
        performAction();
      }
    }
  }
}

// ✅ CORRECT - Early returns
if (!user) return;
if (!user.isActive) return;
if (!user.hasPermission) return;
if (!resource.isAvailable) return;

performAction();
```

### No Nested If/Else

```typescript
// ❌ WRONG - Nested conditionals
if (amount > 0) {
  if (amount <= 100) {
    return "small";
  } else {
    return "large";
  }
} else {
  return "invalid";
}

// ✅ CORRECT - Early returns or switch
if (amount <= 0) return "invalid";
if (amount <= 100) return "small";
return "large";

// Or for complex logic, use lookup
const getSizeCategory = (amount: number): string => {
  if (amount <= 0) return "invalid";
  if (amount <= 100) return "small";
  if (amount <= 1000) return "medium";
  return "large";
};
```

### Small, Focused Functions

```typescript
// ❌ WRONG - Large function with multiple responsibilities
const processOrder = (order: Order): ProcessedOrder => {
  // 100+ lines of code
  // Validation logic
  // Pricing logic
  // Tax calculation
  // Shipping logic
  // Email notification
  // Database update
};

// ✅ CORRECT - Composed small functions
const processOrder = (order: Order): ProcessedOrder => {
  const validated = validateOrder(order);
  const priced = calculatePricing(validated);
  const withTax = calculateTax(priced);
  const withShipping = assignShipping(withTax);
  const final = finalizeOrder(withShipping);
  
  notifyCustomer(final);
  saveToDatabase(final);
  
  return final;
};

const validateOrder = (order: Order): Order => {
  if (order.items.length === 0) {
    throw new OrderError("Order must have items");
  }
  return order;
};

const calculatePricing = (order: Order): Order => {
  const total = order.items.reduce((sum, item) => sum + item.price, 0);
  return { ...order, total };
};
```

## No Comments - Self-Documenting Code

```typescript
// ❌ WRONG - Comments explaining code
const calculateDiscount = (price: number, customer: Customer): number => {
  // Check if customer is premium
  if (customer.tier === "premium") {
    // Apply 20% discount for premium customers
    return price * 0.8;
  }
  // Regular customers get 10% discount
  return price * 0.9;
};

// ✅ CORRECT - Self-documenting through clear naming
const PREMIUM_DISCOUNT_MULTIPLIER = 0.8;
const STANDARD_DISCOUNT_MULTIPLIER = 0.9;

const isPremiumCustomer = (customer: Customer): boolean => {
  return customer.tier === "premium";
};

const calculateDiscount = (price: number, customer: Customer): number => {
  const multiplier = isPremiumCustomer(customer)
    ? PREMIUM_DISCOUNT_MULTIPLIER
    : STANDARD_DISCOUNT_MULTIPLIER;
  
  return price * multiplier;
};
```

### When Comments Are Acceptable

**TypeScript:**
```typescript
// ✅ ACCEPTABLE - JSDoc for public APIs
/**
 * Processes a payment request
 * @param request - The payment request details
 * @returns Result containing payment or error
 */
export const processPayment = (
  request: PaymentRequest
): Result<Payment, PaymentError> => {
  // Implementation
};
```

**C#:**
```csharp
// ✅ ACCEPTABLE - XML documentation
/// <summary>
/// Processes a payment request
/// </summary>
/// <param name="request">The payment request details</param>
/// <returns>Result containing payment or error</returns>
public Result<Payment, PaymentError> ProcessPayment(
    PaymentRequest request)
{
    // Implementation
}
```

## Function Parameters - Options Objects

### TypeScript

```typescript
// ❌ WRONG - Multiple positional parameters
const createUser = (
  name: string,
  email: string,
  role: string,
  isActive: boolean,
  department?: string,
  manager?: string
) => {
  // implementation
};

// Unclear at call site
createUser("John", "john@example.com", "admin", true, undefined, "jane@example.com");

// ✅ CORRECT - Options object
type CreateUserOptions = {
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  department?: string;
  manager?: string;
};

const createUser = (options: CreateUserOptions): User => {
  const { name, email, role, isActive, department, manager } = options;
  // implementation
};

// Clear at call site
createUser({
  name: "John",
  email: "john@example.com",
  role: "admin",
  isActive: true,
  manager: "jane@example.com",
});
```

### C#

```csharp
// ❌ WRONG - Multiple positional parameters
public User CreateUser(
    string name,
    string email,
    string role,
    bool isActive,
    string? department = null,
    string? manager = null)
{
    // implementation
}

// ✅ CORRECT - Options record
public record CreateUserOptions(
    string Name,
    string Email,
    string Role,
    bool IsActive,
    string? Department = null,
    string? Manager = null
);

public User CreateUser(CreateUserOptions options)
{
    // implementation
}

// Clear at call site
var user = CreateUser(new CreateUserOptions(
    Name: "John",
    Email: "john@example.com",
    Role: "admin",
    IsActive: true,
    Manager: "jane@example.com"
));
```

## Functional Programming Patterns

### Use Array Methods Over Loops

**TypeScript:**
```typescript
// ❌ WRONG - Imperative loop
const getActiveUsers = (users: User[]): User[] => {
  const result: User[] = [];
  for (let i = 0; i < users.length; i++) {
    if (users[i].isActive) {
      result.push(users[i]);
    }
  }
  return result;
};

// ✅ CORRECT - Functional approach
const getActiveUsers = (users: User[]): User[] => {
  return users.filter(user => user.isActive);
};

const calculateTotal = (orders: Order[]): number => {
  return orders
    .filter(o => o.status === 'completed')
    .map(o => o.total)
    .reduce((sum, total) => sum + total, 0);
};
```

**C#:**
```csharp
// ❌ WRONG - Imperative loop
public ImmutableList<User> GetActiveUsers(ImmutableList<User> users)
{
    var result = ImmutableList<User>.Empty;
    foreach (var user in users)
    {
        if (user.IsActive)
        {
            result = result.Add(user);
        }
    }
    return result;
}

// ✅ CORRECT - LINQ
public ImmutableList<User> GetActiveUsers(ImmutableList<User> users)
{
    return users
        .Where(user => user.IsActive)
        .ToImmutableList();
}

public decimal CalculateTotal(ImmutableList<Order> orders)
{
    return orders
        .Where(o => o.Status == OrderStatus.Completed)
        .Sum(o => o.Total);
}
```

## Naming Conventions Summary

### TypeScript
- **Components**: `PascalCase` (e.g., `UserProfile`)
- **Functions**: `camelCase` (e.g., `calculateTotal`)
- **Types**: `PascalCase` (e.g., `User`, `PaymentRequest`)
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `API_BASE_URL`)
- **Files**: `kebab-case.ts` (e.g., `user-profile.tsx`)

### C#
- **Classes/Records**: `PascalCase` (e.g., `PaymentProcessor`)
- **Interfaces**: `IPascalCase` (e.g., `IPaymentGateway`)
- **Methods**: `PascalCase` (e.g., `ProcessPayment`)
- **Properties**: `PascalCase` (e.g., `UserId`)
- **Private fields**: `_camelCase` (e.g., `_repository`)
- **Constants**: `PascalCase` (e.g., `MaxRetryAttempts`)
- **Local variables**: `camelCase` (e.g., `paymentResult`)

## Summary

- **NO data mutation** - immutable only
- Maximum 2 levels of nesting
- Early returns instead of nested if/else
- Small, focused functions (single responsibility)
- Self-documenting code (no comments)
- Options objects for multiple parameters
- Functional patterns (map, filter, reduce, LINQ)
- Clear, descriptive naming