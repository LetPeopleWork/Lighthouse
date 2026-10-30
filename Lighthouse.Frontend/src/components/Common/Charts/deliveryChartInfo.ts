const DETAIL_PAGE =
	"https://docs.lighthouse.letpeople.work/portfolios/detail.html";

export const DEFAULT_DELIVERY_TERMS = {
	workItemsTerm: "Work Items",
	featureTerm: "Feature",
	featuresTerm: "Features",
	deliveryTerm: "Delivery",
} as const;

export interface DeliveryChartTerms {
	readonly workItemsTerm: string;
	readonly featureTerm: string;
	readonly featuresTerm: string;
	readonly deliveryTerm: string;
}

export interface DeliveryChartInfo {
	readonly text: string;
	readonly learnMoreUrl: string;
}

const learnMoreUrl = (anchor: string): string => `${DETAIL_PAGE}#${anchor}`;

export const deliveryChartInfo = {
	burnup: ({
		workItemsTerm,
		featuresTerm,
		deliveryTerm,
	}: Pick<
		DeliveryChartTerms,
		"workItemsTerm" | "featuresTerm" | "deliveryTerm"
	>): DeliveryChartInfo => ({
		text: `Total scope against completed ${workItemsTerm} over time. The top line is everything in this ${deliveryTerm} and rises when scope is added; the filled area is what's done. The gap between them is the remaining work: a gap that narrows is closing in, a gap that stays open or widens is losing ground to added scope. A dashed line shows how much of the scope is still estimated, from ${featuresTerm} not yet broken down.`,
		learnMoreUrl: learnMoreUrl("burnup"),
	}),

	predictability: ({
		deliveryTerm,
	}: Pick<DeliveryChartTerms, "deliveryTerm">): DeliveryChartInfo => ({
		text: `How the forecast for this ${deliveryTerm} changed day by day. How Likely? shows the chance of finishing by the target date. The line's colour shows the level: Risky up to 50%, Realistic up to 70%, Confident up to 85%, Certain above that. When? shows the forecasted finish dates (50th to 95th percentile) against the target. Lines that rise mean the finish is slipping later; lines that fall mean it's pulling in.`,
		learnMoreUrl: learnMoreUrl("predictability"),
	}),

	progress: ({
		featureTerm,
	}: Pick<DeliveryChartTerms, "featureTerm">): DeliveryChartInfo => ({
		text: `Where each ${featureTerm} stands: how much of it is done, against how likely it is to be late. Green is on track, amber needs watching, red needs action. Press Run to replay how each ${featureTerm} moved. One drifting up into red is using up its buffer faster than it's making progress.`,
		learnMoreUrl: learnMoreUrl("delivery-progress-fever-chart"),
	}),

	featuresOverTime: ({
		workItemsTerm,
		featureTerm,
		featuresTerm,
		deliveryTerm,
	}: DeliveryChartTerms): DeliveryChartInfo => ({
		text: `Which ${featureTerm} grew, and when. Each day is one bar, split into one band per ${featureTerm} and sized by its ${workItemsTerm}. A band that suddenly grows is scope discovered inside that ${featureTerm}. A hatched band is still an estimate rather than counted ${workItemsTerm}. The line, on the right-hand axis, counts how many ${featuresTerm} the ${deliveryTerm} held.`,
		learnMoreUrl: learnMoreUrl("features-over-time"),
	}),
};
