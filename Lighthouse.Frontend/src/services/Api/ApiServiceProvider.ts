import { ApiService } from "./ApiService";
import { IApiService } from "./IApiService";
import { MockApiService } from "./MockApiService";

export class ApiServiceProvider {

    static getApiService(): IApiService {
        console.log(`${import.meta.env.VITE_API_SERVICE_TYPE}`)
        if (import.meta.env.VITE_API_SERVICE_TYPE === 'MOCK') {
            console.log("Using Mock API Service")
            return new MockApiService();
        } else {
            console.log("Using Real API Service")
            let baseUrl = "/api";
            if (import.meta.env.VITE_API_BASE_URL !== undefined) {
                console.log("Using custom base API URL")
                baseUrl = import.meta.env.VITE_API_BASE_URL
            }

            console.log(`Base Url: ${baseUrl}`)

            return new ApiService(baseUrl);
        }
    }
}