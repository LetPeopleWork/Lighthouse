import type { IPercentileValue } from "../../models/PercentileValue";

export const integerValueFormatter = (value: number): string => {
	return Number.isInteger(value) ? value.toString() : "";
};

export const dateValueFormatter = (value: number): string => {
	return new Date(value).toLocaleDateString();
};

export interface GetMaxYAxisHeightParams<T> {
	percentiles: IPercentileValue[];
	serviceLevelExpectation?: IPercentileValue | null;
	dataPoints: T[];
	getDataValue: (item: T) => number;
	minValue?: number;
}

export function getMaxYAxisHeight<T>({
	percentiles,
	serviceLevelExpectation,
	dataPoints,
	getDataValue,
	minValue = 0,
}: GetMaxYAxisHeightParams<T>): number {
	const maxFromPercentiles =
		percentiles.length > 0 ? Math.max(...percentiles.map((p) => p.value)) : 0;

	const maxFromSle = serviceLevelExpectation?.value ?? 0;

	const maxFromData =
		dataPoints.length > 0
			? Math.max(...dataPoints.map((element) => getDataValue(element)))
			: 0;

	const absoluteMax = Math.max(
		maxFromPercentiles,
		maxFromSle,
		maxFromData,
		minValue,
	);

	return absoluteMax * 1.1;
}

export interface TimeAxisConfig {
	id: string;
	scaleType: "time";
	label: string;
	min?: number;
	max?: number;
	valueFormatter: (value: number) => string;
}

export const createTimeAxis = (
	label: string,
	domain?: [number, number] | null,
): TimeAxisConfig => ({
	id: "timeAxis",
	scaleType: "time",
	label,
	min: domain?.[0],
	max: domain?.[1],
	valueFormatter: dateValueFormatter,
});

export interface LinearAxisConfig {
	id: string;
	scaleType: "linear";
	label: string;
	min?: number;
	max?: number;
	valueFormatter?: (value: number) => string;
	tickNumber?: number;
	tickLabelInterval?: () => boolean;
	disableTicks?: boolean;
}

export const createLinearAxis = (
	id: string,
	label: string,
	options: {
		min?: number;
		max?: number;
		useIntegerFormatter?: boolean;
		tickNumber?: number;
		tickLabelInterval?: () => boolean;
		disableTicks?: boolean;
	} = {},
): LinearAxisConfig => ({
	id,
	scaleType: "linear",
	label,
	min: options.min,
	max: options.max,
	valueFormatter: options.useIntegerFormatter
		? integerValueFormatter
		: undefined,
	tickNumber: options.tickNumber,
	tickLabelInterval: options.tickLabelInterval,
	disableTicks: options.disableTicks,
});

export interface GetDateOnlyTimestampParams {
	date: Date;
}

export const getDateOnlyTimestamp = (date: Date): number => {
	const dateOnly = new Date(date);
	dateOnly.setHours(0, 0, 0, 0);
	return dateOnly.getTime();
};
