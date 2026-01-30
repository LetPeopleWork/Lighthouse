import '@testing-library/jest-dom';
import userEvent from '@testing-library/user-event';

// Ensure userEvent.setup defaults to no delay for faster tests
// This makes typing and other simulated interactions run synchronously in tests,
// significantly reducing the test runtime without changing existing test code.
const __origUserEventSetup = userEvent.setup.bind(userEvent);
// Override readonly type for testing convenience — cast to any to avoid TS error
;(userEvent as unknown as any).setup = (options: any = {}) => __origUserEventSetup({ delay: null, ...options });

class MockResizeObserver {
  observe() { /* Just declared to fulfill the interface */ }
  unobserve() { /* Just declared to fulfill the interface */ }
  disconnect() { /* Just declared to fulfill the interface */ }
}

global.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;

// Mock console methods to reduce stderr noise in tests
const originalConsoleError = console.error;
const originalConsoleWarn = console.warn;

beforeAll(() => {
  console.error = vi.fn();
  console.warn = vi.fn();
});

afterAll(() => {
  console.error = originalConsoleError;
  console.warn = originalConsoleWarn;
});

// Mock localStorage with actual storage behavior
const store: Record<string, string> = {};

const localStorageMock = {
  getItem: vi.fn((key: string) => store[key] || null),
  setItem: vi.fn((key: string, value: string) => {
    store[key] = value;
  }),
  removeItem: vi.fn((key: string) => {
    delete store[key];
  }),
  clear: vi.fn(() => {
    Object.keys(store).forEach(key => delete store[key]);
  }),
  get length() {
    return Object.keys(store).length;
  },
  key: vi.fn((index: number) => {
    const keys = Object.keys(store);
    return keys[index] || null;
  }),
};

global.localStorage = localStorageMock as Storage;

// Mock CSS imports to avoid CSS parsing errors in tests
const mockCSS = new Proxy(
  {},
  {
    get: () => ({}),
  }
);

// This will be used if Vitest tries to import CSS files
export default mockCSS;