import { describe, test, expect, beforeEach } from 'vitest';
import { ApiServiceProvider } from './ApiServiceProvider';
import { MockApiService } from './MockApiService';
import { ApiService } from './ApiService';

describe('ApiServiceProvider', () => {
  beforeEach(() => {
    import.meta.env.VITE_API_SERVICE_TYPE = undefined;
    ApiServiceProvider['instance'] = null;
  });

  test('returns MockApiService instance when VITE_API_SERVICE_TYPE is DEMO', () => {
    import.meta.env.VITE_API_SERVICE_TYPE = "DEMO";

    const apiService = ApiServiceProvider.getApiService();

    expect(apiService).toBeInstanceOf(MockApiService);
  });

  test('returns ApiService instance with default base URL when VITE_API_SERVICE_TYPE is not DEMO', () => {
    import.meta.env.VITE_API_SERVICE_TYPE = undefined;

    const apiService = ApiServiceProvider.getApiService();

    expect(apiService).toBeInstanceOf(ApiService);
  });
});
