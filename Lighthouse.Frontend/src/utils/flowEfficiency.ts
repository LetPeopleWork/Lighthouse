import type { IStateMapping } from "../models/Common/StateMapping";
import type { ICumulativeStateTimeStateRow } from "../models/Metrics/CumulativeStateTime";

export type FlowEfficiencyResult =
	| { status: "not-configured" }
	| { status: "no-data" }
	| { status: "computed"; efficiencyPercent: number };

const sameState = (left: string, right: string): boolean =>
	left.toLowerCase() === right.toLowerCase();

export function resolveWaitRawStates(
	waitStates: string[],
	stateMappings: IStateMapping[],
): string[] {
	return waitStates.flatMap((entry) => {
		const mapping = stateMappings.find((candidate) =>
			sameState(candidate.name, entry),
		);
		return mapping ? mapping.states : [entry];
	});
}

const displayNameOf = (
	rawState: string,
	stateMappings: IStateMapping[],
): string =>
	stateMappings.find((mapping) =>
		mapping.states.some((state) => sameState(state, rawState)),
	)?.name ?? rawState;

// The chart rows carry a mapping's name in place of its states, so a wait state is compared in that form:
// a mapping's name stays itself, and one of a mapping's states stands for the whole mapping.
export function waitStatesAsDisplayed(
	waitStates: string[],
	stateMappings: IStateMapping[],
): Set<string> {
	return new Set(
		resolveWaitRawStates(waitStates, stateMappings).map((rawState) =>
			displayNameOf(rawState, stateMappings).toLowerCase(),
		),
	);
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

	const waitRows = waitStatesAsDisplayed(waitStates, stateMappings);
	const waitDays = rows
		.filter((row) => waitRows.has(row.state.toLowerCase()))
		.reduce((sum, row) => sum + row.totalDays, 0);

	const activeDays = totalDoingDays - waitDays;
	return {
		status: "computed",
		efficiencyPercent: (activeDays / totalDoingDays) * 100,
	};
}
