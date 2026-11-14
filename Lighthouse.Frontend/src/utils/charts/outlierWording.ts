import { OUTLIER_MEDIAN_MULTIPLE } from "./outliers";

export interface OutlierTerms {
	readonly workItems: string;
	readonly sle: string;
	readonly cycleTime: string;
}

/** A line that lies above the axis, as the popover names it. */
export type AboveAxisLine =
	| {
			readonly kind: "percentile";
			readonly percentile: number;
			readonly value: number;
	  }
	| { readonly kind: "sle"; readonly value: number };

const plural = (count: number): string =>
	count === 1 ? "outlier" : "outliers";

/** All the words the outlier chip, its popover and the empty plot use, with the configured terms put in. */
export function outlierWording(terms: OutlierTerms) {
	const aboveAxisPhrase = (line: AboveAxisLine): string => {
		const name = line.kind === "sle" ? terms.sle : `${line.percentile}%`;
		return `${name} at ${line.value} days`;
	};

	return {
		chipLabel: (count: number) => `${count} ${plural(count)}`,
		// A count shown while the widget reloads would belong to the previous answer.
		chipLabelWhileLoading: "Outliers",
		chipAccessibleName: (count: number) =>
			`${count} ${plural(count)} not drawn on the chart. Show details.`,
		chipAccessibleNameWhileLoading: "Outliers, loading",
		popoverTitle: (count: number) => `${count} ${plural(count)}`,
		notDrawn: (cut: number, median: number) =>
			`Not drawn: longer than ${cut} days (${OUTLIER_MEDIAN_MULTIPLE} times the median, ${median} days).`,
		stillCounted: `Still counted in the percentiles, ${terms.sle} and View Data.`,
		aboveTheAxis: (lines: readonly AboveAxisLine[]) =>
			`Above the axis: ${lines.map(aboveAxisPhrase).join(", ")}.`,
		moreRows: (count: number) => `and ${count} more`,
		openInList: "Open in a list",
		dialogTitle: (cut: number) =>
			`${terms.workItems} longer than ${cut} days, not drawn on the ${terms.cycleTime} chart`,
		allVisibleAreOutliers: `All visible ${terms.workItems} are outliers.`,
		allTypesHidden: "All types are hidden.",
		columns: {
			reference: "ID",
			name: "Name",
			closed: "Closed",
			days: `${terms.cycleTime} (days)`,
			times: "Times the median",
		},
	};
}

export type OutlierWording = ReturnType<typeof outlierWording>;
