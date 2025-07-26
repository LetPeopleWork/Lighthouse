import WorkIcon from "@mui/icons-material/Work";
import { IconButton, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

interface SystemWIPLimitDisplayProps {
	featureOwner: IFeatureOwner;
	hide?: boolean;
}

const SystemWIPLimitDisplay: React.FC<SystemWIPLimitDisplayProps> = ({
	featureOwner,
	hide = false,
}) => {
	const theme = useTheme();
	if (hide || !featureOwner.systemWIPLimit || featureOwner.systemWIPLimit < 1) {
		return null;
	}

	const wipLimit = featureOwner.systemWIPLimit;

	const tooltipText = `System WIP Limit: ${wipLimit} Work ${wipLimit === 1 ? "Item" : "Items"}`;

	return (
		<Tooltip title={tooltipText} arrow>
			<IconButton
				size="small"
				sx={{
					color: theme.palette.primary.main,
					"&:hover": {
						backgroundColor: "action.hover",
					},
				}}
			>
				<WorkIcon />
			</IconButton>
		</Tooltip>
	);
};

export default SystemWIPLimitDisplay;
