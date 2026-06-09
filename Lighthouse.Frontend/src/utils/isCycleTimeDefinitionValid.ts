import type { IStateMapping } from "../models/Common/StateMapping";

const resolveBoundary = (
	boundary: string,
	stateMappings: IStateMapping[],
): string[] => {
	const mapping = stateMappings.find(
		(candidate) =>
			candidate.name.toLowerCase() === boundary.trim().toLowerCase(),
	);
	return mapping ? mapping.states : [boundary.trim()];
};

export const resolveWorkflowStates = (
	toDoStates: string[],
	doingStates: string[],
	doneStates: string[],
	stateMappings: IStateMapping[],
): string[] => {
	const ordered = [...toDoStates, ...doingStates, ...doneStates].flatMap(
		(state) => resolveBoundary(state, stateMappings),
	);
	const seen = new Set<string>();
	return ordered.filter((state) => {
		const key = state.toLowerCase();
		if (seen.has(key)) {
			return false;
		}
		seen.add(key);
		return true;
	});
};

export const cycleTimeBoundaryIndex = (
	boundary: string,
	workflowStates: string[],
	stateMappings: IStateMapping[],
): number => {
	const raw = resolveBoundary(boundary, stateMappings);
	return workflowStates.findIndex((state) =>
		raw.some((rawState) => rawState.toLowerCase() === state.toLowerCase()),
	);
};

export const isCycleTimeDefinitionValid = (
	definition: { startState: string; endState: string },
	allStates: string[],
	stateMappings: IStateMapping[],
): boolean => {
	const present = new Set(allStates.map((state) => state.toLowerCase()));
	const startRaw = resolveBoundary(definition.startState, stateMappings);
	const endRaw = resolveBoundary(definition.endState, stateMappings);

	return (
		startRaw.length > 0 &&
		endRaw.length > 0 &&
		startRaw.every(
			(state) => state !== "" && present.has(state.toLowerCase()),
		) &&
		endRaw.every((state) => state !== "" && present.has(state.toLowerCase()))
	);
};
