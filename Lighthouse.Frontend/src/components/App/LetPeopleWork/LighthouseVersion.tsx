import React, { useState, useEffect } from "react";
import { Button, IconButton, Tooltip } from '@mui/material';
import { IApiService } from "../../../services/Api/IApiService";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import LoadingAnimation from "../../Common/LoadingAnimation/LoadingAnimation";
import { Link } from "react-router-dom";
import UpdateIcon from '@mui/icons-material/Update';
import LatestReleaseInformationDialog from "./LatestReleaseInformationDialog";
import { IRelease } from "../../../models/Release/Release";

const LighthouseVersion: React.FC = () => {
  const [version, setVersion] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [isUpdateAvailable, setIsUpdateAvailable] = useState(false);
  const [latestRelease, setLatestRelease] = useState<IRelease | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const apiService: IApiService = ApiServiceProvider.getApiService();
        const versionData = await apiService.getCurrentVersion();
        setVersion(versionData);

        const updateAvailable = await apiService.isUpdateAvailable();
        setIsUpdateAvailable(updateAvailable);

        if (updateAvailable) {
          const releaseData = await apiService.getLatestRelease();
          setLatestRelease(releaseData);
        }

        setIsLoading(false);
      } catch (error) {
        console.error('Error fetching version data:', error);
        setHasError(true);
      }
    };

    fetchData();
  }, []);

  const handleDialogOpen = () => {
    setIsDialogOpen(true);
  };

  const handleDialogClose = () => {
    setIsDialogOpen(false);
  };

  return (
    <LoadingAnimation isLoading={isLoading} hasError={hasError}>
      <div style={{ display: 'flex', alignItems: 'center' }}>
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
        {isUpdateAvailable && (
          <Tooltip title="New Version Available">
            <IconButton
              onClick={handleDialogOpen}
              sx={{
                marginLeft: 1,
                color: 'rgba(48, 87, 78, 1)',
                animation: 'pulse 2s infinite',
                '@keyframes pulse': {
                  '0%': { transform: 'scale(1)' },
                  '50%': { transform: 'scale(1.2)' },
                  '100%': { transform: 'scale(1)' }
                }
              }}
            >
              <UpdateIcon />
            </IconButton>
          </Tooltip>
        )}
      </div>

      <LatestReleaseInformationDialog
        open={isDialogOpen}
        onClose={handleDialogClose}
        latestRelease={latestRelease}
      />
    </LoadingAnimation>
  );
};

export default LighthouseVersion;