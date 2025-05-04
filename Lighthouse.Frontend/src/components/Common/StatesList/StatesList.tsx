import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import type { StatesCollection } from "../../../models/StatesCollection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface StatesListComponentProps {
	toDoStates: string[];
	onAddToDoState: (type: string) => void;
	onRemoveToDoState: (type: string) => void;

	doingStates: string[];
	onAddDoingState: (type: string) => void;
	onRemoveDoingState: (type: string) => void;

	doneStates: string[];
	onAddDoneState: (type: string) => void;
	onRemoveDoneState: (type: string) => void;

	isForTeam?: boolean;
}

const StatesList: React.FC<StatesListComponentProps> = ({
	toDoStates,
	onAddToDoState,
	onRemoveToDoState,
	doingStates,
	onAddDoingState,
	onRemoveDoingState,
	doneStates,
	onAddDoneState,
	onRemoveDoneState,
	isForTeam = true,
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

		fetchStates();
	}, [suggestionService, isForTeam]);

	const allUsedStates = useMemo(() => {
		return [...toDoStates, ...doingStates, ...doneStates];
	}, [toDoStates, doingStates, doneStates]);

	const todoSuggestions = useMemo(() => {
		const toDoArray = statesSuggestions?.toDoStates ?? [];
		const doingArray = statesSuggestions?.doingStates ?? [];
		const doneArray = statesSuggestions?.doneStates ?? [];

		const allStates = [...toDoArray, ...doingArray, ...doneArray];

		return Array.from(new Set(allStates)).filter(
			(state) => !allUsedStates.includes(state),
		);
	}, [statesSuggestions, allUsedStates]);

	const doingSuggestions = useMemo(() => {
		const toDoArray = statesSuggestions?.toDoStates ?? [];
		const doingArray = statesSuggestions?.doingStates ?? [];
		const doneArray = statesSuggestions?.doneStates ?? [];

		const allStates = [...doingArray, ...toDoArray, ...doneArray];

		return Array.from(new Set(allStates)).filter(
			(state) => !allUsedStates.includes(state),
		);
	}, [statesSuggestions, allUsedStates]);

	const doneSuggestions = useMemo(() => {
		const toDoArray = statesSuggestions?.toDoStates ?? [];
		const doingArray = statesSuggestions?.doingStates ?? [];
		const doneArray = statesSuggestions?.doneStates ?? [];

		const allStates = [...doneArray, ...toDoArray, ...doingArray];

		return Array.from(new Set(allStates)).filter(
			(state) => !allUsedStates.includes(state),
		);
	}, [statesSuggestions, allUsedStates]);

	return (
		<InputGroup title="States">
			<Grid container>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">To Do</Typography>
					<ItemListManager
						title="To Do States"
						items={toDoStates}
						onAddItem={onAddToDoState}
						onRemoveItem={onRemoveToDoState}
						suggestions={todoSuggestions}
						isLoading={isLoading}
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">Doing</Typography>
					<ItemListManager
						title="Doing States"
						items={doingStates}
						onAddItem={onAddDoingState}
						onRemoveItem={onRemoveDoingState}
						suggestions={doingSuggestions}
						isLoading={isLoading}
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">Done</Typography>
					<ItemListManager
						title="Done States"
						items={doneStates}
						onAddItem={onAddDoneState}
						onRemoveItem={onRemoveDoneState}
						suggestions={doneSuggestions}
						isLoading={isLoading}
					/>
				</Grid>
			</Grid>
		</InputGroup>
	);
};

export default StatesList;
