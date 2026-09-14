import CloudOffIcon from "@mui/icons-material/CloudOff";
import CloudSyncIcon from "@mui/icons-material/CloudSync";
import CircularProgress from "@mui/material/CircularProgress";
import type React from "react";

interface RefreshStatusIconProps {
	isUpdating: boolean;
	hasFailed: boolean;
	failedLabel: string;
}

/**
 * What the refresh button shows about the refresh it last started. A failure has to be visible here,
 * because this icon is the only thing most people ever look at to decide whether the data in front of
 * them is current — and an icon that looks idle after a refresh broke reads as "nothing to do".
 */
const RefreshStatusIcon: React.FC<RefreshStatusIconProps> = ({
	isUpdating,
	hasFailed,
	failedLabel,
}) => {
	if (isUpdating) {
		return <CircularProgress size={24} />;
	}

	if (hasFailed) {
		return <CloudOffIcon color="error" titleAccess={failedLabel} />;
	}

	return <CloudSyncIcon />;
};

export default RefreshStatusIcon;
