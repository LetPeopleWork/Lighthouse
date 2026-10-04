import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Stack, Tooltip, Typography } from "@mui/material";
import type React from "react";
import type { RefinementStage } from "../../../../models/Refinement/Refinement";
import { STAGE_WORDS } from "./stageWording";

export const describeDisagreement = (stage: RefinementStage): string =>
	stage === "Ready"
		? "The stage says Ready, but the votes don't agree yet."
		: `The votes say Ready, but the stage is still ${STAGE_WORDS[stage]}.`;

interface StageCellProps {
	stage?: RefinementStage | null;
	signalsDisagree?: boolean;
}

const StageCell: React.FC<Readonly<StageCellProps>> = ({
	stage,
	signalsDisagree,
}) => {
	if (!stage) {
		return null;
	}

	return (
		<Stack
			direction="row"
			sx={{ alignItems: "center", gap: 0.5, height: "100%" }}
		>
			<Typography variant="body2">{STAGE_WORDS[stage]}</Typography>
			{signalsDisagree && (
				<Tooltip title={describeDisagreement(stage)} describeChild>
					<WarningAmberIcon
						role="img"
						aria-label={`Stage and votes disagree. ${describeDisagreement(stage)}`}
						fontSize="small"
						sx={{ color: "warning.main" }}
					/>
				</Tooltip>
			)}
		</Stack>
	);
};

export default StageCell;
