import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import { IconButton, Stack, Tooltip, Typography } from "@mui/material";
import type React from "react";
import type { IYardstick } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";

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

interface YardstickQuestionProps {
	yardstick: IYardstick;
	getTerm: GetTerm;
}

const YardstickQuestion: React.FC<Readonly<YardstickQuestionProps>> = ({
	yardstick,
	getTerm,
}) => {
	const shown = withANumberOrNone(yardstick);
	const tooltip = yardstickTooltip(shown, getTerm);

	return (
		<Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
			<Typography>{yardstickQuestion(shown, getTerm)}</Typography>
			<Tooltip title={tooltip}>
				<IconButton size="small" aria-label={tooltip}>
					<InfoOutlinedIcon fontSize="small" />
				</IconButton>
			</Tooltip>
		</Stack>
	);
};

export default YardstickQuestion;
