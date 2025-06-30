import UpdateIcon from "@mui/icons-material/Update";
import { Button, IconButton, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { ILighthouseRelease } from "../../../models/LighthouseRelease/LighthouseRelease";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import LoadingAnimation from "../../Common/LoadingAnimation/LoadingAnimation";
import LatestReleaseInformationDialog from "./LatestReleaseInformationDialog";

const LighthouseVersion: React.FC = () => {
	const theme = useTheme();
	const [version, setVersion] = useState<string>();
	const [isLoading, setIsLoading] = useState(true);
	const [hasError, setHasError] = useState(false);
	const [isUpdateAvailable, setIsUpdateAvailable] = useState(false);
	const [isUpdateSupported, setIsUpdateSupported] = useState(false);
	const [newReleases, setNewReleases] = useState<ILighthouseRelease[]>([]);
	const [isDialogOpen, setIsDialogOpen] = useState(false);
	const [isInstalling, setIsInstalling] = useState(false);
	const [installError, setInstallError] = useState<string | null>(null);
	const [installSuccess, setInstallSuccess] = useState(false);

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

					const updateSupported = await versionService.isUpdateSupported();
					setIsUpdateSupported(updateSupported);
				}

				setIsLoading(false);
			} catch (error) {
				console.error("Error fetching version data:", error);
				setHasError(true);
			}
		};

		fetchData();
	}, [versionService]);

	const handleInstallUpdate = async () => {
		setIsInstalling(true);
		setInstallError(null);
		setInstallSuccess(false);

		try {
			const result = await versionService.installUpdate();
			if (result) {
				setInstallSuccess(true);
				// The application will restart automatically after a successful update
				// Show a success message and wait for the backend to come back online
				await waitForBackendRestart();
			} else {
				setInstallError("Update installation failed. Please try again.");
			}
		} catch (error) {
			console.error("Error installing update:", error);
			setInstallError("An error occurred while installing the update.");
		} finally {
			setIsInstalling(false);
		}
	};

	const waitForBackendRestart = async () => {
		// Wait for the backend to restart by polling the version endpoint
		const maxAttempts = 60; // 2 minutes
		const delay = 2000; // 2 seconds

		for (let i = 0; i < maxAttempts; i++) {
			try {
				await new Promise((resolve) => setTimeout(resolve, delay));
				await versionService.getCurrentVersion();
				// If we get here, the backend is back online
				window.location.reload();
				return;
			} catch {
				// Backend is still restarting, continue polling
			}
		}

		// If we get here, the backend didn't come back online
		setInstallError(
			"Update may have succeeded, but the application didn't restart properly. Please refresh the page.",
		);
	};

	const handleDialogOpen = () => {
		setIsDialogOpen(true);
	};

	const handleDialogClose = () => {
		setIsDialogOpen(false);
	};

	return (
		<LoadingAnimation isLoading={isLoading} hasError={hasError}>
			<div style={{ display: "flex", alignItems: "center" }}>
				<Button
					component={Link}
					to={`https://github.com/LetPeopleWork/Lighthouse/releases/tag/${version}`}
					className="nav-link"
					target="_blank"
					rel="noopener noreferrer"
					sx={{
						textDecoration: "none",
						fontFamily: "Quicksand, sans-serif",
						color: theme.palette.primary.main,
						fontWeight: "bold",
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
								color: theme.palette.primary.main,
								animation: "pulse 2s infinite",
								"@keyframes pulse": {
									"0%": { transform: "scale(1)" },
									"50%": { transform: "scale(1.2)" },
									"100%": { transform: "scale(1)" },
								},
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
				isUpdateSupported={isUpdateSupported}
				isInstalling={isInstalling}
				installError={installError}
				installSuccess={installSuccess}
				onInstallUpdate={handleInstallUpdate}
			/>
		</LoadingAnimation>
	);
};

export default LighthouseVersion;
