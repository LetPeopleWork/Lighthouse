import { ApiService } from "./ApiService";
import { IApiService } from "./IApiService";
import { MockApiService } from "./MockApiService";

export class ApiServiceProvider {

    private static instance : IApiService | null;

    static getApiService(): IApiService {

        if (this.instance == null) {
            if (import.meta.env.VITE_API_SERVICE_TYPE === 'MOCK') {
                console.log("Using Mock API Service")
                this.instance = new MockApiService();
            } else {
                console.log("Using Real API Service")
                let baseUrl = "/api";
                if (import.meta.env.VITE_API_BASE_URL !== undefined) {
                    console.log("Using custom base API URL")
                    baseUrl = import.meta.env.VITE_API_BASE_URL
                }

                console.log(`Base Url: ${baseUrl}`)

                this.instance = new ApiService(baseUrl);
            }
        }

        return this.instance;
    }
}