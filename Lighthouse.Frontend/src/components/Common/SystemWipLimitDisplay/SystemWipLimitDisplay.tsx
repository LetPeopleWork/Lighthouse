import WorkIcon from "@mui/icons-material/Work";
import { IconButton, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface SystemWIPLimitDisplayProps {
	featureOwner: IFeatureOwner;
	hide?: boolean;
}

const SystemWIPLimitDisplay: React.FC<SystemWIPLimitDisplayProps> = ({
	featureOwner,
	hide = false,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);

	if (hide || !featureOwner.systemWIPLimit || featureOwner.systemWIPLimit < 1) {
		return null;
	}

	const wipLimit = featureOwner.systemWIPLimit;

	const tooltipText = `System ${wipTerm} Limit: ${wipLimit} ${wipLimit === 1 ? workItemTerm : workItemsTerm}`;

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
