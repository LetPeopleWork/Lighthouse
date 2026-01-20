import InfoIcon from "@mui/icons-material/Info";
import UpdateIcon from "@mui/icons-material/Update";
import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControl,
	IconButton,
	MenuItem,
	Select,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
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
	const [showAboutDialog, setShowAboutDialog] = useState(false);
	const [newReleases, setNewReleases] = useState<ILighthouseRelease[]>([]);
	const [isDialogOpen, setIsDialogOpen] = useState(false);
	const [isInstalling, setIsInstalling] = useState(false);
	const [installError, setInstallError] = useState<string | null>(null);
	const [installSuccess, setInstallSuccess] = useState(false);
	const [showRestartDialog, setShowRestartDialog] = useState(false);
	const [showUpdateNotification, setShowUpdateNotification] = useState(false);
	const [notificationPreference, setNotificationPreference] = useState("show");

	const { versionService } = useContext(ApiServiceContext);

	// localStorage utilities for managing notification preferences
	const getNotificationKey = useCallback(
		(version: string) => `lighthouse-hide-update-notification-${version}`,
		[],
	);

	const getGlobalNotificationKey = useCallback(
		() => "lighthouse-hide-all-update-notifications",
		[],
	);

	const shouldShowNotification = useCallback(
		(version: string): boolean => {
			if (!version) return false;

			// Check if all notifications are disabled globally
			const globalKey = getGlobalNotificationKey();
			if (localStorage.getItem(globalKey) === "true") {
				return false;
			}

			// Check if this specific version notification is disabled
			const versionKey = getNotificationKey(version);
			return localStorage.getItem(versionKey) !== "true";
		},
		[getNotificationKey, getGlobalNotificationKey],
	);

	const hideNotificationForVersion = useCallback(
		(version: string) => {
			const key = getNotificationKey(version);
			localStorage.setItem(key, "true");
		},
		[getNotificationKey],
	);

	const hideAllNotifications = useCallback(() => {
		const key = getGlobalNotificationKey();
		localStorage.setItem(key, "true");
	}, [getGlobalNotificationKey]);

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

					// Show notification popup if user hasn't opted out for this version
					if (
						releaseData.length > 0 &&
						versionData !== "DEV" &&
						shouldShowNotification(releaseData[0].version)
					) {
						setShowUpdateNotification(true);
					}
				}

				setIsLoading(false);
			} catch (error) {
				console.error("Error fetching version data:", error);
				setHasError(true);
			}
		};

		fetchData();
	}, [versionService, shouldShowNotification]);

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
				setShowRestartDialog(true);
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

	const handleRestartConfirm = () => {
		if (globalThis.window !== undefined) {
			globalThis.location.reload();
		}
	};

	const handleDialogOpen = () => {
		setIsDialogOpen(true);
	};

	const handleDialogClose = () => {
		setIsDialogOpen(false);
	};

	const handleNotificationClose = () => {
		if (
			notificationPreference === "dontShowVersion" &&
			newReleases.length > 0
		) {
			hideNotificationForVersion(newReleases[0].version);
		} else if (notificationPreference === "dontShowAll") {
			hideAllNotifications();
		}
		setShowUpdateNotification(false);
		setNotificationPreference("show");
	};

	const handleShowDetails = () => {
		setShowUpdateNotification(false);
		setIsDialogOpen(true);
		setNotificationPreference("show");
	};

	const handleAboutOpen = (e: React.MouseEvent) => {
		e.preventDefault();
		setShowAboutDialog(true);
	};

	const handleAboutClose = () => {
		setShowAboutDialog(false);
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
				<Tooltip title="About Lighthouse">
					<IconButton
						onClick={handleAboutOpen}
						size="small"
						sx={{ marginLeft: 0.5, color: theme.palette.primary.main }}
					>
						<InfoIcon fontSize="small" />
					</IconButton>
				</Tooltip>
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

			<Dialog open={showUpdateNotification} maxWidth="sm" fullWidth>
				<DialogTitle>New Version Available!</DialogTitle>
				<DialogContent>
					<Typography variant="body1" gutterBottom>
						{newReleases.length > 0 && (
							<>
								A new version of Lighthouse is available:{" "}
								<strong>{newReleases[0].version}</strong>
							</>
						)}
					</Typography>
					<Typography variant="body2" color="text.secondary" gutterBottom>
						Would you like to see the details of this update?
					</Typography>
				</DialogContent>
				<DialogActions
					sx={{ justifyContent: "space-between", alignItems: "center", p: 2 }}
				>
					<FormControl size="small" sx={{ minWidth: 180 }}>
						<Typography
							variant="caption"
							color="text.secondary"
							sx={{ mb: 0.5, fontSize: "0.7rem" }}
						>
							Notification preference:
						</Typography>
						<Select
							value={notificationPreference}
							onChange={(e) => setNotificationPreference(e.target.value)}
							variant="outlined"
							size="small"
							sx={{ fontSize: "0.8rem" }}
						>
							<MenuItem value="show" sx={{ fontSize: "0.8rem" }}>
								Show future notifications
							</MenuItem>
							<MenuItem value="dontShowVersion" sx={{ fontSize: "0.8rem" }}>
								Don't show for this version
							</MenuItem>
							<MenuItem value="dontShowAll" sx={{ fontSize: "0.8rem" }}>
								Don't show any more updates
							</MenuItem>
						</Select>
					</FormControl>
					<div style={{ display: "flex", gap: 8 }}>
						<Button onClick={handleNotificationClose}>Close</Button>
						<Button onClick={handleShowDetails} variant="contained" autoFocus>
							Show Details
						</Button>
					</div>
				</DialogActions>
			</Dialog>

			<Dialog open={showRestartDialog} maxWidth="sm" fullWidth>
				<DialogTitle>Update Complete!</DialogTitle>
				<DialogContent>
					<Typography variant="body1">
						The update has been successfully installed and the application has
						restarted. Click "Reload Page" to refresh the application with the
						new version.
					</Typography>
				</DialogContent>
				<DialogActions>
					<Button
						onClick={handleRestartConfirm}
						color="primary"
						variant="contained"
						autoFocus
					>
						Reload Page
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={showAboutDialog}
				onClose={handleAboutClose}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>About Lighthouse</DialogTitle>
				<DialogContent>
					<Typography variant="body1" gutterBottom>
						<strong>Lighthouse {version}</strong>
					</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
						Flow metrics, predictability, and Monte Carlo forecasting tool for
						software delivery
					</Typography>

					<Typography variant="body2" sx={{ mb: 1, mt: 2 }}>
						<strong>CE Marking</strong>
					</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
						CE | EU Cyber Resilience Act Conformant
					</Typography>

					<Typography variant="body2" sx={{ mb: 1 }}>
						<strong>Compliance</strong>
					</Typography>
					<Typography variant="body2" color="text.secondary">
						This product conforms to EU Regulation 2024/2847 (Cyber Resilience
						Act).{" "}
						<Button
							component={Link}
							to="https://docs.lighthouse.letpeople.work/compliance/declaration-of-conformity.html"
							target="_blank"
							rel="noopener noreferrer"
							size="small"
							sx={{ textTransform: "none", p: 0, minWidth: 0 }}
						>
							Declaration of Conformity
						</Button>
					</Typography>

					<Typography variant="body2" sx={{ mt: 2 }} color="text.secondary">
						© 2025 LetPeopleWork GmbH. Licensed under MIT License.
					</Typography>
				</DialogContent>
				<DialogActions>
					<Button onClick={handleAboutClose} variant="contained">
						Close
					</Button>
				</DialogActions>
			</Dialog>
		</LoadingAnimation>
	);
};

export default LighthouseVersion;
