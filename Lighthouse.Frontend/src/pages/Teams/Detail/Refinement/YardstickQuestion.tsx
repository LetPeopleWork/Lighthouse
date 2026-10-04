import { Stack, Typography } from "@mui/material";
import type React from "react";
import type { IYardstick } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import InfoTooltip from "./InfoTooltip";

type GetTerm = (key: string) => string;

const NO_NUMBER: IYardstick = {
	source: "Unavailable",
	days: null,
	probability: null,
};

// Anything the browser cannot turn into a number of days is shown as no number, never as "null days".
const withANumberOrNone = (yardstick: IYardstick): IYardstick =>
	(yardstick.source === "Sle" || yardstick.source === "CycleTimeFallback") &&
	yardstick.days !== null &&
	yardstick.days > 0
		? yardstick
		: NO_NUMBER;

const inDays = (days: number | null): string =>
	days === 1 ? "1 day" : `${days} days`;

const yardstickQuestion = (yardstick: IYardstick, getTerm: GetTerm): string =>
	yardstick.source === "Unavailable"
		? `Doable within our ${getTerm(TERMINOLOGY_KEYS.SLE)}?`
		: `Doable within ${inDays(yardstick.days)}?`;

const yardstickTooltip = (yardstick: IYardstick, getTerm: GetTerm): string => {
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);

	switch (yardstick.source) {
		case "Sle":
			return `${sleTerm} ${yardstick.probability}% of ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS).toLowerCase()} in ${inDays(yardstick.days)} or less`;
		case "CycleTimeFallback":
			return `No ${sleTerm} set, based off ${yardstick.probability}% of historical ${getTerm(TERMINOLOGY_KEYS.CYCLE_TIME).toLowerCase()}`;
		case "Unavailable":
			return `No ${sleTerm} is set and no ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} have finished yet`;
	}
};

/** The question a voter answers, and where the number in it comes from. */
export interface YardstickQuestionWords {
	question: string;
	tooltip: string;
}

export const askYardstick = (
	yardstick: IYardstick,
	getTerm: GetTerm,
): YardstickQuestionWords => {
	const shown = withANumberOrNone(yardstick);
	return {
		question: yardstickQuestion(shown, getTerm),
		tooltip: yardstickTooltip(shown, getTerm),
	};
};

const YardstickQuestion: React.FC<Readonly<YardstickQuestionWords>> = ({
	question,
	tooltip,
}) => (
	<Stack
		direction="row"
		spacing={0.5}
		sx={{ alignItems: "center", minWidth: 0 }}
	>
		<Typography variant="inherit" component="span" noWrap>
			{question}
		</Typography>
		<InfoTooltip text={tooltip} />
	</Stack>
);

export default YardstickQuestion;
