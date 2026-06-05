import type { IStateMapping } from "../models/Common/StateMapping";
import type { ICumulativeStateTimeStateRow } from "../models/Metrics/CumulativeStateTime";

export type FlowEfficiencyResult =
	| { status: "not-configured" }
	| { status: "no-data" }
	| { status: "computed"; efficiencyPercent: number };

const matchesMappingName = (mapping: IStateMapping, entry: string): boolean =>
	mapping.name.toLowerCase() === entry.toLowerCase();

export function resolveWaitRawStates(
	waitStates: string[],
	stateMappings: IStateMapping[],
): string[] {
	return waitStates.flatMap((entry) => {
		const mapping = stateMappings.find((candidate) =>
			matchesMappingName(candidate, entry),
		);
		return mapping ? mapping.states : [entry];
	});
}

export function flowEfficiency(
	rows: ICumulativeStateTimeStateRow[],
	waitStates: string[],
	stateMappings: IStateMapping[],
): FlowEfficiencyResult {
	if (waitStates.length === 0) {
		return { status: "not-configured" };
	}

	const totalDoingDays = rows.reduce((sum, row) => sum + row.totalDays, 0);
	if (totalDoingDays <= 0) {
		return { status: "no-data" };
	}

	const waitRawStates = new Set(
		resolveWaitRawStates(waitStates, stateMappings).map((state) =>
			state.toLowerCase(),
		),
	);
	const waitDays = rows
		.filter((row) => waitRawStates.has(row.state.toLowerCase()))
		.reduce((sum, row) => sum + row.totalDays, 0);

	const activeDays = totalDoingDays - waitDays;
	return {
		status: "computed",
		efficiencyPercent: (activeDays / totalDoingDays) * 100,
	};
}
