import { OUTLIER_MEDIAN_MULTIPLE } from "./outliers";

export interface OutlierTerms {
	readonly workItems: string;
	readonly sle: string;
	readonly cycleTime: string;
}

/** A line that lies above the axis, as the chip's accessible name states it. */
export type AboveAxisLine =
	| {
			readonly kind: "percentile";
			readonly percentile: number;
			readonly value: number;
	  }
	| { readonly kind: "sle"; readonly value: number };

export interface OutlierRuleTerms {
	readonly workItem: string;
	readonly workItems: string;
	readonly cycleTime: string;
}

/** The rule as the widget's help states it. */
export const outlierRuleSentence = ({
	workItem,
	workItems,
	cycleTime,
}: OutlierRuleTerms): string =>
	`An outlier is a closed ${workItem} whose ${cycleTime} is more than ${OUTLIER_MEDIAN_MULTIPLE} times the median ${cycleTime} of all closed ${workItems} in this date range.`;

const plural = (count: number): string =>
	count === 1 ? "outlier" : "outliers";

/** All the words the outlier chip and the empty plot use, with the configured terms put in. */
export function outlierWording(terms: OutlierTerms) {
	const aboveAxisPhrase = (line: AboveAxisLine): string => {
		const name = line.kind === "sle" ? terms.sle : `${line.percentile}%`;
		return `${name} at ${line.value} days`;
	};

	return {
		chipLabel: (count: number) => `${count} ${plural(count)}`,
		// A count shown while the widget reloads would belong to the previous answer.
		chipLabelWhileLoading: "Outliers",
		chipAccessibleName: (count: number, aboveTheAxisSentence: string | null) =>
			[
				`${count} ${plural(count)} not drawn on the chart.`,
				aboveTheAxisSentence,
				"Show the list.",
			]
				.filter((part) => part !== null)
				.join(" "),
		chipAccessibleNameWhileLoading: "Outliers, loading",
		aboveTheAxis: (lines: readonly AboveAxisLine[]) =>
			`Above the axis: ${lines.map(aboveAxisPhrase).join(", ")}.`,
		listTitle: `${terms.workItems} with a ${terms.cycleTime} more than ${OUTLIER_MEDIAN_MULTIPLE} times the median`,
		allVisibleAreOutliers: `All visible ${terms.workItems} are outliers.`,
		allTypesHidden: "All types are hidden.",
	};
}

export type OutlierWording = ReturnType<typeof outlierWording>;
