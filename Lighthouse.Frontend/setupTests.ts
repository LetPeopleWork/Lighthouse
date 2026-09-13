import '@testing-library/jest-dom';
import userEvent from '@testing-library/user-event';

// The UI formats dates and numbers with toLocaleDateString() and friends, which follow whatever
// regional format the machine is set to — that is deliberate, so each user sees their own format.
// The tests, though, assert one concrete format ("1/31/2025"), so on a machine set to anything but
// US English the suite fails on formatting alone and nothing else. Pin the default here, the same
// way the test script pins the timezone. It cannot be done with an environment variable: Node on
// Windows takes its locale from the operating system and ignores LANG and LC_ALL.
const TEST_LOCALE = "en-US";

type LocaleMethod = (locales?: unknown, options?: unknown) => string;

const withDefaultLocale = (original: LocaleMethod): LocaleMethod =>
  function (this: unknown, locales?: unknown, options?: unknown) {
    return original.call(this, locales ?? TEST_LOCALE, options);
  };

Date.prototype.toLocaleDateString = withDefaultLocale(
  Date.prototype.toLocaleDateString as LocaleMethod,
) as Date["toLocaleDateString"];
Date.prototype.toLocaleTimeString = withDefaultLocale(
  Date.prototype.toLocaleTimeString as LocaleMethod,
) as Date["toLocaleTimeString"];
Date.prototype.toLocaleString = withDefaultLocale(
  Date.prototype.toLocaleString as LocaleMethod,
) as Date["toLocaleString"];
Number.prototype.toLocaleString = withDefaultLocale(
  Number.prototype.toLocaleString as LocaleMethod,
) as typeof Number.prototype.toLocaleString;

const OriginalDateTimeFormat = Intl.DateTimeFormat;
Intl.DateTimeFormat = new Proxy(OriginalDateTimeFormat, {
  construct: (target, [locales, options]) =>
    new target(locales ?? TEST_LOCALE, options),
  apply: (target, _thisArg, [locales, options]) =>
    target(locales ?? TEST_LOCALE, options),
});

const OriginalNumberFormat = Intl.NumberFormat;
Intl.NumberFormat = new Proxy(OriginalNumberFormat, {
  construct: (target, [locales, options]) =>
    new target(locales ?? TEST_LOCALE, options),
  apply: (target, _thisArg, [locales, options]) =>
    target(locales ?? TEST_LOCALE, options),
});

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

// jsdom exposes localStorage as a getter-only accessor on the window object, so a
// plain assignment throws. Redefining the property replaces it outright.
Object.defineProperty(globalThis, "localStorage", {
  value: localStorageMock as Storage,
  writable: true,
  configurable: true,
});

// Mock CSS imports to avoid CSS parsing errors in tests
const mockCSS = new Proxy(
  {},
  {
    get: () => ({}),
  }
);

// This will be used if Vitest tries to import CSS files
export default mockCSS;