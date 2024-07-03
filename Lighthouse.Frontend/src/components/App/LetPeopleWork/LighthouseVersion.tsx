import React, { useState, useEffect } from "react";
import { Box } from '@mui/material';
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
        setVersion(versionData);
        setIsLoading(false);
      } catch (error) {
        console.error('Error fetching version data:', error);
        setHasError(true);
      }
    };

    fetchData();
  }, []);

  return (
    <LoadingAnimation isLoading={isLoading} hasError={hasError}>
      <Box
        component="a"
        href={`https://github.com/LetPeopleWork/Lighthouse/releases/tag/${version}`}
        sx={{
          textDecoration: 'none',
          fontFamily: 'Quicksand, sans-serif',
          color: 'rgba(48, 87, 78, 1)',
          fontWeight: 'bold',
        }}
      >
        {version}
      </Box>
    </LoadingAnimation>
  );
}

export default LighthouseVersion;
