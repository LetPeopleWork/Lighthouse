import { Typography } from "@mui/material";
import type React from "react";
import {
	type CadenceHintTerms,
	describeHowToGetACadence,
	describeNextRefinement,
} from "./nextRefinementWording";

/** Marks the text so the tab's layout can place it at the end of the heading's row. */
export const NEXT_REFINEMENT_SLOT = "data-next-refinement";

interface NextRefinementProps {
	nextRefinementDate: string | null | undefined;
	terms: CadenceHintTerms;
	canChangeSettings: boolean;
}

/** The next Refinement, or, for a Team without a cadence, how to get one. */
const NextRefinement: React.FC<Readonly<NextRefinementProps>> = ({
	nextRefinementDate,
	terms,
	canChangeSettings,
}) => {
	const wording =
		describeNextRefinement(nextRefinementDate, new Date(), terms.refinement) ??
		describeHowToGetACadence(canChangeSettings, terms);
	return (
		<Typography color="text.secondary" {...{ [NEXT_REFINEMENT_SLOT]: true }}>
			{wording}
		</Typography>
	);
};

export default NextRefinement;
