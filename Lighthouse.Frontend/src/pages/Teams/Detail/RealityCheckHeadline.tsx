import { Typography } from "@mui/material";
import type React from "react";
import type { RealityCheckDenominator } from "../../../models/Forecasts/RealityCheckResult";
import { realityCheckHeadline } from "./realityCheckCopy";

interface RealityCheckHeadlineProps {
	denominator: RealityCheckDenominator;
}

const RealityCheckHeadline: React.FC<Readonly<RealityCheckHeadlineProps>> = ({
	denominator,
}) => (
	<Typography variant="subtitle1" component="h3">
		{realityCheckHeadline(denominator)}
	</Typography>
);

export default RealityCheckHeadline;
