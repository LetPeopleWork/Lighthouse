import React, { useState, useEffect } from "react";
import { IApiService } from "../../../services/Api/IApiService";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import LoadingAnimation from "../../Common/LoadingAnimation/LoadingAnimation";

const LighthouseVersion: React.FC = () => {
    const [version, setVersion] = useState<string>();
    const [isLoading, setIsLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const apiService: IApiService = ApiServiceProvider.getApiService();
                const versionData = await apiService.getVersion();
                setVersion(versionData)
                setIsLoading(false);
            } catch (error) {
                console.error('Error fetching project overview data:', error);
                setHasError(true);
            }
        };

        fetchData();
    }, []);

    return (
        <LoadingAnimation isLoading={isLoading} hasError={hasError}>
            <a href={`https://github.com/LetPeopleWork/Lighthouse/releases/tag/${version}`}>{version}</a>
        </LoadingAnimation>
    );
}

export default LighthouseVersion;