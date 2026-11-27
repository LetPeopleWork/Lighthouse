import { useTheme } from "@mui/material";
import type { MarkElementProps } from "@mui/x-charts";
import { createContext, useContext } from "react";

/** Whether each day of the chart, by its index, was filled in from history rather than recorded at the time. */
export const FilledInDaysContext = createContext<readonly boolean[]>([]);

/**
 * A recorded day draws a solid marker and a day filled in from history an open one, because a filled-in day is
 * measured against today's SLE rather than the one in force at the time.
 */
export function WithinSleShareMark(props: Readonly<MarkElementProps>) {
	const filledIn = useContext(FilledInDaysContext);
	const theme = useTheme();
	const { x, y, dataIndex, color, hidden } = props;
	if (hidden) return null;

	return (
		<circle
			cx={x}
			cy={y}
			r={4}
			fill={filledIn[dataIndex] ? theme.palette.background.paper : color}
			stroke={color}
			strokeWidth={2}
		/>
	);
}
