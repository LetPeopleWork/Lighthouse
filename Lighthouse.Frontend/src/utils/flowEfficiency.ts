import type { IStateMapping } from "../models/Common/StateMapping";
import type { ICumulativeStateTimeStateRow } from "../models/Metrics/CumulativeStateTime";
import { displayNameOf, foldKey } from "./stateMappingView";

export type FlowEfficiencyResult =
	| { status: "not-configured" }
	| { status: "no-data" }
	| { status: "computed"; efficiencyPercent: number };

export function resolveWaitRawStates(
	waitStates: string[],
	stateMappings: IStateMapping[],
): string[] {
	return waitStates.flatMap((entry) => {
		const key = foldKey(entry);
		const mapping = stateMappings.find(
			(candidate) => foldKey(candidate.name) === key,
		);
		return mapping ? mapping.states : [entry];
	});
}

// The chart rows carry a mapping's name in place of its states, so a wait state is compared in that form:
// a mapping's name stays itself, and one of a mapping's states stands for the whole mapping.
export function displayedWaitStateMatcher(
	waitStates: string[],
	stateMappings: IStateMapping[],
): (displayedState: string) => boolean {
	const waitKeys = new Set(
		resolveWaitRawStates(waitStates, stateMappings).map((rawState) =>
			foldKey(displayNameOf(rawState, stateMappings)),
		),
	);
	return (displayedState) => waitKeys.has(foldKey(displayedState));
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

	const isWaitState = displayedWaitStateMatcher(waitStates, stateMappings);
	const waitDays = rows
		.filter((row) => isWaitState(row.state))
		.reduce((sum, row) => sum + row.totalDays, 0);

	const activeDays = totalDoingDays - waitDays;
	return {
		status: "computed",
		efficiencyPercent: (activeDays / totalDoingDays) * 100,
	};
}
