import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
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
}) => {
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
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">Doing</Typography>
					<ItemListManager
						title="Doing States"
						items={doingStates}
						onAddItem={onAddDoingState}
						onRemoveItem={onRemoveDoingState}
					/>
				</Grid>
				<Grid size={{ xs: 12 }}>
					<Typography variant="h6">Done</Typography>
					<ItemListManager
						title="Done States"
						items={doneStates}
						onAddItem={onAddDoneState}
						onRemoveItem={onRemoveDoneState}
					/>
				</Grid>
			</Grid>
		</InputGroup>
	);
};

export default StatesList;
