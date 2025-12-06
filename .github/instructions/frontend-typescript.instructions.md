---
applyTo: "**/*.ts,**/*.tsx,**/*.jsx"
---

# TypeScript/React Frontend Development Guidelines

These instructions apply to all TypeScript and React code in this repository.

## TypeScript Configuration

**Required `tsconfig.json` settings:**
```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "strictBindCallApply": true,
    "strictPropertyInitialization": true,
    "noImplicitThis": true,
    "alwaysStrict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

## Type System Rules

### No `any` Types - EVER

```typescript
// ❌ WRONG - Using any
const processData = (data: any) => {
  return data.value;
};

// ✅ CORRECT - Use unknown and narrow
const processData = (data: unknown) => {
  if (isDataWithValue(data)) {
    return data.value;
  }
  throw new Error("Invalid data");
};

// Type guard
function isDataWithValue(data: unknown): data is { value: string } {
  return typeof data === 'object' && 
         data !== null && 
         'value' in data;
}
```

### Type vs Interface

```typescript
// ✅ CORRECT - Use 'type' for data structures
type User = {
  readonly id: string;
  readonly email: string;
  readonly role: UserRole;
};

type PaymentRequest = {
  amount: number;
  currency: string;
  cardId: string;
};

// ✅ CORRECT - Use 'interface' ONLY for behavior contracts
interface Logger {
  log(message: string): void;
  error(message: string, error: Error): void;
}

interface PaymentGateway {
  processPayment(payment: Payment): Promise<PaymentResult>;
  refund(transactionId: string): Promise<RefundResult>;
}

// ❌ WRONG - Interface for data structure
interface User {
  id: string;
  email: string;
}
```

## Schema-First Development with Zod

Always define schemas first, then derive types:

```typescript
import { z } from 'zod';

// Define schemas first
const AddressSchema = z.object({
  street: z.string().min(1),
  city: z.string().min(1),
  postalCode: z.string().regex(/^\d{5}(-\d{4})?$/),
  country: z.string().length(2),
});

const PaymentRequestSchema = z.object({
  amount: z.number().positive().max(10000),
  currency: z.enum(['USD', 'EUR', 'GBP']),
  cardId: z.string().min(1),
  customerId: z.string().uuid(),
  billingAddress: AddressSchema,
});

// Derive types from schemas
type Address = z.infer<typeof AddressSchema>;
type PaymentRequest = z.infer<typeof PaymentRequestSchema>;

// Use schemas at trust boundaries
export const parsePaymentRequest = (data: unknown): PaymentRequest => {
  return PaymentRequestSchema.parse(data);
};

// Validation in API calls
const handlePaymentSubmit = async (data: unknown) => {
  try {
    const validatedRequest = PaymentRequestSchema.parse(data);
    await processPayment(validatedRequest);
  } catch (error) {
    if (error instanceof z.ZodError) {
      // Handle validation errors
      return { success: false, errors: error.errors };
    }
    throw error;
  }
};
```

### When Schemas Are Required

Use schemas when:
1. Data comes from outside the application (API requests, form inputs)
2. Data goes to users/external systems
3. Data is persisted to storage
4. You need runtime validation
5. Schema provides meaningful error messages

Use plain types when:
- Data is entirely internal
- No runtime validation needed
- Working with computed/derived values

```typescript
// ✅ Schema required - external data
const UserResponseSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  role: z.enum(['admin', 'user', 'guest']),
});

const fetchUser = async (id: string) => {
  const response = await fetch(`/api/users/${id}`);
  const data = await response.json();
  return UserResponseSchema.parse(data);  // Validate external data
};

// ❌ Schema NOT required - internal types
type CartTotal = {
  subtotal: number;
  tax: number;
  total: number;
};

type LoadingState = 
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: User }
  | { status: 'error'; error: Error };
```

## React Component Patterns

### Component Structure

```typescript
// ✅ CORRECT - Functional component with proper typing
type UserProfileProps = {
  userId: string;
  onUpdate: (user: User) => void;
};

export const UserProfile = ({ userId, onUpdate }: UserProfileProps) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    loadUser(userId);
  }, [userId]);
  
  const loadUser = async (id: string) => {
    setLoading(true);
    try {
      const userData = await fetchUser(id);
      setUser(userData);
    } finally {
      setLoading(false);
    }
  };
  
  if (loading) return <Spinner />;
  if (!user) return <ErrorMessage message="User not found" />;
  
  return (
    <div>
      <h2>{user.name}</h2>
      <button onClick={() => onUpdate(user)}>Update</button>
    </div>
  );
};
```

### Immutable State Updates

```typescript
// ❌ WRONG - Mutating state
const addItem = (item: Item) => {
  items.push(item);  // MUTATION!
  setItems(items);
};

const updateItem = (id: string, updates: Partial<Item>) => {
  const item = items.find(i => i.id === id);
  if (item) {
    item.name = updates.name;  // MUTATION!
    setItems([...items]);
  }
};

// ✅ CORRECT - Immutable updates
const addItem = (item: Item) => {
  setItems([...items, item]);
};

const updateItem = (id: string, updates: Partial<Item>) => {
  setItems(items.map(item => 
    item.id === id ? { ...item, ...updates } : item
  ));
};

const removeItem = (id: string) => {
  setItems(items.filter(item => item.id !== id));
};
```

### Custom Hooks

```typescript
// ✅ CORRECT - Custom hook with proper typing
type UseFetchResult<T> = {
  data: T | null;
  loading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
};

export const useFetch = <T,>(
  url: string,
  schema: z.ZodSchema<T>
): UseFetchResult<T> => {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  
  const fetchData = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await fetch(url);
      const json = await response.json();
      const validated = schema.parse(json);
      setData(validated);
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Unknown error'));
    } finally {
      setLoading(false);
    }
  };
  
  useEffect(() => {
    fetchData();
  }, [url]);
  
  return { data, loading, error, refetch: fetchData };
};

// Usage
const UserList = () => {
  const { data: users, loading, error } = useFetch(
    '/api/users',
    z.array(UserSchema)
  );
  
  if (loading) return <Spinner />;
  if (error) return <ErrorMessage error={error} />;
  
  return <ul>{users?.map(user => <li key={user.id}>{user.name}</li>)}</ul>;
};
```

## Testing with Vitest and React Testing Library

### Test Structure

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PaymentForm } from './PaymentForm';

describe('PaymentForm', () => {
  it('should show validation error for negative amount', async () => {
    // Arrange
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<PaymentForm onSubmit={onSubmit} />);
    
    // Act
    const amountInput = screen.getByLabelText('Amount');
    await user.type(amountInput, '-100');
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    
    // Assert
    expect(screen.getByText('Amount must be positive')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });
  
  it('should submit valid payment', async () => {
    // Arrange
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<PaymentForm onSubmit={onSubmit} />);
    
    // Act
    await user.type(screen.getByLabelText('Amount'), '100');
    await user.type(screen.getByLabelText('Card Number'), '4242424242424242');
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    
    // Assert
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          amount: 100,
          cardNumber: '4242424242424242',
        })
      );
    });
  });
});
```

### Factory Functions for Test Data

```typescript
// ✅ CORRECT - Factory functions with validation
const getMockUser = (overrides?: Partial<User>): User => {
  const baseUser = {
    id: '550e8400-e29b-41d4-a716-446655440000',
    email: 'test@example.com',
    name: 'Test User',
    role: 'user' as const,
    createdAt: new Date('2024-01-01'),
  };
  
  const userData = { ...baseUser, ...overrides };
  
  // Validate against real schema
  return UserSchema.parse(userData);
};

const getMockPaymentRequest = (
  overrides?: Partial<PaymentRequest>
): PaymentRequest => {
  const baseRequest = {
    amount: 100,
    currency: 'USD' as const,
    cardId: 'card_123',
    customerId: '550e8400-e29b-41d4-a716-446655440000',
  };
  
  const requestData = { ...baseRequest, ...overrides };
  
  return PaymentRequestSchema.parse(requestData);
};

// Usage in tests
it('should process high-value payment', () => {
  const payment = getMockPaymentRequest({ amount: 5000 });
  // Test high-value payment logic
});
```

### Component Testing Best Practices

```typescript
// ✅ CORRECT - Testing behavior through user interactions
describe('ShoppingCart', () => {
  it('should update total when adding items', async () => {
    const user = userEvent.setup();
    render(<ShoppingCart />);
    
    // Initial state
    expect(screen.getByText('Total: $0.00')).toBeInTheDocument();
    
    // Add item
    await user.click(screen.getByRole('button', { name: 'Add Item 1' }));
    
    // Verify behavior
    expect(screen.getByText('Total: $29.99')).toBeInTheDocument();
  });
  
  it('should apply discount code', async () => {
    const user = userEvent.setup();
    render(<ShoppingCart />);
    
    await user.click(screen.getByRole('button', { name: 'Add Item 1' }));
    await user.type(screen.getByLabelText('Discount Code'), 'SAVE10');
    await user.click(screen.getByRole('button', { name: 'Apply' }));
    
    expect(screen.getByText('Total: $26.99')).toBeInTheDocument();
    expect(screen.getByText('Discount: -$3.00')).toBeInTheDocument();
  });
});

// ❌ WRONG - Testing implementation details
describe('ShoppingCart', () => {
  it('should call calculateTotal method', () => {
    const spy = vi.spyOn(ShoppingCart.prototype, 'calculateTotal');
    render(<ShoppingCart />);
    expect(spy).toHaveBeenCalled();  // Testing implementation!
  });
});
```

## Functional Programming Patterns

### Array Methods Over Loops

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

const calculateTotalRevenue = (orders: Order[]): number => {
  return orders
    .filter(order => order.status === 'completed')
    .map(order => order.total)
    .reduce((sum, total) => sum + total, 0);
};
```

### Composition

```typescript
// ✅ CORRECT - Function composition
const processOrder = (order: Order): ProcessedOrder => {
  const validated = validateOrder(order);
  const withPromotions = applyPromotions(validated);
  const withTax = calculateTax(withPromotions);
  const final = assignWarehouse(withTax);
  return final;
};

// Or using pipe helper
const pipe = <T>(...fns: Array<(arg: T) => T>) => (value: T) =>
  fns.reduce((acc, fn) => fn(acc), value);

const processOrder = (order: Order): ProcessedOrder =>
  pipe(
    validateOrder,
    applyPromotions,
    calculateTax,
    assignWarehouse
  )(order);
```

## Naming Conventions

- **Components**: `PascalCase` (e.g., `UserProfile`, `PaymentForm`)
- **Functions**: `camelCase`, verb-based (e.g., `calculateTotal`, `validatePayment`)
- **Types**: `PascalCase` (e.g., `User`, `PaymentRequest`)
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `API_BASE_URL`, `MAX_RETRY_ATTEMPTS`)
- **Files**: `kebab-case.tsx` (e.g., `user-profile.tsx`, `payment-form.tsx`)

## Anti-Patterns to Avoid

```typescript
// ❌ WRONG - Nested conditionals
if (user) {
  if (user.isActive) {
    if (user.hasPermission) {
      performAction();
    }
  }
}

// ✅ CORRECT - Early returns
if (!user || !user.isActive || !user.hasPermission) {
  return;
}
performAction();

// ❌ WRONG - Large component with multiple responsibilities
export const Dashboard = () => {
  // 200+ lines of JSX
  // Multiple concerns mixed together
};

// ✅ CORRECT - Small, focused components
export const Dashboard = () => {
  return (
    <div>
      <DashboardHeader />
      <UserStats />
      <RecentActivity />
      <QuickActions />
    </div>
  );
};
```

## Summary for TypeScript/React Code

- TypeScript strict mode always enabled
- No `any` types - use `unknown` and type guards
- Schema-first with Zod at trust boundaries
- Immutable state updates in React
- Test behavior through user interactions
- Use factory functions for test data
- Functional programming patterns preferred
- Follow TDD strictly for all frontend code