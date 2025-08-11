import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import WarningIcon from "@mui/icons-material/Warning";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Divider from "@mui/material/Divider";
import Typography from "@mui/material/Typography";
import type React from "react";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import {
	errorColor,
	successColor,
	warningColor,
} from "../../../utils/theme/colors";

interface LicenseStatusDialogProps {
	open: boolean;
	onClose: () => void;
	licenseStatus?: ILicenseStatus;
	isLoading: boolean;
	error: Error | null;
}

const LicenseStatusDialog: React.FC<LicenseStatusDialogProps> = ({
	open,
	onClose,
	licenseStatus,
	isLoading,
	error,
}) => {
	const formatDate = (date: Date) => {
		return date.toLocaleDateString(undefined, {
			year: "numeric",
			month: "long",
			day: "numeric",
		});
	};

	const getStatusIcon = () => {
		if (!licenseStatus) return null;

		if (!licenseStatus.hasLicense || !licenseStatus.isValid) {
			return <ErrorIcon style={{ color: errorColor }} />;
		}

		// Check if license expires within 30 days
		if (licenseStatus.expiryDate) {
			const now = new Date();
			const thirtyDaysFromNow = new Date(
				now.getTime() + 30 * 24 * 60 * 60 * 1000,
			);
			if (licenseStatus.expiryDate <= thirtyDaysFromNow) {
				return <WarningIcon style={{ color: warningColor }} />;
			}
		}

		return <CheckCircleIcon style={{ color: successColor }} />;
	};

	const getStatusMessage = () => {
		if (!licenseStatus) return null;

		if (!licenseStatus.hasLicense) {
			return (
				<Alert severity="error" sx={{ mb: 2 }}>
					No license found. Please contact your administrator to obtain a valid
					license.
				</Alert>
			);
		}

		if (!licenseStatus.isValid) {
			return (
				<Alert severity="error" sx={{ mb: 2 }}>
					The current license is invalid. Please contact your administrator to
					resolve this issue.
				</Alert>
			);
		}

		// Check if license expires within 30 days
		if (licenseStatus.expiryDate) {
			const now = new Date();
			const thirtyDaysFromNow = new Date(
				now.getTime() + 30 * 24 * 60 * 60 * 1000,
			);
			if (licenseStatus.expiryDate <= thirtyDaysFromNow) {
				return (
					<Alert severity="warning" sx={{ mb: 2 }}>
						Your license will expire soon. Please contact your administrator to
						renew it.
					</Alert>
				);
			}
		}

		return (
			<Alert severity="success" sx={{ mb: 2 }}>
				Your license is valid and active.
			</Alert>
		);
	};

	const renderContent = () => {
		if (isLoading) {
			return (
				<Box display="flex" justifyContent="center" alignItems="center" p={4}>
					<CircularProgress />
					<Typography variant="body1" sx={{ ml: 2 }}>
						Loading license information...
					</Typography>
				</Box>
			);
		}

		if (error) {
			return (
				<Alert severity="error">
					<Typography variant="body1">
						Failed to load license information: {error.message}
					</Typography>
				</Alert>
			);
		}

		if (!licenseStatus) {
			return (
				<Alert severity="warning">
					<Typography variant="body1">
						License information is not available.
					</Typography>
				</Alert>
			);
		}

		return (
			<>
				{getStatusMessage()}

				<Box display="flex" alignItems="center" mb={2}>
					{getStatusIcon()}
					<Typography variant="h6" sx={{ ml: 1 }}>
						License Status
					</Typography>
				</Box>

				<Divider sx={{ mb: 2 }} />

				<Box>
					<Typography variant="body2" color="text.secondary" gutterBottom>
						Has License
					</Typography>
					<Typography variant="body1" sx={{ mb: 2 }}>
						{licenseStatus.hasLicense ? "Yes" : "No"}
					</Typography>

					<Typography variant="body2" color="text.secondary" gutterBottom>
						Valid
					</Typography>
					<Typography variant="body1" sx={{ mb: 2 }}>
						{licenseStatus.isValid ? "Yes" : "No"}
					</Typography>

					{licenseStatus.hasLicense && licenseStatus.name && (
						<>
							<Typography variant="body2" color="text.secondary" gutterBottom>
								Licensed To
							</Typography>
							<Typography variant="body1" sx={{ mb: 2 }}>
								{licenseStatus.name}
							</Typography>
						</>
					)}

					{licenseStatus.hasLicense && licenseStatus.email && (
						<>
							<Typography variant="body2" color="text.secondary" gutterBottom>
								Email
							</Typography>
							<Typography variant="body1" sx={{ mb: 2 }}>
								{licenseStatus.email}
							</Typography>
						</>
					)}

					{licenseStatus.hasLicense && licenseStatus.organization && (
						<>
							<Typography variant="body2" color="text.secondary" gutterBottom>
								Organization
							</Typography>
							<Typography variant="body1" sx={{ mb: 2 }}>
								{licenseStatus.organization}
							</Typography>
						</>
					)}

					{licenseStatus.hasLicense && licenseStatus.licenseNumber && (
						<>
							<Typography variant="body2" color="text.secondary" gutterBottom>
								License Number
							</Typography>
							<Typography variant="body1" sx={{ mb: 2 }}>
								{licenseStatus.licenseNumber}
							</Typography>
						</>
					)}

					{licenseStatus.hasLicense && licenseStatus.expiryDate && (
						<>
							<Typography variant="body2" color="text.secondary" gutterBottom>
								Expiry Date
							</Typography>
							<Typography variant="body1" sx={{ mb: 2 }}>
								{formatDate(licenseStatus.expiryDate)}
							</Typography>
						</>
					)}
				</Box>
			</>
		);
	};

	return (
		<Dialog
			open={open}
			onClose={onClose}
			maxWidth="sm"
			fullWidth
			data-testid="license-status-dialog"
		>
			<DialogTitle>License Information</DialogTitle>
			<DialogContent>{renderContent()}</DialogContent>
			<DialogActions>
				<Button onClick={onClose} variant="contained">
					Close
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default LicenseStatusDialog;
