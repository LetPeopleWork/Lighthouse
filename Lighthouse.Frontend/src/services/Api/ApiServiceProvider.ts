import { ApiService } from "./ApiService";
import { IApiService } from "./IApiService";
import { MockApiService } from "./MockApiService";

export class ApiServiceProvider{
    private static instance: IApiService;

    static getApiService(): IApiService {
        if (!ApiServiceProvider.instance) {
            if (import.meta.env.VITE_API_SERVICE_TYPE === 'MOCK') {
                console.log("Using Mock API Service")
                ApiServiceProvider.instance = new MockApiService();
            } else {
                console.log("Using Real API Service")
                let baseUrl = "/api";
                if (import.meta.env.VITE_API_BASE_URL !== null){
                    console.log("Using custom base API URL")
                    baseUrl = import.meta.env.VITE_API_BASE_URL
                }

                console.log(`Base Url: ${baseUrl}`)

                ApiServiceProvider.instance = new ApiService(baseUrl);
            }
        }
        
        return ApiServiceProvider.instance;
    }
}