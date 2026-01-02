import { Button, Container } from "@mui/material";
import Grid from "@mui/material/Grid";
import React, { useCallback, useContext, useEffect } from "react";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import ConnectionDetailTable from "./ConnectionDetailTable";
import ModifyTrackingSystemConnectionDialog from "./ModifyTrackingSystemConnectionDialog";

const WorkTrackingSystemConnectionSettings: React.FC = () => {
	const [openNewConnection, setOpenNewConnection] = React.useState(false);
	const [workTrackingSystems, setWorkTrackingSystems] = React.useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [workTrackingSystemConnections, setWorkTrackingSystemConnections] =
		React.useState<IWorkTrackingSystemConnection[]>([]);
	const [selectedConnection, setSelectedConnection] =
		React.useState<IWorkTrackingSystemConnection | null>(null);

	const [openExistingConnection, setOpenExistingConnection] =
		React.useState(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);

	const { workTrackingSystemService } = useContext(ApiServiceContext);

	const initializeData = useCallback(async () => {
		const workTrackingSystemsData =
			await workTrackingSystemService.getWorkTrackingSystems();
		setWorkTrackingSystems(workTrackingSystemsData);

		const existingConnections =
			await workTrackingSystemService.getConfiguredWorkTrackingSystems();
		setWorkTrackingSystemConnections(existingConnections);
	}, [workTrackingSystemService]);

	const onAddConnectionButtonClicked = () => {
		setOpenNewConnection(true);
	};

	const handleCloseNewConnection = async (
		newConnection: IWorkTrackingSystemConnection | null,
	) => {
		setOpenNewConnection(false);
		if (newConnection == null) {
			return;
		}

		const addedConnection =
			await workTrackingSystemService.addNewWorkTrackingSystemConnection(
				newConnection,
			);
		setWorkTrackingSystemConnections((prevSystems) => [
			...prevSystems,
			addedConnection,
		]);
	};

	const handleCloseEditConnection = async (
		modifiedConnection: IWorkTrackingSystemConnection | null,
	) => {
		setOpenExistingConnection(false);
		setSelectedConnection(null);

		if (modifiedConnection == null) {
			return;
		}

		const updatedConnection =
			await workTrackingSystemService.updateWorkTrackingSystemConnection(
				modifiedConnection,
			);

		setWorkTrackingSystemConnections((prevConnections) =>
			prevConnections.map((connection) =>
				connection.id === updatedConnection.id ? updatedConnection : connection,
			),
		);
	};

	const handleDeleteConnection = async (
		connection: IWorkTrackingSystemConnection,
	) => {
		setSelectedConnection(connection);
		setDeleteDialogOpen(true);
	};

	const handleDeleteConfirmation = async (confirmed: boolean) => {
		if (confirmed && selectedConnection?.id) {
			setWorkTrackingSystemConnections((prevConnections) =>
				prevConnections.filter(
					(connection) => connection.id !== selectedConnection.id,
				),
			);

			await workTrackingSystemService.deleteWorkTrackingSystemConnection(
				selectedConnection.id,
			);
		}

		setDeleteDialogOpen(false);
		setSelectedConnection(null);
	};

	const onEditConnectionButtonClicked = (
		system: IWorkTrackingSystemConnection,
	) => {
		setOpenExistingConnection(true);
		setSelectedConnection(system);
	};

	const onValidateConnection = async (
		settings: IWorkTrackingSystemConnection,
	) => {
		return await workTrackingSystemService.validateWorkTrackingSystemConnection(
			settings,
		);
	};

	useEffect(() => {
		initializeData();
	}, [initializeData]);

	return (
		<InputGroup title={"Work Tracking Systems"}>
			<Container maxWidth={false}>
				<Grid container spacing={3}>
					<Grid size={{ xs: 12 }}>
						<Button variant="contained" onClick={onAddConnectionButtonClicked}>
							Add Connection
						</Button>
						<ModifyTrackingSystemConnectionDialog
							open={openNewConnection}
							onClose={handleCloseNewConnection}
							workTrackingSystems={workTrackingSystems}
							validateSettings={onValidateConnection}
						/>
					</Grid>
					<Grid size={{ xs: 12 }}>
						<ConnectionDetailTable
							workTrackingSystemConnections={workTrackingSystemConnections}
							onEditConnectionButtonClicked={onEditConnectionButtonClicked}
							handleDeleteConnection={handleDeleteConnection}
						/>
						{selectedConnection && (
							<ModifyTrackingSystemConnectionDialog
								open={openExistingConnection}
								onClose={handleCloseEditConnection}
								workTrackingSystems={[selectedConnection]}
								validateSettings={onValidateConnection}
							/>
						)}
						{selectedConnection && (
							<DeleteConfirmationDialog
								open={deleteDialogOpen}
								itemName={selectedConnection.name}
								onClose={handleDeleteConfirmation}
							/>
						)}
					</Grid>
				</Grid>
			</Container>
		</InputGroup>
	);
};

export default WorkTrackingSystemConnectionSettings;
