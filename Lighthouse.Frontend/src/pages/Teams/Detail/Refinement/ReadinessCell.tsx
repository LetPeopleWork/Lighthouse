import { Typography } from "@mui/material";
import type React from "react";
import type { RowReadiness } from "../../../../models/Refinement/Refinement";
import { describeReadiness } from "./voteWording";

interface ReadinessCellProps {
	readiness?: RowReadiness;
	missingVotes?: number | null;
}

const ReadinessCell: React.FC<Readonly<ReadinessCellProps>> = ({
	readiness,
	missingVotes,
}) =>
	readiness === undefined ? null : (
		<Typography variant="body2">
			{describeReadiness(readiness, missingVotes ?? null)}
		</Typography>
	);

export default ReadinessCell;
