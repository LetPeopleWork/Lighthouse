import React, { useState, useEffect, useContext } from "react";
import { Button, IconButton, Tooltip } from '@mui/material';
import LoadingAnimation from "../../Common/LoadingAnimation/LoadingAnimation";
import { Link } from "react-router-dom";
import UpdateIcon from '@mui/icons-material/Update';
import LatestReleaseInformationDialog from "./LatestReleaseInformationDialog";
import { ILighthouseRelease } from "../../../models/LighthouseRelease/LighthouseRelease";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const LighthouseVersion: React.FC = () => {
  const [version, setVersion] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [isUpdateAvailable, setIsUpdateAvailable] = useState(false);
  const [newReleases, setNewReleases] = useState<ILighthouseRelease[]>([]);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const { versionService } = useContext(ApiServiceContext);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const versionData = await versionService.getCurrentVersion();
        setVersion(versionData);

        const updateAvailable = await versionService.isUpdateAvailable();
        setIsUpdateAvailable(updateAvailable);

        if (updateAvailable) {
          const releaseData = await versionService.getNewReleases();
          setNewReleases(releaseData);
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
        newReleases={newReleases}
      />
    </LoadingAnimation>
  );
};

export default LighthouseVersion;