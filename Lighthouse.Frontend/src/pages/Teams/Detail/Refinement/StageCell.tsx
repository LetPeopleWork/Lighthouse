import { Stack, Typography } from "@mui/material";
import type React from "react";
import type { RefinementStage } from "../../../../models/Refinement/Refinement";
import { STAGE_WORDS } from "./stageWording";

interface StageCellProps {
	stage?: RefinementStage | null;
}

const StageCell: React.FC<Readonly<StageCellProps>> = ({ stage }) => {
	if (!stage) {
		return null;
	}

	return (
		<Stack direction="row" sx={{ alignItems: "center", height: "100%" }}>
			<Typography variant="body2">{STAGE_WORDS[stage]}</Typography>
		</Stack>
	);
};

export default StageCell;
