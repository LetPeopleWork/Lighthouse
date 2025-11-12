import '@testing-library/jest-dom';

class MockResizeObserver {
    observe() { /* Just declared to fulfill the interface */ }
    unobserve() { /* Just declared to fulfill the interface */ }
    disconnect() { /* Just declared to fulfill the interface */ }
  }
  
  global.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;

// Mock CSS imports to avoid CSS parsing errors in tests
const mockCSS = new Proxy(
  {},
  {
    get: () => ({}),
  }
);

// This will be used if Vitest tries to import CSS files
export default mockCSS;