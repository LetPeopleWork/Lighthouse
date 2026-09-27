import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import {
	IconButton,
	Link,
	Popover,
	Stack,
	Tooltip,
	Typography,
} from "@mui/material";
import type React from "react";
import { useState } from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	CREDIT_LEAD,
	EXPLANATION_NAME,
	FULL_MONTE,
	headlineExplanation,
	realityCheckHeadline,
} from "./realityCheckCopy";

interface RealityCheckHeadlineProps {
	result: RealityCheckResult;
}

// The explanation holds a link, so it opens on a press into a popover that takes focus, never as a
// tooltip on hover or focus, which a keyboard could not reach into.
const RealityCheckHeadline: React.FC<Readonly<RealityCheckHeadlineProps>> = ({
	result,
}) => {
	const { getTerm } = useTerminology();
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);

	return (
		<Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
			<Typography variant="subtitle1" component="h3">
				{realityCheckHeadline(result.denominator)}
			</Typography>
			<Tooltip title={EXPLANATION_NAME}>
				<IconButton
					size="small"
					aria-label={EXPLANATION_NAME}
					onClick={(event) => setAnchor(event.currentTarget)}
				>
					<InfoOutlinedIcon fontSize="small" />
				</IconButton>
			</Tooltip>
			<Popover
				open={anchor !== null}
				anchorEl={anchor}
				onClose={() => setAnchor(null)}
				anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
				slotProps={{
					paper: { role: "dialog", "aria-label": EXPLANATION_NAME },
				}}
			>
				<Stack spacing={1} sx={{ p: 2, maxWidth: 440 }}>
					{headlineExplanation(result, getTerm).map((paragraph) => (
						<Typography key={paragraph} variant="body2">
							{paragraph}
						</Typography>
					))}
					<Typography variant="body2">
						{CREDIT_LEAD}{" "}
						<Link
							href={FULL_MONTE.url}
							target="_blank"
							rel="noopener noreferrer"
						>
							{FULL_MONTE.title}
						</Link>
					</Typography>
				</Stack>
			</Popover>
		</Stack>
	);
};

export default RealityCheckHeadline;
