---
applyTo: "**/*.cs,**/*.csproj"
---

# C# Backend Development Guidelines

These instructions apply to all C# backend code in this repository.

## C# Specific Requirements

### Project Configuration

**Required `.csproj` settings:**
```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningLevel>5</WarningLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

### Type Safety and Nullability

```csharp
// ✅ CORRECT - Nullable reference types enabled
public class UserService
{
    public User? FindUser(string userId)  // Explicitly nullable
    {
        return _repository.GetById(userId);
    }
    
    public User GetUserOrThrow(string userId)  // Non-nullable
    {
        return _repository.GetById(userId) 
            ?? throw new UserNotFoundException(userId);
    }
}

// ❌ WRONG - Nullable warnings ignored
#nullable disable
public class UserService
{
    public User FindUser(string userId)  // Unclear if can be null
    {
        return _repository.GetById(userId);
    }
}
```

### Immutability with Records

**Prefer record types for data structures:**

```csharp
// ✅ CORRECT - Immutable record
public record User(
    string Id,
    string Email,
    UserRole Role
);

public record PaymentRequest(
    decimal Amount,
    string Currency,
    string CardId,
    string CustomerId
);

// Use 'with' expressions for updates
var updatedUser = user with { Role = UserRole.Admin };

// ❌ WRONG - Mutable class
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; }
}
```

### Collections - Use Immutable Types

```csharp
using System.Collections.Immutable;

// ✅ CORRECT - Immutable collections
public record Order(
    string Id,
    ImmutableList<OrderItem> Items,
    decimal Total
);

public ImmutableList<User> AddUser(ImmutableList<User> users, User newUser)
{
    return users.Add(newUser);
}

// ❌ WRONG - Mutable collections
public class Order
{
    public List<OrderItem> Items { get; set; } = new();
}

public List<User> AddUser(List<User> users, User newUser)
{
    users.Add(newUser);  // MUTATION!
    return users;
}
```

### LINQ and Functional Patterns

```csharp
// ✅ CORRECT - Functional LINQ queries
public ImmutableList<OrderSummary> GetHighValueOrders(
    ImmutableList<Order> orders, 
    decimal threshold)
{
    return orders
        .Where(o => o.Total > threshold)
        .Select(o => new OrderSummary(o.Id, o.Total, o.CustomerName))
        .ToImmutableList();
}

// ✅ CORRECT - Pure functions
public decimal CalculateDiscount(Order order, decimal discountRate)
{
    return order.Total * (1 - discountRate);
}

// ❌ WRONG - Imperative mutation
public List<OrderSummary> GetHighValueOrders(List<Order> orders, decimal threshold)
{
    var result = new List<OrderSummary>();
    foreach (var order in orders)
    {
        if (order.Total > threshold)
        {
            result.Add(new OrderSummary(order.Id, order.Total, order.CustomerName));
        }
    }
    return result;
}
```

### Result Type Pattern

```csharp
// Result type for error handling
public record Result<T, TError>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public TError? Error { get; init; }
    
    public static Result<T, TError> Ok(T value) => 
        new() { Success = true, Value = value };
        
    public static Result<T, TError> Fail(TError error) => 
        new() { Success = false, Error = error };
}

// Usage
public Result<Payment, PaymentError> ProcessPayment(PaymentRequest request)
{
    if (request.Amount <= 0)
    {
        return Result<Payment, PaymentError>.Fail(
            new PaymentError("Amount must be positive"));
    }
    
    if (request.Amount > 10000)
    {
        return Result<Payment, PaymentError>.Fail(
            new PaymentError("Amount exceeds limit"));
    }
    
    var payment = ExecutePayment(request);
    return Result<Payment, PaymentError>.Ok(payment);
}
```

### Testing with xUnit/NUnit

```csharp
using Xunit;

public class PaymentProcessorTests
{
    [Fact]
    public void Should_Reject_Negative_Amount()
    {
        // Arrange
        var request = GetMockPaymentRequest(amount: -100);
        var processor = new PaymentProcessor();
        
        // Act
        var result = processor.ProcessPayment(request);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Amount must be positive", result.Error?.Message);
    }
    
    [Fact]
    public void Should_Process_Valid_Payment()
    {
        // Arrange
        var request = GetMockPaymentRequest(amount: 100);
        var processor = new PaymentProcessor();
        
        // Act
        var result = processor.ProcessPayment(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(PaymentStatus.Completed, result.Value.Status);
    }
    
    // Factory function for test data
    private PaymentRequest GetMockPaymentRequest(
        decimal? amount = null,
        string? currency = null,
        string? cardId = null)
    {
        return new PaymentRequest(
            Amount: amount ?? 100m,
            Currency: currency ?? "USD",
            CardId: cardId ?? "card_123",
            CustomerId: "cust_456"
        );
    }
}
```

### Theory Tests for Multiple Scenarios

```csharp
[Theory]
[InlineData(-100, false, "Amount must be positive")]
[InlineData(0, false, "Amount must be positive")]
[InlineData(10001, false, "Amount exceeds limit")]
[InlineData(100, true, null)]
[InlineData(10000, true, null)]
public void Should_Validate_Payment_Amount(
    decimal amount, 
    bool expectedSuccess, 
    string? expectedError)
{
    // Arrange
    var request = GetMockPaymentRequest(amount: amount);
    var processor = new PaymentProcessor();
    
    // Act
    var result = processor.ProcessPayment(request);
    
    // Assert
    Assert.Equal(expectedSuccess, result.Success);
    if (!expectedSuccess)
    {
        Assert.Equal(expectedError, result.Error?.Message);
    }
}
```

### Dependency Injection

```csharp
// ✅ CORRECT - Interface for behavior contracts
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(Payment payment);
    Task<RefundResult> RefundAsync(string transactionId);
}

// Implementation
public class StripePaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    
    public StripePaymentGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        // Implementation
    }
    
    public async Task<RefundResult> RefundAsync(string transactionId)
    {
        // Implementation
    }
}

// Registration in Program.cs or Startup.cs
services.AddScoped<IPaymentGateway, StripePaymentGateway>();
```

### Async/Await Patterns

```csharp
// ✅ CORRECT - Async all the way
public async Task<Result<Order, OrderError>> ProcessOrderAsync(
    OrderRequest request,
    CancellationToken cancellationToken = default)
{
    var validationResult = await ValidateOrderAsync(request, cancellationToken);
    if (!validationResult.Success)
    {
        return Result<Order, OrderError>.Fail(validationResult.Error!);
    }
    
    var order = await CreateOrderAsync(request, cancellationToken);
    return Result<Order, OrderError>.Ok(order);
}

// ❌ WRONG - Blocking async calls
public Result<Order, OrderError> ProcessOrder(OrderRequest request)
{
    var validationResult = ValidateOrderAsync(request).Result;  // DON'T BLOCK!
    // ...
}

// ❌ WRONG - async void (except event handlers)
public async void ProcessPayment()  // Should be async Task
{
    await _gateway.ProcessAsync();
}
```

### Pattern Matching

```csharp
// ✅ CORRECT - Modern pattern matching
public string GetStatusMessage(PaymentStatus status) => status switch
{
    PaymentStatus.Pending => "Payment is being processed",
    PaymentStatus.Completed => "Payment successful",
    PaymentStatus.Failed => "Payment failed",
    PaymentStatus.Refunded => "Payment has been refunded",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};

// ✅ CORRECT - Type pattern matching
public decimal CalculateFee(IPaymentMethod method) => method switch
{
    CreditCardPayment cc => cc.Amount * 0.029m,
    DebitCardPayment dc => dc.Amount * 0.015m,
    BankTransferPayment => 0m,
    _ => throw new NotSupportedException($"Payment method {method.GetType().Name} not supported")
};

// ✅ CORRECT - Property pattern matching
public bool IsEligibleForDiscount(User user) => user switch
{
    { Role: UserRole.Premium, AccountAge: > 365 } => true,
    { TotalSpent: > 10000 } => true,
    _ => false
};
```

### Naming Conventions

- **Classes/Records**: `PascalCase` (e.g., `PaymentProcessor`, `UserAccount`)
- **Interfaces**: `IPascalCase` (e.g., `IPaymentGateway`, `IUserRepository`)
- **Methods**: `PascalCase` with verbs (e.g., `ProcessPayment`, `ValidateUser`)
- **Properties**: `PascalCase` (e.g., `UserId`, `TotalAmount`)
- **Private fields**: `camelCase` (e.g., `httpClient`, `repository`)
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `MAX_RETRY_ATTEMPTS`)
- **Local variables**: `camelCase` (e.g., `paymentResult`, `userId`)

### Anti-Patterns to Avoid

```csharp
// ❌ WRONG - Mutable properties
public class User
{
    public string Name { get; set; }
    public void UpdateName(string newName)
    {
        Name = newName;  // Mutation!
    }
}

// ✅ CORRECT - Immutable with 'with' expression
public record User(string Name, string Email)
{
    public User UpdateName(string newName) => this with { Name = newName };
}

// ❌ WRONG - Nested conditionals
public void ProcessOrder(Order order)
{
    if (order != null)
    {
        if (order.IsValid)
        {
            if (order.HasItems)
            {
                // Process
            }
        }
    }
}

// ✅ CORRECT - Early returns
public void ProcessOrder(Order? order)
{
    if (order is null) return;
    if (!order.IsValid) return;
    if (!order.HasItems) return;
    
    // Process
}

// ❌ WRONG - Exceptions for control flow
public User? GetUser(string id)
{
    try
    {
        return _repository.GetById(id);
    }
    catch (NotFoundException)
    {
        return null;
    }
}

// ✅ CORRECT - Result type or TryGet pattern
public Result<User, UserError> GetUser(string id)
{
    var user = _repository.FindById(id);
    if (user is null)
    {
        return Result<User, UserError>.Fail(new UserNotFoundError(id));
    }
    return Result<User, UserError>.Ok(user);
}

// Or use TryGet pattern
public bool TryGetUser(string id, out User? user)
{
    user = _repository.FindById(id);
    return user is not null;
}

// ❌ WRONG - String concatenation in loops
public string BuildReport(ImmutableList<Order> orders)
{
    var report = "";
    foreach (var order in orders)
    {
        report += $"Order {order.Id}: ${order.Total}\n";  // Creates new string each iteration!
    }
    return report;
}

// ✅ CORRECT - StringBuilder or LINQ
public string BuildReport(ImmutableList<Order> orders)
{
    return string.Join("\n", 
        orders.Select(o => $"Order {o.Id}: ${o.Total}"));
}
```

### Using Records Effectively

```csharp
// ✅ Simple value object
public record Money(decimal Amount, string Currency);

// ✅ With validation in constructor
public record Email
{
    public string Value { get; init; }
    
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ArgumentException("Invalid email", nameof(value));
        
        Value = value;
    }
}

// ✅ Inheritance with records
public abstract record PaymentMethod(decimal Amount);
public record CreditCardPayment(decimal Amount, string CardNumber) : PaymentMethod(Amount);
public record BankTransferPayment(decimal Amount, string AccountNumber) : PaymentMethod(Amount);

// ✅ With computed properties
public record Order(
    string Id,
    ImmutableList<OrderItem> Items,
    decimal ShippingCost)
{
    public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
    public decimal Total => Subtotal + ShippingCost;
}
```

### Options Objects Pattern

```csharp
// ✅ CORRECT - Options record for complex parameters
public record CreateUserOptions(
    string Name,
    string Email,
    string Role,
    bool IsActive = true,
    string? Department = null,
    string? Manager = null
);

public User CreateUser(CreateUserOptions options)
{
    // Implementation
}

// Clear at call site
var user = CreateUser(new CreateUserOptions(
    Name: "John Doe",
    Email: "john@example.com",
    Role: "Developer",
    Department: "Engineering"
));

// ❌ WRONG - Many positional parameters
public User CreateUser(
    string name,
    string email,
    string role,
    bool isActive = true,
    string? department = null,
    string? manager = null)
{
    // Hard to read at call sites
}
```

## Summary for C# Code

- Enable nullable reference types and treat warnings as errors
- Use record types for immutable data structures
- Use ImmutableList/ImmutableDictionary for collections
- Prefer LINQ and functional patterns over imperative loops
- Use Result types or TryGet pattern for error handling
- Write async methods all the way through (no blocking)
- Use pattern matching for complex conditionals
- Test behavior through public APIs
- Follow TDD strictly for all C# code
- Use dependency injection for behavior contracts
- Prefer options objects for complex parameters