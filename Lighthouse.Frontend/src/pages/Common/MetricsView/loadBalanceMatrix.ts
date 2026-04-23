import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";

export type LoadBalanceMatrixPoint = {
	readonly date: Date;
	readonly dayOffset: number;
	readonly dateLabel: string;
	readonly wip: number;
	readonly totalWorkItemAge: number;
	readonly opacity: number;
};

export type LoadBalanceMatrixData = {
	readonly baselineAvailable: boolean;
	readonly averageWip: number | null;
	readonly averageTotalWorkItemAge: number | null;
	readonly points: ReadonlyArray<LoadBalanceMatrixPoint>;
};

type DeriveLoadBalanceMatrixInput = {
	readonly endDate: Date;
	readonly currentWip: number;
	readonly currentTotalWorkItemAge: number | null;
	readonly wipPbcData: ProcessBehaviourChartData | null;
	readonly totalWorkItemAgePbcData: ProcessBehaviourChartData | null;
	readonly projectionDays?: number;
};

function isReadyBaseline(data: ProcessBehaviourChartData | null): boolean {
	return (
		data !== null &&
		data.status === "Ready" &&
		data.baselineConfigured &&
		Number.isFinite(data.average)
	);
}

function addDays(date: Date, days: number): Date {
	const result = new Date(date);
	result.setDate(result.getDate() + days);
	return result;
}

function getPointOpacity(dayOffset: number): number {
	if (dayOffset === 0) {
		return 1;
	}

	const minOpacity = 0.25;
	const step = 0.15;
	return Math.max(minOpacity, 1 - dayOffset * step);
}

export function isLoadBalanceBaselineAvailable(
	wipPbcData: ProcessBehaviourChartData | null,
	totalWorkItemAgePbcData: ProcessBehaviourChartData | null,
): boolean {
	return (
		isReadyBaseline(wipPbcData) && isReadyBaseline(totalWorkItemAgePbcData)
	);
}

export function deriveLoadBalanceMatrixData(
	input: DeriveLoadBalanceMatrixInput,
): LoadBalanceMatrixData {
	const projectionDays = input.projectionDays ?? 5;
	const baselineAvailable = isLoadBalanceBaselineAvailable(
		input.wipPbcData,
		input.totalWorkItemAgePbcData,
	);

	const averageWip = baselineAvailable
		? (input.wipPbcData?.average ?? null)
		: null;
	const averageTotalWorkItemAge = baselineAvailable
		? (input.totalWorkItemAgePbcData?.average ?? null)
		: null;

	const currentTotalWorkItemAge = input.currentTotalWorkItemAge ?? 0;
	const points: LoadBalanceMatrixPoint[] = [];

	for (let dayOffset = 0; dayOffset <= projectionDays; dayOffset++) {
		const pointDate = addDays(input.endDate, dayOffset);
		points.push({
			date: pointDate,
			dayOffset,
			dateLabel: pointDate.toLocaleDateString(),
			wip: input.currentWip,
			totalWorkItemAge: currentTotalWorkItemAge + input.currentWip * dayOffset,
			opacity: getPointOpacity(dayOffset),
		});
	}

	return {
		baselineAvailable,
		averageWip,
		averageTotalWorkItemAge,
		points,
	};
}
