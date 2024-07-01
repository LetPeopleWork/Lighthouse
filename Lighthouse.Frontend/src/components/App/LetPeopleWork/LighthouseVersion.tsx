import React, { useState } from "react";
import { IApiService } from "../../../services/Api/IApiService";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import LoadingAnimation from "../../Common/LoadingAnimation/LoadingAnimation";

const LighthouseVersion: React.FC = () => {
    const [version, setVersion] = useState<string>();

    const fetchVersionData = async () => {
        const apiService: IApiService = ApiServiceProvider.getApiService();
        const versionData = await apiService.getVersion();
        setVersion(versionData)
    }

    return (
        <LoadingAnimation asyncFunction={fetchVersionData}>
            <a href={`https://github.com/LetPeopleWork/Lighthouse/releases/tag/${version}`}>{version}</a>
        </LoadingAnimation>
    );
}

export default LighthouseVersion;