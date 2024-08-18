import { ApiService } from "./ApiService";
import { DemoApiService } from "./DemoApiService";
import { IApiService } from "./IApiService";

export class ApiServiceProvider {

    private static instance : IApiService | null;

    static getApiService(): IApiService {

        if (this.instance == null) {
            if (import.meta.env.VITE_API_SERVICE_TYPE === 'DEMO') {

                const useDelay : boolean = import.meta.env.VITE_API_SERVICE_DELAY === "TRUE";

                console.log(`Using DEMO API Service - Applaying delay: ${useDelay}`)
                this.instance = new DemoApiService(useDelay);
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