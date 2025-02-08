import '@testing-library/jest-dom';

class MockResizeObserver {
    observe() { /* Just declared to fulfill the interface */ }
    unobserve() { /* Just declared to fulfill the interface */ }
    disconnect() { /* Just declared to fulfill the interface */ }
  }
  
  global.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;