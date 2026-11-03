import type { IStateMapping } from "../models/Common/StateMapping";
import {
	displayNameOf,
	displayStatesFor,
	foldKey,
	isBlankName,
	trimAsServer,
} from "./stateMappingView";

/** The workflow as the charts show it: To Do, Doing and Done in order, each mapping's states under its name. */
export const resolveWorkflowStates = (
	toDoStates: string[],
	doingStates: string[],
	doneStates: string[],
	stateMappings: IStateMapping[],
): string[] =>
	displayStatesFor(
		[...toDoStates, ...doingStates, ...doneStates],
		stateMappings,
	);

export const cycleTimeBoundaryIndex = (
	boundary: string,
	workflowStates: string[],
	stateMappings: IStateMapping[],
): number => {
	if (isBlankName(boundary)) {
		return -1;
	}
	const key = foldKey(displayNameOf(trimAsServer(boundary), stateMappings));
	return workflowStates.findIndex((state) => foldKey(state) === key);
};

export const isCycleTimeDefinitionValid = (
	definition: { startState: string; endState: string },
	workflowStates: string[],
	stateMappings: IStateMapping[],
): boolean =>
	cycleTimeBoundaryIndex(
		definition.startState,
		workflowStates,
		stateMappings,
	) >= 0 &&
	cycleTimeBoundaryIndex(definition.endState, workflowStates, stateMappings) >=
		0;
