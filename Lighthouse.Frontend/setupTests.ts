import '@testing-library/jest-dom';
import { registerLicense } from '@syncfusion/ej2-base';

registerLicense('ORg4AjUWIQA/Gnt2UlhhQlVMfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hTX9Td0RjUHxcc3xTQ2Bd');

class MockResizeObserver {
    observe() { /* Just declared to fulfill the interface */ }
    unobserve() { /* Just declared to fulfill the interface */ }
    disconnect() { /* Just declared to fulfill the interface */ }
  }
  
  global.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;