import type { DataGridExportHeaderRow } from "../../../../../components/Common/DataGrid/types";
import type { IDelivery } from "../../../../../models/Delivery";
import { formatLocalDate } from "../../../../../utils/date/localDate";

/** The words this instance uses, so an exported file reads in the reader's own vocabulary. */
export interface DeliveryExportTerms {
	deliveryTerm: string;
	workItemsTerm: string;
}

const FORECAST_PROBABILITIES = [70, 85, 95] as const;

const forecastFor = (delivery: IDelivery, probability: number): string => {
	const forecast = delivery.completionDates?.find(
		(candidate) => candidate.probability === probability,
	);
	return forecast ? formatLocalDate(new Date(forecast.expectedDate)) : "";
};

const deliveryDate = (delivery: IDelivery): string => {
	if (!delivery.date) {
		return "";
	}

	const parsed = new Date(delivery.date);
	return Number.isNaN(parsed.getTime()) ? "" : formatLocalDate(parsed);
};

const likelihood = (delivery: IDelivery): string =>
	delivery.likelihoodPercentage === null ||
	delivery.likelihoodPercentage === undefined
		? ""
		: `${Math.round(delivery.likelihoodPercentage)}%`;

/**
 * A number nobody computed must leave as an empty cell. Writing 0, NaN or "null" into a status
 * report reads as a measurement, and the reader has no way to tell it apart from a real one.
 */
export function buildDeliveryExportHeaderRows(
	delivery: IDelivery,
	{ deliveryTerm, workItemsTerm }: DeliveryExportTerms,
): DataGridExportHeaderRow[] {
	const completed = delivery.totalWork - delivery.remainingWork;

	return [
		{ label: deliveryTerm, value: delivery.name ?? "" },
		{ label: "Date", value: deliveryDate(delivery) },
		...FORECAST_PROBABILITIES.map((probability) => ({
			label: `Forecast ${probability}%`,
			value: forecastFor(delivery, probability),
		})),
		{ label: "Likelihood", value: likelihood(delivery) },
		{ label: `Total ${workItemsTerm}`, value: String(delivery.totalWork ?? 0) },
		{ label: `Completed ${workItemsTerm}`, value: String(completed) },
		{
			label: `Remaining ${workItemsTerm}`,
			value: String(delivery.remainingWork ?? 0),
		},
	];
}
