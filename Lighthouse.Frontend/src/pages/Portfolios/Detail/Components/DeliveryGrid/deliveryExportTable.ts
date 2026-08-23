import type { DataGridExportTable } from "../../../../../components/Common/DataGrid/types";
import type { IDelivery } from "../../../../../models/Delivery";
import type { ArchivedDelivery } from "../../../../../models/Delivery/ArchivedDelivery";
import type { FeatureMetric } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import type { IEntityReference } from "../../../../../models/EntityReference";
import type { IFeature } from "../../../../../models/Feature";
import type { IWhenForecast } from "../../../../../models/Forecasts/WhenForecast";
import { formatLocalDate } from "../../../../../utils/date/localDate";
import { withheldName } from "../../../../../utils/dependencies/dependencySentences";
import { getWorkItemName } from "../../../../../utils/featureName";
import { featureWarningSentences } from "../../../../../utils/features/featureWarningSentences";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
} from "../../../../../utils/forecast/cannotForecast";
import { featureLikelihoodLabel } from "../../../../../utils/forecast/featureLikelihoodLabel";
import { formatLikelihood } from "../../../../../utils/forecast/formatLikelihood";

/** The words this instance uses, so an exported file reads in the reader's own vocabulary. */
export interface DeliveryExportTerms {
	deliveryTerm: string;
	workItemsTerm: string;
	featureTerm: string;
	portfolioTerm: string;
	teamTerm: string;
}

const FORECAST_PROBABILITIES = [50, 70, 85, 95] as const;

const HEADERS = [
	"Name",
	"Team",
	"Progress",
	...FORECAST_PROBABILITIES.map((probability) => `Forecast ${probability}%`),
	"Likelihood",
	"State",
	"Dependencies",
	"Warnings",
];

const NOTHING_TO_SAY = "";

const dateFor = (
	forecasts: readonly IWhenForecast[],
	probability: number,
): string => {
	const forecast = forecasts.find(
		(candidate) => candidate.probability === probability,
	);
	return forecast
		? formatLocalDate(new Date(forecast.expectedDate))
		: NOTHING_TO_SAY;
};

const progress = (total: number, remaining: number): string =>
	`${total - remaining}/${total}`;

const deliveryRow = (
	delivery: IDelivery,
	{ deliveryTerm }: DeliveryExportTerms,
): string[] => [
	`${delivery.name} (${deliveryTerm})`,
	NOTHING_TO_SAY,
	progress(delivery.totalWork, delivery.remainingWork),
	...FORECAST_PROBABILITIES.map((probability) =>
		dateFor(delivery.completionDates ?? [], probability),
	),
	delivery.likelihoodPercentage === null ||
	delivery.likelihoodPercentage === undefined
		? NOTHING_TO_SAY
		: `${Math.round(delivery.likelihoodPercentage)}%`,
	NOTHING_TO_SAY,
	NOTHING_TO_SAY,
	NOTHING_TO_SAY,
];

const teamsOn = (feature: IFeature, teams: IEntityReference[]): string => {
	const withWork = teams.filter(
		(team) => feature.getTotalWorkForTeam(team.id) > 0,
	);

	return withWork.length === 0
		? "Unassigned"
		: withWork.map((team) => team.name).join("; ");
};

const forecastCells = (feature: IFeature): string[] => {
	if (
		cannotBeForecast({ teamsWithoutForecast: feature.teamsWithoutForecast })
	) {
		return FORECAST_PROBABILITIES.map(() => CANNOT_FORECAST_SHORT);
	}

	return FORECAST_PROBABILITIES.map((probability) =>
		dateFor(feature.forecasts ?? [], probability),
	);
};

const likelihoodOf = (feature: IFeature, delivery: IDelivery): string => {
	const featureLikelihood = delivery.featureLikelihoods?.find(
		(candidate) => candidate.featureId === feature.id,
	);

	return featureLikelihood
		? featureLikelihoodLabel(
				featureLikelihood,
				feature.getRemainingWorkForFeature() > 0,
			)
		: NOTHING_TO_SAY;
};

const dependenciesOf = (
	feature: IFeature,
	terms: DeliveryExportTerms,
): string =>
	(feature.dependsOn ?? [])
		.map((dependency) =>
			dependency.isWithheld
				? withheldName(terms)
				: `${dependency.referenceId}: ${dependency.name}`,
		)
		.join("; ");

const featureRow = (
	feature: IFeature,
	delivery: IDelivery,
	teams: IEntityReference[],
	terms: DeliveryExportTerms,
): string[] => {
	const warnings = featureWarningSentences(
		{
			isDoneWithRemainingWork:
				feature.stateCategory === "Done" &&
				feature.getRemainingWorkForFeature() > 0,
			isUsingDefaultFeatureSize: feature.isUsingDefaultFeatureSize,
			dependencies: feature.dependsOn,
		},
		terms,
	);

	return [
		getWorkItemName(feature.name, feature.referenceId),
		teamsOn(feature, teams),
		progress(
			feature.getTotalWorkForFeature(),
			feature.getRemainingWorkForFeature(),
		),
		...forecastCells(feature),
		likelihoodOf(feature, delivery),
		feature.state ?? NOTHING_TO_SAY,
		dependenciesOf(feature, terms),
		warnings.length > 0 ? "Yes" : "No",
	];
};

/**
 * The whole exported table, written here rather than scraped off the grid: half these cells are drawn
 * by a renderer and have no backing field to read, so scraping them yields a raw array, a stale count
 * or nothing at all.
 *
 * A number nobody computed leaves as an empty cell. Writing 0, NaN or "null" into a status report
 * reads as a measurement, and the reader has no way to tell it apart from a real one.
 *
 * The Features arrive in the order the reader is looking at them in, and are emitted in that order.
 */
export function buildDeliveryExportTable(
	delivery: IDelivery,
	features: IFeature[],
	teams: IEntityReference[],
	terms: DeliveryExportTerms,
): DataGridExportTable {
	return {
		headers: HEADERS,
		rows: [
			deliveryRow(delivery, terms),
			...features.map((feature) => featureRow(feature, delivery, teams, terms)),
		],
	};
}

const archivedDeliveryRow = (
	archived: ArchivedDelivery,
	{ deliveryTerm }: DeliveryExportTerms,
): string[] => [
	`${archived.name} (${deliveryTerm})`,
	NOTHING_TO_SAY,
	progress(archived.totalWork, archived.remainingWork),
	...FORECAST_PROBABILITIES.map((probability) =>
		dateFor(archived.whenDistribution, probability),
	),
	archived.likelihoodPercentage === null
		? NOTHING_TO_SAY
		: `${Math.round(archived.likelihoodPercentage)}%`,
	NOTHING_TO_SAY,
	NOTHING_TO_SAY,
	NOTHING_TO_SAY,
];

// The record keeps how far along a Feature was as a percentage and how many items it held, so the
// same "done of total" cell a live export writes is recoverable exactly. A record written before
// item counts were kept has no total, and half a fraction is worse than none.
const pinnedProgress = (row: FeatureMetric): string =>
	row.totalItems === null
		? NOTHING_TO_SAY
		: `${Math.round((row.completion / 100) * row.totalItems)}/${row.totalItems}`;

const pinnedLikelihood = (row: FeatureMetric): string =>
	row.likelihood === null
		? CANNOT_FORECAST_SHORT
		: formatLikelihood(row.likelihood, {
				hasRemainingWork: row.completion < 100,
				precision: "round",
			});

const archivedFeatureRow = (row: FeatureMetric): string[] => [
	getWorkItemName(row.name, row.referenceId),
	NOTHING_TO_SAY,
	pinnedProgress(row),
	...FORECAST_PROBABILITIES.map(() => NOTHING_TO_SAY),
	pinnedLikelihood(row),
	NOTHING_TO_SAY,
	NOTHING_TO_SAY,
	pinnedWarning(row),
];

// A record that says the size was measured is answering the question, and answering it with a blank
// would read as nobody having asked. Only a row from before sizes were kept has nothing to say.
const pinnedWarning = (row: FeatureMetric): string => {
	if (row.isUsingDefaultSize === null || row.isUsingDefaultSize === undefined) {
		return NOTHING_TO_SAY;
	}

	return row.isUsingDefaultSize ? "Yes" : "No";
};

/**
 * The same table, for a Delivery that has been retired. Same columns and the same Delivery-first
 * shape, so a report built from a closed Delivery lines up beside one built from a running Delivery
 * without the reader having to notice which is which.
 *
 * The record never held a Feature's Team, state, dependencies or its own forecast dates, and those
 * cells are left empty rather than filled from anywhere. Anything written there would be today's
 * answer under a heading that promises the closing day's.
 */
export function buildArchivedDeliveryExportTable(
	archived: ArchivedDelivery,
	rows: readonly FeatureMetric[],
	terms: DeliveryExportTerms,
): DataGridExportTable {
	return {
		headers: HEADERS,
		rows: [
			archivedDeliveryRow(archived, terms),
			...rows.map(archivedFeatureRow),
		],
	};
}
