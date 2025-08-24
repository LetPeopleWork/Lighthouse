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
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ModifyTrackingSystemConnectionDialog from "../../../pages/Settings/Connections/ModifyTrackingSystemConnectionDialog";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import FileUploadComponent from "../FileUpload/FileUploadComponent";
import InputGroup from "../InputGroup/InputGroup";

interface GeneralSettingsComponentProps<T extends IBaseSettings> {
	settings: T | null;
	onSettingsChange: <K extends keyof T>(key: K, value: T[K]) => void;
	title?: string;
	workTrackingSystemConnection?: IWorkTrackingSystemConnection | null;
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
	workTrackingSystemConnection = null,
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

	const [selectedFile, setSelectedFile] = useState<File | null>(null);
	const [uploadProgress] = useState<number | undefined>(undefined);
	const [validationErrors] = useState<string[]>([]);

	const { workTrackingSystemService } = useContext(ApiServiceContext);

	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const queryTerm = getTerm(TERMINOLOGY_KEYS.QUERY);
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

	const handleFileSelect = (file: File | null) => {
		setSelectedFile(file);

		if (file) {
			const reader = new FileReader();
			reader.onload = (e) => {
				const csvContent = e.target?.result as string;
				if (csvContent) {
					onSettingsChange(
						"workItemQuery" as keyof T,
						csvContent as T[keyof T],
					);
				}
			};
			reader.readAsText(file);
		} else {
			onSettingsChange("workItemQuery" as keyof T, "" as T[keyof T]);
		}
	};

	const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
		onSettingsChange("workItemQuery" as keyof T, "" as T[keyof T]);
		onWorkTrackingSystemChange(event);
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
			{(!showWorkTrackingSystemSelection ||
				workTrackingSystemConnection?.dataSourceType !== "File") && (
				<Grid size={{ xs: 12 }}>
					<TextField
						label={`${workItemTerm} ${queryTerm}`}
						multiline
						rows={4}
						fullWidth
						margin="normal"
						value={settings?.workItemQuery ?? ""}
						onChange={(e) =>
							onSettingsChange(
								"workItemQuery" as keyof T,
								e.target.value as T[keyof T],
							)
						}
					/>
				</Grid>
			)}
			{showWorkTrackingSystemSelection &&
				(workTrackingSystemConnection?.dataSourceType === "File" ||
					(workTrackingSystemConnection &&
						!workTrackingSystemConnection.dataSourceType)) && (
					<FileUploadComponent
						workTrackingSystemConnection={workTrackingSystemConnection}
						selectedFile={selectedFile}
						onFileSelect={handleFileSelect}
						uploadProgress={uploadProgress}
						validationErrors={validationErrors}
					/>
				)}
		</InputGroup>
	);
};
export default GeneralSettingsComponent;
