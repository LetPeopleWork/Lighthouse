import { Box, Stack, Typography } from "@mui/material";
import type React from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";

interface RealityCheckEvidenceProps {
	result: RealityCheckResult;
}

const samplingWindowTitle = (windowDays: number): string =>
	`Sampling window: ${windowDays} days`;

// Panels follow the server's sweep order, and every swept window gets one: a missing panel would read
// as "this window was fine".
const RealityCheckEvidence: React.FC<Readonly<RealityCheckEvidenceProps>> = ({
	result,
}) => (
	<Stack spacing={2}>
		{result.sampledWindowDays.map((windowDays) => (
			<Box
				key={windowDays}
				component="fieldset"
				aria-label={samplingWindowTitle(windowDays)}
				sx={{ border: 0, margin: 0, padding: 0, minWidth: 0 }}
			>
				<Typography variant="subtitle2">
					{samplingWindowTitle(windowDays)}
				</Typography>
			</Box>
		))}
	</Stack>
);

export default RealityCheckEvidence;
