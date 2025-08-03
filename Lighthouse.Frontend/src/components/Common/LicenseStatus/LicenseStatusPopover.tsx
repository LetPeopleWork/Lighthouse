import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import InfoIcon from "@mui/icons-material/Info";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import WarningIcon from "@mui/icons-material/Warning";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import { useTheme } from "@mui/material/styles";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useContext, useRef, useState } from "react";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	errorColor,
	successColor,
	warningColor,
} from "../../../utils/theme/colors";

interface LicenseStatusPopoverProps {
	anchorEl: HTMLButtonElement | null;
	onClose: () => void;
	licenseStatus?: ILicenseStatus;
	isLoading: boolean;
	error: Error | null;
	onLicenseImported?: (newLicenseStatus: ILicenseStatus) => void;
}

const LicenseStatusPopover: React.FC<LicenseStatusPopoverProps> = ({
	anchorEl,
	onClose,
	licenseStatus,
	isLoading,
	error,
	onLicenseImported,
}) => {
	const theme = useTheme();
	const { licensingService } = useContext(ApiServiceContext);
	const fileInputRef = useRef<HTMLInputElement>(null);
	const [isUploading, setIsUploading] = useState(false);
	const [uploadError, setUploadError] = useState<string | null>(null);
	const open = Boolean(anchorEl);

	const formatDate = (date: Date) => {
		return date.toLocaleDateString(undefined, {
			year: "numeric",
			month: "long",
			day: "numeric",
		});
	};

	const handleFileUpload = async (
		event: React.ChangeEvent<HTMLInputElement>,
	) => {
		const file = event.target.files?.[0];
		if (!file) return;

		// Validate file type
		if (!file.name.toLowerCase().endsWith(".json")) {
			setUploadError("Please select a JSON file");
			return;
		}

		setIsUploading(true);
		setUploadError(null);

		try {
			const newLicenseStatus = await licensingService.importLicense(file);
			onLicenseImported?.(newLicenseStatus);
			// Clear the file input
			if (fileInputRef.current) {
				fileInputRef.current.value = "";
			}
		} catch (error) {
			// Check if this is a 400 error or license validation error
			const isLicenseValidationError =
				(error instanceof Error && error.message.includes("400")) ||
				(error instanceof Error &&
					error.message.toLowerCase().includes("license")) ||
				(error instanceof Error &&
					error.message.toLowerCase().includes("invalid"));

			if (isLicenseValidationError) {
				setUploadError(
					"License could not be loaded. Make sure you upload a valid license file that was not manually changed.",
				);
			} else {
				setUploadError(
					error instanceof Error
						? error.message
						: "Failed to upload license file",
				);
			}
		} finally {
			setIsUploading(false);
		}
	};

	const handleUploadClick = () => {
		fileInputRef.current?.click();
	};

	const handleInfoClick = () => {
		window.open(
			"https://letpeople.work/lighthouse",
			"_blank",
			"noopener,noreferrer",
		);
	};

	const getUploadButtonText = () => {
		if (isUploading) {
			return "Uploading...";
		}
		return licenseStatus?.hasLicense ? "Renew License" : "Add License";
	};

	const getStatusIcon = () => {
		if (!licenseStatus) return null;

		if (!licenseStatus.hasLicense) {
			return null; // No icon for no license state
		}

		if (!licenseStatus.isValid) {
			return <ErrorIcon style={{ color: errorColor, fontSize: 20 }} />;
		}

		// Check license expiry status
		if (licenseStatus.expiryDate) {
			const now = new Date();
			const expiryDate = new Date(licenseStatus.expiryDate);

			// Check if license has already expired
			if (expiryDate <= now) {
				return <ErrorIcon style={{ color: errorColor, fontSize: 20 }} />;
			}

			// Check if license expires within 30 days
			const thirtyDaysFromNow = new Date(
				now.getTime() + 30 * 24 * 60 * 60 * 1000,
			);
			if (expiryDate <= thirtyDaysFromNow) {
				return <WarningIcon style={{ color: warningColor, fontSize: 20 }} />;
			}
		}

		return <CheckCircleIcon style={{ color: successColor, fontSize: 20 }} />;
	};

	const renderContent = () => {
		if (isLoading) {
			return (
				<Box display="flex" alignItems="center" p={2}>
					<CircularProgress size={16} sx={{ mr: 1 }} />
					<Typography variant="body2">Loading...</Typography>
				</Box>
			);
		}

		if (error) {
			return (
				<Box p={2}>
					<Box display="flex" alignItems="center" mb={1}>
						<ErrorIcon
							style={{ color: errorColor, fontSize: 20, marginRight: 8 }}
						/>
						<Typography variant="body2" fontWeight="bold">
							Error
						</Typography>
					</Box>
					<Typography variant="body2" color="text.secondary">
						Failed to load license information
					</Typography>
				</Box>
			);
		}

		if (!licenseStatus) {
			return (
				<Box p={2}>
					<Typography variant="body2" color="text.secondary">
						License information unavailable
					</Typography>
				</Box>
			);
		}

		if (!licenseStatus.hasLicense) {
			return (
				<Box p={2}>
					<Typography variant="body2" fontWeight="bold" gutterBottom>
						No License
					</Typography>
					<Typography variant="body2" color="text.secondary">
						Lighthouse is running without a license. Premium Features are not
						enabled.
					</Typography>
				</Box>
			);
		}

		// Has license - show details
		return (
			<Box p={2} minWidth={280}>
				<Box display="flex" alignItems="center" mb={1}>
					{getStatusIcon()}
					<Typography
						variant="body2"
						fontWeight="bold"
						sx={{ ml: getStatusIcon() ? 1 : 0 }}
					>
						{(() => {
							if (!licenseStatus.isValid) return "Invalid License";
							if (
								licenseStatus.expiryDate &&
								new Date(licenseStatus.expiryDate) <= new Date()
							) {
								return "Expired";
							}
							return "Licensed";
						})()}
					</Typography>
				</Box>

				{licenseStatus.name && (
					<Typography variant="body2" gutterBottom>
						<strong>Licensed to:</strong> {licenseStatus.name}
					</Typography>
				)}

				{licenseStatus.email && (
					<Typography variant="body2" gutterBottom>
						<strong>Email:</strong> {licenseStatus.email}
					</Typography>
				)}

				{licenseStatus.organization && (
					<Typography variant="body2" gutterBottom>
						<strong>Organization:</strong> {licenseStatus.organization}
					</Typography>
				)}

				{licenseStatus.expiryDate && (
					<Typography variant="body2" gutterBottom>
						<strong>Expires:</strong> {formatDate(licenseStatus.expiryDate)}
					</Typography>
				)}

				{/* Status messages */}
				{!licenseStatus.isValid && (
					<Typography
						variant="body2"
						color="error"
						sx={{ mt: 1, fontStyle: "italic" }}
					>
						License is invalid. Premium Features will be disabled.
					</Typography>
				)}

				{licenseStatus.isValid &&
					licenseStatus.expiryDate &&
					(() => {
						const now = new Date();
						const expiryDate = new Date(licenseStatus.expiryDate);

						// Check if license has expired
						if (expiryDate <= now) {
							return (
								<Typography
									variant="body2"
									color="error"
									sx={{ mt: 1, fontStyle: "italic" }}
								>
									License has expired. Premium Features are disabled.
								</Typography>
							);
						}

						// Check if license expires within 30 days
						const thirtyDaysFromNow = new Date(
							now.getTime() + 30 * 24 * 60 * 60 * 1000,
						);
						if (expiryDate <= thirtyDaysFromNow) {
							return (
								<Typography
									variant="body2"
									color="warning.main"
									sx={{ mt: 1, fontStyle: "italic" }}
								>
									License will expire soon. Premium Features will be disabled if
									not renewed.
								</Typography>
							);
						}
						return null;
					})()}
			</Box>
		);
	};

	return (
		<Popover
			open={open}
			anchorEl={anchorEl}
			onClose={onClose}
			anchorOrigin={{
				vertical: "bottom",
				horizontal: "right",
			}}
			transformOrigin={{
				vertical: "top",
				horizontal: "right",
			}}
			data-testid="license-status-popover"
			sx={{
				"& .MuiPopover-paper": {
					backgroundColor: theme.palette.background.paper,
					border: `1px solid ${theme.palette.divider}`,
					boxShadow: theme.shadows[8],
				},
			}}
		>
			{/* Info icon in top right */}
			<Box sx={{ position: "relative" }}>
				<Tooltip title="Learn more about Lighthouse">
					<IconButton
						onClick={handleInfoClick}
						size="small"
						sx={{
							position: "absolute",
							top: 8,
							right: 8,
							zIndex: 1,
							color: theme.palette.primary.main,
							"&:hover": {
								backgroundColor: `${theme.palette.primary.main}10`,
							},
						}}
					>
						<InfoIcon />
					</IconButton>
				</Tooltip>
				{renderContent()}
			</Box>
			{/* Action buttons - always visible */}
			<Box>
				<Divider />
				<Box
					sx={{
						p: 2,
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
						gap: 1,
						minHeight: "40px",
					}}
				>
					{/* Error text on the left */}
					{uploadError && (
						<Typography
							variant="caption"
							color="error"
							sx={{
								wordWrap: "break-word",
								whiteSpace: "normal",
								maxWidth: "200px",
								lineHeight: 1.4,
								flex: 1,
							}}
						>
							{uploadError}
						</Typography>
					)}

					{/* Button on the right */}
					<Box sx={{ display: "flex", alignItems: "center" }}>
						<input
							type="file"
							ref={fileInputRef}
							onChange={handleFileUpload}
							accept=".json"
							style={{ display: "none" }}
						/>

						<Button
							variant="outlined"
							size="small"
							startIcon={
								isUploading ? (
									<CircularProgress size={16} />
								) : (
									<UploadFileIcon />
								)
							}
							onClick={handleUploadClick}
							disabled={isUploading}
							sx={{
								textTransform: "none",
								fontSize: "0.75rem",
							}}
						>
							{getUploadButtonText()}
						</Button>
					</Box>
				</Box>
			</Box>
		</Popover>
	);
};

export default LicenseStatusPopover;
