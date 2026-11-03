import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { StatesCollection } from "../../../models/StatesCollection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { displayStatesFor, foldKey } from "../../../utils/stateMappingView";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface StatesListComponentProps {
	toDoStates: string[];
	onAddToDoState: (type: string) => void;
	onRemoveToDoState: (type: string) => void;
	onReorderToDoStates?: (newOrder: string[]) => void;

	doingStates: string[];
	onAddDoingState: (type: string) => void;
	onRemoveDoingState: (type: string) => void;
	onReorderDoingStates?: (newOrder: string[]) => void;

	doneStates: string[];
	onAddDoneState: (type: string) => void;
	onRemoveDoneState: (type: string) => void;
	onReorderDoneStates?: (newOrder: string[]) => void;

	isForTeam?: boolean;
	stateMappingNames?: string[];
	/** The lists above hold raw states; the mappings only group them on display. */
	stateMappings?: IStateMapping[];
}

const NO_MAPPINGS: IStateMapping[] = [];

function mappingTooltipsFor(
	mappings: IStateMapping[],
): ReadonlyMap<string, string> {
	return new Map(
		mappings.map((mapping) => [
			mapping.name,
			`State mapping: ${mapping.states.join(", ")}. Delete it under State Mappings to remove it.`,
		]),
	);
}

function groupKeyOf(rawState: string, mappings: IStateMapping[]): string {
	const key = foldKey(rawState);
	const group = mappings.find((mapping) =>
		mapping.states.some((state) => foldKey(state) === key),
	);
	return foldKey(group?.name ?? rawState);
}

/** Turns a reordered display list back into raw states, each mapping's states together in their stored order. */
function rawOrderFor(
	displayOrder: string[],
	rawStates: string[],
	mappings: IStateMapping[],
): string[] {
	return displayOrder.flatMap((entry) => {
		const entryKey = foldKey(entry);
		return rawStates.filter(
			(state) => groupKeyOf(state, mappings) === entryKey,
		);
	});
}

function reorderHandlerFor(
	onReorder: ((newOrder: string[]) => void) | undefined,
	rawStates: string[],
	mappings: IStateMapping[],
): ((newOrder: string[]) => void) | undefined {
	if (!onReorder) return undefined;
	return (displayOrder) =>
		onReorder(rawOrderFor(displayOrder, rawStates, mappings));
}

const StatesList: React.FC<StatesListComponentProps> = ({
	toDoStates,
	onAddToDoState,
	onRemoveToDoState,
	onReorderToDoStates,
	doingStates,
	onAddDoingState,
	onRemoveDoingState,
	onReorderDoingStates,
	doneStates,
	onAddDoneState,
	onRemoveDoneState,
	onReorderDoneStates,
	isForTeam = true,
	stateMappingNames = [],
	stateMappings = NO_MAPPINGS,
}) => {
	const [statesSuggestions, setStatesSuggestions] =
		useState<StatesCollection | null>(null);
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const { suggestionService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchStates = async () => {
			setIsLoading(true);
			try {
				let availableStates: StatesCollection;
				if (isForTeam) {
					availableStates = await suggestionService.getStatesForTeams();
				} else {
					availableStates = await suggestionService.getStatesForProjects();
				}
				setStatesSuggestions(availableStates);
			} catch (error) {
				console.error("Failed to fetch states:", error);
			} finally {
				setIsLoading(false);
			}
		};

		fetchStates().catch((error) => {
			console.error("Failed to fetch states:", error);
		});
	}, [suggestionService, isForTeam]);

	const mappingTooltips = useMemo(
		() => mappingTooltipsFor(stateMappings),
		[stateMappings],
	);
	const toDoDisplayStates = useMemo(
		() => displayStatesFor(toDoStates, stateMappings),
		[toDoStates, stateMappings],
	);
	const doingDisplayStates = useMemo(
		() => displayStatesFor(doingStates, stateMappings),
		[doingStates, stateMappings],
	);
	const doneDisplayStates = useMemo(
		() => displayStatesFor(doneStates, stateMappings),
		[doneStates, stateMappings],
	);

	const reorderToDo = useMemo(
		() => reorderHandlerFor(onReorderToDoStates, toDoStates, stateMappings),
		[onReorderToDoStates, toDoStates, stateMappings],
	);
	const reorderDoing = useMemo(
		() => reorderHandlerFor(onReorderDoingStates, doingStates, stateMappings),
		[onReorderDoingStates, doingStates, stateMappings],
	);
	const reorderDone = useMemo(
		() => reorderHandlerFor(onReorderDoneStates, doneStates, stateMappings),
		[onReorderDoneStates, doneStates, stateMappings],
	);

	const allUsedStates = useMemo(() => {
		return [...toDoStates, ...doingStates, ...doneStates];
	}, [toDoStates, doingStates, doneStates]);

	const isOfferable = useMemo(() => {
		const mappingKeys = new Set(
			stateMappings.map((mapping) => foldKey(mapping.name)),
		);
		return (state: string) =>
			!allUsedStates.includes(state) && !mappingKeys.has(foldKey(state));
	}, [allUsedStates, stateMappings]);

	const todoSuggestions = useMemo(() => {
		const toDoArray = statesSuggestions?.toDoStates ?? [];
		const doingArray = statesSuggestions?.doingStates ?? [];
		const doneArray = statesSuggestions?.doneStates ?? [];

		const allStates = [
			...stateMappingNames,
			...toDoArray,
			...doingArray,
			...doneArray,
		];

		return Array.from(new Set(allStates)).filter(isOfferable);
	}, [statesSuggestions, isOfferable, stateMappingNames]);

	const doingSuggestions = useMemo(() => {
		const toDoArray = statesSuggestions?.toDoStates ?? [];
		const doingArray = statesSuggestions?.doingStates ?? [];
		const doneArray = statesSuggestions?.doneStates ?? [];

		const allStates = [
			...stateMappingNames,
			...doingArray,
			...toDoArray,
			...doneArray,
		];

		return Array.from(new Set(allStates)).filter(isOfferable);
	}, [statesSuggestions, isOfferable, stateMappingNames]);

	const doneSuggestions = useMemo(() => {
		const toDoArray = statesSuggestions?.toDoStates ?? [];
		const doingArray = statesSuggestions?.doingStates ?? [];
		const doneArray = statesSuggestions?.doneStates ?? [];

		const allStates = [
			...stateMappingNames,
			...doneArray,
			...toDoArray,
			...doingArray,
		];

		return Array.from(new Set(allStates)).filter(isOfferable);
	}, [statesSuggestions, isOfferable, stateMappingNames]);

	return (
		<InputGroup title="States">
			<Grid container>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">To Do</Typography>
					<ItemListManager
						title="To Do States"
						items={toDoDisplayStates}
						groupedItems={mappingTooltips}
						onAddItem={onAddToDoState}
						onRemoveItem={onRemoveToDoState}
						onReorderItems={reorderToDo}
						suggestions={todoSuggestions}
						isLoading={isLoading}
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">Doing</Typography>
					<ItemListManager
						title="Doing States"
						items={doingDisplayStates}
						groupedItems={mappingTooltips}
						onAddItem={onAddDoingState}
						onRemoveItem={onRemoveDoingState}
						onReorderItems={reorderDoing}
						suggestions={doingSuggestions}
						isLoading={isLoading}
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">Done</Typography>
					<ItemListManager
						title="Done States"
						items={doneDisplayStates}
						groupedItems={mappingTooltips}
						onAddItem={onAddDoneState}
						onRemoveItem={onRemoveDoneState}
						onReorderItems={reorderDone}
						suggestions={doneSuggestions}
						isLoading={isLoading}
					/>
				</Grid>
			</Grid>
		</InputGroup>
	);
};

export default StatesList;
