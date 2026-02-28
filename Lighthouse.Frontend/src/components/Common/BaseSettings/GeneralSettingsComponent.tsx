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
import { useState } from "react";
import type { IBoardInformation } from "../../../models/Boards/BoardInformation";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IDataRetrievalWizard } from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
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
	showWorkTrackingSystemSelection?: boolean;
}

const GeneralSettingsComponent = <T extends IBaseSettings>({
	settings,
	onSettingsChange,
	title = "General Configuration",
	workTrackingSystems = [],
	selectedWorkTrackingSystem = null,
	onWorkTrackingSystemChange = () => {},
	showWorkTrackingSystemSelection = false,
}: GeneralSettingsComponentProps<T>) => {
	const [activeWizard, setActiveWizard] = useState<IDataRetrievalWizard | null>(
		null,
	);
	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);

	// Get available wizards for the selected work tracking system
	const availableWizards = selectedWorkTrackingSystem
		? getWizardsForSystem(selectedWorkTrackingSystem.workTrackingSystem)
		: [];

	const handleWizardComplete = (boardInfo: IBoardInformation) => {
		// Only update fields that have non-empty values to preserve existing data
		if (boardInfo.dataRetrievalValue.trim() !== "") {
			onSettingsChange(
				"dataRetrievalValue" as keyof T,
				boardInfo.dataRetrievalValue as T[keyof T],
			);
		}

		if (boardInfo.workItemTypes.length > 0) {
			onSettingsChange(
				"workItemTypes" as keyof T,
				boardInfo.workItemTypes as T[keyof T],
			);
		}

		if (boardInfo.toDoStates.length > 0) {
			onSettingsChange(
				"toDoStates" as keyof T,
				boardInfo.toDoStates as T[keyof T],
			);
		}

		if (boardInfo.doingStates.length > 0) {
			onSettingsChange(
				"doingStates" as keyof T,
				boardInfo.doingStates as T[keyof T],
			);
		}

		if (boardInfo.doneStates.length > 0) {
			onSettingsChange(
				"doneStates" as keyof T,
				boardInfo.doneStates as T[keyof T],
			);
		}

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
