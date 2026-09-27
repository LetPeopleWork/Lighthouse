import { Stack } from "@mui/material";
import type React from "react";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";

interface RealityCheckLevelLabelProps {
	confidenceLevel: number;
}

// The name and icon say which confidence level this is, never how well it did, so the level's colour
// stays out of the label.
const RealityCheckLevelLabel: React.FC<
	Readonly<RealityCheckLevelLabelProps>
> = ({ confidenceLevel }) => {
	const { level, IconComponent } = new ForecastLevel(confidenceLevel);
	return (
		<Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
			<span>{`${confidenceLevel}th `}</span>
			<IconComponent fontSize="small" aria-hidden="true" />
			<span>{level}</span>
		</Stack>
	);
};

export default RealityCheckLevelLabel;
