export interface WhenDistributionPoint {
	probability: number;
	expectedDate: Date;
}

export interface FeatureMetric {
	referenceId: string;
	name: string;
	completion: number;
	likelihood: number;
}

export interface DeliveryMetricsHistoryPoint {
	date: Date;
	totalWork: number;
	doneWork: number;
	remainingWork: number;
	estimatedItemCount: number | null;
	forecastHowMany: number | null;
	likelihoodPercentage: number | null;
	whenDistribution: WhenDistributionPoint[] | null;
	featureBreakdown: FeatureMetric[];
}

export interface DeliveryMetricsHistory {
	deliveryDate: Date;
	firstSnapshotDate: Date | null;
	points: DeliveryMetricsHistoryPoint[];
}

class BoundaryError extends Error {}

function asObject(value: unknown, context: string): Record<string, unknown> {
	if (value === null || typeof value !== "object" || Array.isArray(value)) {
		throw new BoundaryError(`Expected an object for ${context}`);
	}
	return value as Record<string, unknown>;
}

function asNumber(value: unknown, context: string): number {
	if (typeof value !== "number" || Number.isNaN(value)) {
		throw new BoundaryError(`Expected a number for ${context}`);
	}
	return value;
}

function asNullableNumber(value: unknown, context: string): number | null {
	if (value === null || value === undefined) {
		return null;
	}
	return asNumber(value, context);
}

function asString(value: unknown, context: string): string {
	if (typeof value !== "string") {
		throw new BoundaryError(`Expected a string for ${context}`);
	}
	return value;
}

function asDate(value: unknown, context: string): Date {
	if (typeof value !== "string") {
		throw new BoundaryError(`Expected a date string for ${context}`);
	}
	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		throw new BoundaryError(`Expected a valid date for ${context}`);
	}
	return date;
}

function asNullableDate(value: unknown, context: string): Date | null {
	if (value === null || value === undefined) {
		return null;
	}
	return asDate(value, context);
}

function parseWhenDistribution(value: unknown): WhenDistributionPoint[] | null {
	if (value === null || value === undefined) {
		return null;
	}
	if (!Array.isArray(value)) {
		throw new BoundaryError("Expected an array for whenDistribution");
	}
	return value.map((entry) => {
		const point = asObject(entry, "whenDistribution entry");
		return {
			probability: asNumber(point.probability, "whenDistribution.probability"),
			expectedDate: asDate(point.expectedDate, "whenDistribution.expectedDate"),
		};
	});
}

function parseFeatureBreakdown(value: unknown): FeatureMetric[] {
	if (value === null || value === undefined) {
		return [];
	}
	if (!Array.isArray(value)) {
		throw new BoundaryError("Expected an array for featureBreakdown");
	}
	return value.map((entry) => {
		const metric = asObject(entry, "featureBreakdown entry");
		return {
			referenceId: asString(metric.referenceId, "featureBreakdown.referenceId"),
			name: asString(metric.name, "featureBreakdown.name"),
			completion: asNumber(metric.completion, "featureBreakdown.completion"),
			likelihood: asNumber(metric.likelihood, "featureBreakdown.likelihood"),
		};
	});
}

function parsePoint(value: unknown): DeliveryMetricsHistoryPoint {
	const point = asObject(value, "metrics-history point");
	return {
		date: asDate(point.date, "point.date"),
		totalWork: asNumber(point.totalWork, "point.totalWork"),
		doneWork: asNumber(point.doneWork, "point.doneWork"),
		remainingWork: asNumber(point.remainingWork, "point.remainingWork"),
		estimatedItemCount: asNullableNumber(
			point.estimatedItemCount,
			"point.estimatedItemCount",
		),
		forecastHowMany: asNullableNumber(
			point.forecastHowMany,
			"point.forecastHowMany",
		),
		likelihoodPercentage: asNullableNumber(
			point.likelihoodPercentage,
			"point.likelihoodPercentage",
		),
		whenDistribution: parseWhenDistribution(point.whenDistribution),
		featureBreakdown: parseFeatureBreakdown(point.featureBreakdown),
	};
}

export function parseDeliveryMetricsHistory(
	value: unknown,
): DeliveryMetricsHistory {
	const body = asObject(value, "metrics-history response");
	const points = body.points;
	if (!Array.isArray(points)) {
		throw new BoundaryError("Expected an array for points");
	}
	return {
		deliveryDate: asDate(body.deliveryDate, "deliveryDate"),
		firstSnapshotDate: asNullableDate(
			body.firstSnapshotDate,
			"firstSnapshotDate",
		),
		points: points.map(parsePoint),
	};
}
