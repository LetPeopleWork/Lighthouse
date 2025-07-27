import {
	Button,
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
} from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ModifyTrackingSystemConnectionDialog from "../../../pages/Settings/Connections/ModifyTrackingSystemConnectionDialog";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";

interface WorkTrackingSystemComponentProps {
	workTrackingSystems: IWorkTrackingSystemConnection[];
	selectedWorkTrackingSystem: IWorkTrackingSystemConnection | null;
	onWorkTrackingSystemChange: (event: SelectChangeEvent<string>) => void;
	onNewWorkTrackingSystemConnectionAdded: (
		newConnection: IWorkTrackingSystemConnection,
	) => void;
}

const WorkTrackingSystemComponent: React.FC<
	WorkTrackingSystemComponentProps
> = ({
	workTrackingSystems,
	selectedWorkTrackingSystem,
	onWorkTrackingSystemChange,
	onNewWorkTrackingSystemConnectionAdded,
}) => {
	const [defaultWorkTrackingSystems, setDefaultWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);

	const [openDialog, setOpenDialog] = useState<boolean>(false);

	const { workTrackingSystemService } = useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const workTrackingSystemsTerm = getTerm(
		TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS,
	);

	const handleDialogOpen = () => {
		setOpenDialog(true);
	};

	const handleDialogClose = async (
		newConnection: IWorkTrackingSystemConnection | null,
	) => {
		setOpenDialog(false);
		if (newConnection) {
			const addedConnection =
				await workTrackingSystemService.addNewWorkTrackingSystemConnection(
					newConnection,
				);
			onNewWorkTrackingSystemConnectionAdded(addedConnection);
		}
	};

	const onValidateConnection = async (
		settings: IWorkTrackingSystemConnection,
	) => {
		return await workTrackingSystemService.validateWorkTrackingSystemConnection(
			settings,
		);
	};

	useEffect(() => {
		const fetchDefaultSystems = async () => {
			try {
				const systems =
					await workTrackingSystemService.getWorkTrackingSystems();
				setDefaultWorkTrackingSystems(systems);
			} catch (error) {
				console.error(
					`Error fetching default ${workTrackingSystemsTerm}`,
					error,
				);
			}
		};

		fetchDefaultSystems();
	}, [workTrackingSystemService, workTrackingSystemsTerm]);

	return (
		<InputGroup title={workTrackingSystemTerm}>
			<FormControl fullWidth margin="normal">
				<InputLabel>{`Select ${workTrackingSystemTerm}`}</InputLabel>
				<Select
					value={selectedWorkTrackingSystem?.name ?? ""}
					onChange={onWorkTrackingSystemChange}
					label={`Select ${workTrackingSystemTerm}`}
				>
					{workTrackingSystems.map((system) => (
						<MenuItem key={system.id} value={system.name}>
							{system.name}
						</MenuItem>
					))}
				</Select>
			</FormControl>
			<Button variant="contained" color="primary" onClick={handleDialogOpen}>
				Add New {workTrackingSystemTerm}
			</Button>
			<ModifyTrackingSystemConnectionDialog
				open={openDialog}
				onClose={handleDialogClose}
				workTrackingSystems={defaultWorkTrackingSystems}
				validateSettings={onValidateConnection}
			/>
		</InputGroup>
	);
};

export default WorkTrackingSystemComponent;
