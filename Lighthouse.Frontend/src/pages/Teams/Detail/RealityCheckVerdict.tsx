import { Typography } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import { windowVerdict } from "./realityCheckCopy";

interface RealityCheckVerdictProps {
	result: RealityCheckResult;
}

const RealityCheckVerdict: React.FC<Readonly<RealityCheckVerdictProps>> = ({
	result,
}) => {
	const { getTerm } = useTerminology();

	return (
		<Typography variant="body1">
			{windowVerdict(
				result.teamName,
				result.sampledWindowDays,
				result.soundWindow,
				getTerm,
			)}
		</Typography>
	);
};

export default RealityCheckVerdict;
