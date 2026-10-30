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
	readonly summary: string;
	readonly whatYouSee: readonly string[];
	readonly howToRead: readonly string[];
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
		summary: `Total scope against completed ${workItemsTerm} over time.`,
		whatYouSee: [
			`Top line: everything in this ${deliveryTerm}; rises when scope is added`,
			`Filled area: the ${workItemsTerm} done`,
			`Dashed line: scope still estimated, from ${featuresTerm} not yet broken down`,
		],
		howToRead: [
			"The gap between line and area is the remaining work",
			"Gap narrowing: closing in",
			"Gap open or widening: losing ground to added scope",
		],
		learnMoreUrl: learnMoreUrl("burnup"),
	}),

	predictability: ({
		deliveryTerm,
	}: Pick<DeliveryChartTerms, "deliveryTerm">): DeliveryChartInfo => ({
		summary: `How the forecast for this ${deliveryTerm} changed day by day.`,
		whatYouSee: [
			"How Likely?: the chance of finishing by the target date, one point per day",
			"Line colour: Risky up to 50%, Realistic up to 70%, Confident up to 85%, Certain above",
			"When?: the forecasted finish dates (50th–95th percentile) against the target date",
		],
		howToRead: [
			"How Likely? rising: the odds of making the target are improving",
			"When? lines rising: the finish is slipping later; falling: pulling in",
			"A step in the target line: the target was moved, so read jumps around it with that in mind",
		],
		learnMoreUrl: learnMoreUrl("predictability"),
	}),

	progress: ({
		featureTerm,
	}: Pick<DeliveryChartTerms, "featureTerm">): DeliveryChartInfo => ({
		summary: `Where each ${featureTerm} stands: how much is done, against how likely it is to be late.`,
		whatYouSee: [
			`One dot per ${featureTerm}: further right = more done, higher = more likely to be late`,
			"Green, amber and red zones",
			`Run replays how each ${featureTerm} moved`,
		],
		howToRead: [
			"Green is on track, amber needs watching, red needs action",
			`Drifting up into red: the ${featureTerm} is using up its buffer faster than it is making progress`,
		],
		learnMoreUrl: learnMoreUrl("delivery-progress-fever-chart"),
	}),

	featuresOverTime: ({
		workItemsTerm,
		featureTerm,
		featuresTerm,
		deliveryTerm,
	}: DeliveryChartTerms): DeliveryChartInfo => ({
		summary: `Which ${featureTerm} grew, and when.`,
		whatYouSee: [
			`One bar per day, split into a band per ${featureTerm} and sized by its ${workItemsTerm}`,
			`Hatched band: the size is still an estimate, not counted ${workItemsTerm}`,
			`The line (right-hand axis): how many ${featuresTerm} the ${deliveryTerm} held`,
		],
		howToRead: [
			`A band that suddenly grows: scope discovered inside that ${featureTerm}`,
			`A band going from hatched to solid: the ${featureTerm} was broken down`,
			`A band that ends: the ${featureTerm} left the ${deliveryTerm}`,
		],
		learnMoreUrl: learnMoreUrl("features-over-time"),
	}),
};
