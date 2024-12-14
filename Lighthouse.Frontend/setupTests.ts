import '@testing-library/jest-dom';
import { registerLicense } from '@syncfusion/ej2-base';

registerLicense('Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWH1fdHRTQmFcVENwXUo=');

class MockResizeObserver {
    observe() { /* Just declared to fulfill the interface */ }
    unobserve() { /* Just declared to fulfill the interface */ }
    disconnect() { /* Just declared to fulfill the interface */ }
  }
  
  global.ResizeObserver = MockResizeObserver as unknown as typeof ResizeObserver;