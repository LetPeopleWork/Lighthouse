import SecurityIcon from "@mui/icons-material/Security";
import IconButton from "@mui/material/IconButton";
import { useTheme } from "@mui/material/styles";
import Tooltip from "@mui/material/Tooltip";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	errorColor,
	successColor,
	warningColor,
} from "../../../utils/theme/colors";
import LicenseStatusPopover from "./LicenseStatusPopover";

const LicenseStatusIcon: React.FC = () => {
	const theme = useTheme();
	const { licensingService } = useContext(ApiServiceContext);
	const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);

	const {
		data: licenseStatus,
		isLoading,
		error,
	} = useQuery({
		queryKey: ["licenseStatus"],
		queryFn: () => licensingService.getLicenseStatus(),
		staleTime: 1000 * 60 * 5, // 5 minutes
		refetchInterval: 1000 * 60 * 60, // 1 hour
	});

	const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
		setAnchorEl(event.currentTarget);
	};

	const handleClose = () => {
		setAnchorEl(null);
	};

	const getIconColor = () => {
		if (isLoading || error || !licenseStatus) {
			return theme.palette.text.secondary;
		}

		if (!licenseStatus.hasLicense) {
			return theme.palette.text.primary; // Neutral color for no license (acceptable state)
		}

		if (!licenseStatus.isValid) {
			return errorColor; // Red for invalid license
		}

		// Check if license expires within 30 days
		if (licenseStatus.expiryDate) {
			const now = new Date();
			const thirtyDaysFromNow = new Date(
				now.getTime() + 30 * 24 * 60 * 60 * 1000,
			);
			if (licenseStatus.expiryDate <= thirtyDaysFromNow) {
				return warningColor;
			}
		}

		return successColor;
	};

	const getTooltipText = () => {
		if (isLoading) {
			return "Loading license status...";
		}

		if (error) {
			return "Error loading license status";
		}

		if (!licenseStatus) {
			return "License status unknown";
		}

		if (!licenseStatus.hasLicense) {
			return "No license - Click for details";
		}

		if (!licenseStatus.isValid) {
			return "Invalid license - Click for details";
		}

		if (licenseStatus.expiryDate) {
			const now = new Date();
			const thirtyDaysFromNow = new Date(
				now.getTime() + 30 * 24 * 60 * 60 * 1000,
			);
			if (licenseStatus.expiryDate <= thirtyDaysFromNow) {
				return "License expires soon - Click for details";
			}
		}

		return "License valid - Click for details";
	};

	return (
		<>
			<Tooltip title={getTooltipText()} arrow>
				<IconButton
					size="large"
					color="inherit"
					onClick={handleClick}
					aria-label="License Status"
					data-testid="license-status-button"
				>
					<SecurityIcon style={{ color: getIconColor() }} />
				</IconButton>
			</Tooltip>
			<LicenseStatusPopover
				anchorEl={anchorEl}
				onClose={handleClose}
				licenseStatus={licenseStatus}
				isLoading={isLoading}
				error={error}
			/>
		</>
	);
};

export default LicenseStatusIcon;
