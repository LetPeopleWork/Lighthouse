import { Box, Typography } from "@mui/material";
import { format } from "date-fns";
import type React from "react";
import type {
	IReportWindow,
	IThenAndNow,
} from "../../../../models/Reports/Report";
import { parseLocalDate } from "../../../../utils/date/localDate";
import { ChangeMarkIcon } from "../panels/ChangeMarkIcon";
import { verdictColours } from "../panels/ThenNowLineParts";
import { DASH } from "../panels/thenNowComparison";

const LONG_DAY = "d MMM yyyy";

/** Read as one piece, so a page break never separates the label from its lines. */
const KEPT_WHOLE = { breakInside: "avoid" } as const;

const lastDayOf = (reportWindow: IReportWindow): string => {
	const lastDay = parseLocalDate(reportWindow.end);
	return lastDay === null ? DASH : format(lastDay, LONG_DAY);
};

/**
 * A PDF is read without Lighthouse beside it to explain it, so it says which side is frozen, what Now covers and what
 * the marks mean, with the report's own windows.
 */
const HowToReadThis: React.FC<{ readonly thenAndNow: IThenAndNow }> = ({
	thenAndNow: { thenWindow, nowWindow },
}) => (
	<Box data-how-to-read sx={KEPT_WHOLE}>
		<Typography variant="subtitle2" component="h2">
			How to read this
		</Typography>
		<Typography variant="body2">
			{`Then is frozen: the ${thenWindow.lengthDays} days to ${lastDayOf(thenWindow)}, exactly as captured on that day.`}
		</Typography>
		<Typography variant="body2">
			{`Now is the ${nowWindow.lengthDays} days up to ${lastDayOf(nowWindow)}, read when this PDF was produced.`}
		</Typography>
		<Typography variant="body2">
			Each line compares Now with Then.{" "}
			<ChangeMarkIcon kind="improved" colour={verdictColours.better} /> Improved
			and <ChangeMarkIcon kind="worse" colour={verdictColours.worse} /> Got
			worse say which way it moved (green better, red worse);{" "}
			{`${DASH} means there is no value, and the reason is printed beside it.`}
		</Typography>
	</Box>
);

export default HowToReadThis;
