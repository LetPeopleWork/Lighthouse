import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import WarningIcon from "@mui/icons-material/Warning";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Popover from "@mui/material/Popover";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import type React from "react";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
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
}

const LicenseStatusPopover: React.FC<LicenseStatusPopoverProps> = ({
	anchorEl,
	onClose,
	licenseStatus,
	isLoading,
	error,
}) => {
	const theme = useTheme();
	const open = Boolean(anchorEl);

	const formatDate = (date: Date) => {
		return date.toLocaleDateString(undefined, {
			year: "numeric",
			month: "long",
			day: "numeric",
		});
	};

	const getStatusIcon = () => {
		if (!licenseStatus) return null;

		if (!licenseStatus.hasLicense) {
			return null; // No icon for no license state
		}

		if (!licenseStatus.isValid) {
			return <ErrorIcon style={{ color: errorColor, fontSize: 20 }} />;
		}

		// Check if license expires within 30 days
		if (licenseStatus.expiryDate) {
			const now = new Date();
			const thirtyDaysFromNow = new Date(
				now.getTime() + 30 * 24 * 60 * 60 * 1000,
			);
			if (licenseStatus.expiryDate <= thirtyDaysFromNow) {
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
						Lighthouse is running without a license. Premium Features are not enabled.
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
						{licenseStatus.isValid ? "Licensed" : "Invalid License"}
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
						const thirtyDaysFromNow = new Date(
							now.getTime() + 30 * 24 * 60 * 60 * 1000,
						);
						if (licenseStatus.expiryDate <= thirtyDaysFromNow) {
							return (
								<Typography
									variant="body2"
									color="warning.main"
									sx={{ mt: 1, fontStyle: "italic" }}
								>
									License will expire soon. Premium Features will be disabled if not renewed.
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
			{renderContent()}
		</Popover>
	);
};

export default LicenseStatusPopover;
