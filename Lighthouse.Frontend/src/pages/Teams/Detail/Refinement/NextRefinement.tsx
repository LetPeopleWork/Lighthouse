import { Typography } from "@mui/material";
import type React from "react";
import { describeNextRefinement } from "./nextRefinementWording";

/** Marks the text so the tab's layout can place it at the end of the heading's row. */
export const NEXT_REFINEMENT_SLOT = "data-next-refinement";

interface NextRefinementProps {
	nextRefinementDate: string | null | undefined;
	refinementTerm: string;
}

const NextRefinement: React.FC<Readonly<NextRefinementProps>> = ({
	nextRefinementDate,
	refinementTerm,
}) => {
	const wording = describeNextRefinement(
		nextRefinementDate,
		new Date(),
		refinementTerm,
	);
	if (wording === null) {
		return null;
	}
	return (
		<Typography color="text.secondary" {...{ [NEXT_REFINEMENT_SLOT]: true }}>
			{wording}
		</Typography>
	);
};

export default NextRefinement;
