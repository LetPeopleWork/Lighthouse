import type { IWithinSleDay } from "../../../models/Metrics/WithinSleOverTime";
import { formatUtcDate } from "../../../utils/date/localDate";

export type WithinSleLines = {
	dates: string[];
	share: (number | null)[];
	target: (number | null)[];
	filledIn: boolean[];
};

function nextDay(day: string): string {
	const date = new Date(`${day}T00:00:00Z`);
	date.setUTCDate(date.getUTCDate() + 1);
	return formatUtcDate(date);
}

/**
 * One entry per calendar day from the first stored day to the last, so a day nobody recorded is a gap in both
 * lines rather than its neighbours joined across it. A day too few to judge keeps its target but has no share.
 */
export function withinSleLinesOf(
	days: readonly IWithinSleDay[],
): WithinSleLines {
	const byDay = new Map(days.map((day) => [day.recordedAt.slice(0, 10), day]));
	const stored = [...byDay.keys()].sort((a, b) => a.localeCompare(b));
	const lines: WithinSleLines = {
		dates: [],
		share: [],
		target: [],
		filledIn: [],
	};
	if (stored.length === 0) return lines;

	const last = stored.at(-1) as string;
	for (let day = stored[0]; day <= last; day = nextDay(day)) {
		const answered = byDay.get(day);
		lines.dates.push(day);
		lines.share.push(answered?.sharePercent ?? null);
		lines.target.push(answered?.slePercentile ?? null);
		lines.filledIn.push(false);
	}
	return lines;
}
