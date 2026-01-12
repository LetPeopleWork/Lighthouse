import {
	Button,
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
	TextField,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import { useContext, useEffect, useState } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IDataRetrievalWizard } from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ModifyTrackingSystemConnectionDialog from "../../../pages/Settings/Connections/ModifyTrackingSystemConnectionDialog";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWizardsForSystem } from "../../DataRetrievalWizards";
import InputGroup from "../InputGroup/InputGroup";

interface GeneralSettingsComponentProps<T extends IBaseSettings> {
	settings: T | null;
	onSettingsChange: <K extends keyof T>(key: K, value: T[K]) => void;
	title?: string;
	workTrackingSystems?: IWorkTrackingSystemConnection[];
	selectedWorkTrackingSystem?: IWorkTrackingSystemConnection | null;
	onWorkTrackingSystemChange?: (event: SelectChangeEvent<string>) => void;
	onNewWorkTrackingSystemConnectionAdded?: (
		newConnection: IWorkTrackingSystemConnection,
	) => void;
	showWorkTrackingSystemSelection?: boolean;
}

const GeneralSettingsComponent = <T extends IBaseSettings>({
	settings,
	onSettingsChange,
	title = "General Configuration",
	workTrackingSystems = [],
	selectedWorkTrackingSystem = null,
	onWorkTrackingSystemChange = () => {},
	onNewWorkTrackingSystemConnectionAdded = () => {},
	showWorkTrackingSystemSelection = false,
}: GeneralSettingsComponentProps<T>) => {
	const [defaultWorkTrackingSystems, setDefaultWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [openDialog, setOpenDialog] = useState<boolean>(false);
	const [activeWizard, setActiveWizard] = useState<IDataRetrievalWizard | null>(
		null,
	);

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
		if (showWorkTrackingSystemSelection) {
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
		}
	}, [
		workTrackingSystemService,
		workTrackingSystemsTerm,
		showWorkTrackingSystemSelection,
	]);

	// Get available wizards for the selected work tracking system
	const availableWizards = selectedWorkTrackingSystem
		? getWizardsForSystem(selectedWorkTrackingSystem.workTrackingSystem)
		: [];

	const handleWizardComplete = (value: string) => {
		onSettingsChange("dataRetrievalValue" as keyof T, value as T[keyof T]);
		setActiveWizard(null);
	};

	const handleWizardCancel = () => {
		setActiveWizard(null);
	};

	const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
		onWorkTrackingSystemChange(event);
	};

	const getDataRetrievalDisplayName = (
		selectedWorkTrackingSystem: IWorkTrackingSystemConnection | null,
	) => {
		if (selectedWorkTrackingSystem === null) {
			return "Query";
		}

		return selectedWorkTrackingSystem.workTrackingSystemGetDataRetrievalDisplayName();
	};

	return (
		<InputGroup title={title}>
			<Grid size={{ xs: 12 }}>
				<TextField
					label="Name"
					fullWidth
					margin="normal"
					value={settings?.name ?? ""}
					onChange={(e) =>
						onSettingsChange("name" as keyof T, e.target.value as T[keyof T])
					}
				/>
			</Grid>
			{showWorkTrackingSystemSelection && (
				<Grid size={{ xs: 12 }}>
					<FormControl fullWidth margin="normal">
						<InputLabel>{`Select ${workTrackingSystemTerm}`}</InputLabel>
						<Select
							value={selectedWorkTrackingSystem?.name ?? ""}
							onChange={handleWorkTrackingSystemChange}
							label={`Select ${workTrackingSystemTerm}`}
						>
							{workTrackingSystems.map((system) => (
								<MenuItem key={system.id} value={system.name}>
									{system.name}
								</MenuItem>
							))}
						</Select>
					</FormControl>
					<Button
						variant="contained"
						color="primary"
						onClick={handleDialogOpen}
					>
						Add New {workTrackingSystemTerm}
					</Button>
					<ModifyTrackingSystemConnectionDialog
						open={openDialog}
						onClose={handleDialogClose}
						workTrackingSystems={defaultWorkTrackingSystems}
						validateSettings={onValidateConnection}
					/>
				</Grid>
			)}

			{selectedWorkTrackingSystem && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label={
							selectedWorkTrackingSystem
								? getDataRetrievalDisplayName(selectedWorkTrackingSystem)
								: "Data Retrieval"
						}
						multiline
						rows={4}
						fullWidth
						margin="normal"
						value={settings?.dataRetrievalValue ?? ""}
						onChange={(e) =>
							onSettingsChange(
								"dataRetrievalValue" as keyof T,
								e.target.value as T[keyof T],
							)
						}
					/>
				</Grid>
			)}

			{/* Render wizard buttons for the selected work tracking system */}
			{availableWizards.length > 0 && (
				<Grid size={{ xs: 12 }}>
					{availableWizards.map((wizard) => (
						<Button
							key={wizard.id}
							variant="outlined"
							onClick={() => setActiveWizard(wizard)}
							sx={{ mr: 1, mt: 1 }}
						>
							{wizard.name}
						</Button>
					))}
				</Grid>
			)}
			{/* Render the active wizard dialog */}
			{activeWizard && selectedWorkTrackingSystem?.id != null && (
				<activeWizard.component
					open={true}
					workTrackingSystemConnectionId={selectedWorkTrackingSystem.id}
					onComplete={handleWizardComplete}
					onCancel={handleWizardCancel}
				/>
			)}
		</InputGroup>
	);
};
export default GeneralSettingsComponent;
