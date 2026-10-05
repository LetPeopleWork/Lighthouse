import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Box, IconButton, Tooltip } from "@mui/material";
import type React from "react";

type WarningsIconProps = {
	warnings: readonly string[];
};

/**
 * Whether a row needs attention, and everything there is to say about why. A row either needs it or it
 * does not, so there is one icon: a second one beside the first says nothing the first did not, while
 * turning "does this row need me" into a counting exercise. A row can collect several reasons, none of
 * them more urgent than the others, and they are all read in the one place.
 */
const WarningsIcon: React.FC<WarningsIconProps> = ({ warnings }) => {
	// Two causes can word themselves the same, such as two dependencies held up for the same reason;
	// the reader learns nothing from the repeat.
	const reasons = [...new Set(warnings)];
	return (
		<Tooltip title={<WarningList warnings={reasons} />}>
			<IconButton
				size="small"
				sx={{ ml: 1 }}
				// One label carrying every reason: a screen reader announces the control once, and there is
				// no hovering to reveal the rest of them.
				aria-label={reasons.join(" ")}
				data-testid="warnings"
			>
				<WarningAmberIcon sx={{ color: "warning.main" }} />
			</IconButton>
		</Tooltip>
	);
};

// One reason reads as a sentence; several read as a list, because a run-on paragraph leaves the reader
// working out where one reason ends and the next begins.
const WarningList: React.FC<WarningsIconProps> = ({ warnings }) => {
	if (warnings.length === 1) {
		return <span>{warnings[0]}</span>;
	}

	return (
		<Box component="ul" sx={{ m: 0, pl: 2 }}>
			{warnings.map((warning) => (
				<li key={warning}>{warning}</li>
			))}
		</Box>
	);
};

export default WarningsIcon;
