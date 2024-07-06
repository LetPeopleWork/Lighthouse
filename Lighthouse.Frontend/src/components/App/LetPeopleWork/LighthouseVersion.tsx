import React, { useState, useEffect } from "react";
import { Button } from '@mui/material';
import { IApiService } from "../../../services/Api/IApiService";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import LoadingAnimation from "../../Common/LoadingAnimation/LoadingAnimation";
import { Link } from "react-router-dom";

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
      <Button
        component={Link}
        to={`https://github.com/LetPeopleWork/Lighthouse/releases/tag/${version}`}
        className="nav-link"
        target="_blank"
        rel="noopener noreferrer"
        sx={{
          textDecoration: 'none',
          fontFamily: 'Quicksand, sans-serif',
          color: 'rgba(48, 87, 78, 1)',
          fontWeight: 'bold',
        }}
      >
        {version}
      </Button>
    </LoadingAnimation>
  );
};

export default LighthouseVersion;
