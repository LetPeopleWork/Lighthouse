import { Stack, Typography } from "@mui/material";
import type React from "react";
import InfoTooltip from "./InfoTooltip";
import {
	type CadenceHintTerms,
	describeHowToGetACadence,
} from "./nextRefinementWording";

/** Marks the text so the tab's layout can place it at the end of the heading's row. */
export const NEXT_REFINEMENT_SLOT = "data-next-refinement";

interface NextRefinementProps {
	/** "Next Refinement: Thu 8 Oct · in 4 days", or null for a Team without a cadence. */
	nextRefinement: string | null;
	/** The need message names the next Refinement as its title, so the heading's row does not repeat it. */
	titlesTheNeedMessage: boolean;
	terms: CadenceHintTerms;
	canChangeSettings: boolean;
}

/** The next Refinement, or, for a Team without a cadence, that it has none and how to get one. */
const NextRefinement: React.FC<Readonly<NextRefinementProps>> = ({
	nextRefinement,
	titlesTheNeedMessage,
	terms,
	canChangeSettings,
}) => {
	if (nextRefinement !== null) {
		return titlesTheNeedMessage ? null : (
			<Typography color="text.secondary" {...{ [NEXT_REFINEMENT_SLOT]: true }}>
				{nextRefinement}
			</Typography>
		);
	}
	return (
		<Stack
			direction="row"
			spacing={0.5}
			sx={{ alignItems: "center" }}
			{...{ [NEXT_REFINEMENT_SLOT]: true }}
		>
			<Typography color="text.secondary">{`No ${terms.refinement} cadence`}</Typography>
			<InfoTooltip text={describeHowToGetACadence(canChangeSettings, terms)} />
		</Stack>
	);
};

export default NextRefinement;
