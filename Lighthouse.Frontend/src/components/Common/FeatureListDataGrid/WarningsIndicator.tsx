import CheckIcon from "@mui/icons-material/Check";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Box, IconButton, Tooltip } from "@mui/material";
import type React from "react";
import type { IFeatureDependency } from "../../../models/FeatureDependency";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { featureWarningSentences } from "../../../utils/features/featureWarningSentences";

type WarningsIndicatorProps = {
	isDoneWithRemainingWork: boolean;
	isUsingDefaultFeatureSize: boolean;
	dependencies?: IFeatureDependency[];
};

/**
 * Whether a row needs attention, and everything there is to say about why. A row either needs it or it
 * does not, so there is one icon: a second one beside the first says nothing the first did not, while
 * turning "does this row need me" into a counting exercise. A row can collect four or five reasons,
 * none of them more urgent than the others, and they are all read in the one place.
 */
const WarningsIndicator: React.FC<WarningsIndicatorProps> = ({
	isDoneWithRemainingWork,
	isUsingDefaultFeatureSize,
	dependencies = [],
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const warnings = featureWarningSentences(
		{ isDoneWithRemainingWork, isUsingDefaultFeatureSize, dependencies },
		{ workItemsTerm, featureTerm, portfolioTerm, teamTerm },
	);

	if (warnings.length === 0) {
		return (
			<Tooltip title="No warnings">
				<IconButton
					size="small"
					sx={{ ml: 1 }}
					aria-label="No warnings"
					data-testid="no-warnings"
				>
					<CheckIcon sx={{ color: "success.main" }} />
				</IconButton>
			</Tooltip>
		);
	}

	return (
		<Tooltip title={<WarningList warnings={warnings} />}>
			<IconButton
				size="small"
				sx={{ ml: 1 }}
				// One label carrying every reason: a screen reader announces the control once, and there is
				// no hovering to reveal the rest of them.
				aria-label={warnings.join(" ")}
				data-testid="warnings"
			>
				<WarningAmberIcon sx={{ color: "warning.main" }} />
			</IconButton>
		</Tooltip>
	);
};

// One reason reads as a sentence; several read as a list, because a run-on paragraph leaves the reader
// working out where one reason ends and the next begins.
const WarningList: React.FC<{ warnings: string[] }> = ({ warnings }) => {
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

export default WarningsIndicator;
