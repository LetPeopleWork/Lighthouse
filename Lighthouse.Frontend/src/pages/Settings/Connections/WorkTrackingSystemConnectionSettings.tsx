import { Button, Container, Grid } from "@mui/material";
import React, { useEffect } from "react";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import ModifyTrackingSystemConnectionDialog from "./ModifyTrackingSystemConnectionDialog";
import { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import ConnectionDetailTable from "./ConnectionDetailTable";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";

const WorkTrackingSystemConnectionSettings: React.FC = () => {
    const apiService = ApiServiceProvider.getApiService();

    const [openNewConnection, setOpenNewConnection] = React.useState(false);
    const [workTrackingSystems, setWorkTrackingSystems] = React.useState<IWorkTrackingSystemConnection[]>([]);
    const [workTrackingSystemConnections, setWorkTrackingSystemConnections] = React.useState<IWorkTrackingSystemConnection[]>([]);
    const [selectedConnection, setSelectedConnection] = React.useState<IWorkTrackingSystemConnection | null>(null);

    const [openExistingConnection, setOpenExistingConnection] = React.useState(false);
    const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);

    const initializeData = async () => {
        const workTrackingSystemsData = await apiService.getWorkTrackingSystems();
        setWorkTrackingSystems(workTrackingSystemsData);

        const existingConnections = await apiService.getConfiguredWorkTrackingSystems();
        setWorkTrackingSystemConnections(existingConnections);
    }

    const onAddConnectionButtonClicked = () => {
        setOpenNewConnection(true);
    }

    const handleCloseNewConnection = async (newConnection: IWorkTrackingSystemConnection | null) => {
        setOpenNewConnection(false);
        if (newConnection == null) {
            return;
        }

        const addedConnection = await apiService.addNewWorkTrackingSystemConnection(newConnection);
        setWorkTrackingSystemConnections(prevSystems => [...prevSystems, addedConnection]);
    };

    const handleCloseEditConnection = async (modifiedConnection: IWorkTrackingSystemConnection | null) => {
        setOpenExistingConnection(false);
        setSelectedConnection(null);

        if (modifiedConnection == null) {
            return;
        }

        const updatedConnection = await apiService.updateWorkTrackingSystemConnection(modifiedConnection);

        setWorkTrackingSystemConnections(prevConnections =>
            prevConnections.map(connection =>
                connection.id === updatedConnection.id ? updatedConnection : connection
            )
        );
    };

    const handleDeleteConnection = async (connection: IWorkTrackingSystemConnection) => {
        setSelectedConnection(connection);
        setDeleteDialogOpen(true);
    };

    const handleDeleteConfirmation = async (confirmed: boolean) => {
        if (confirmed && selectedConnection?.id) {
            setWorkTrackingSystemConnections(prevConnections =>
                prevConnections.filter(connection => connection.id !== selectedConnection.id)
            );

            await apiService.deleteWorkTrackingSystemConnection(selectedConnection.id);
        }

        setDeleteDialogOpen(false);
        setSelectedConnection(null);
    };

    const onEditConnectionButtonClicked = (system: IWorkTrackingSystemConnection) => {
        setOpenExistingConnection(true);
        setSelectedConnection(system);
    };

    const onValidateConnection = async (settings: IWorkTrackingSystemConnection) => {
        return await apiService.validateWorkTrackingSystemConnection(settings);
    }

    useEffect(() => {
        initializeData();
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    return (
        <InputGroup title={'Work Tracking Systems'}>
            <Container>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Button variant="contained" onClick={onAddConnectionButtonClicked}>Add Connection</Button>
                        <ModifyTrackingSystemConnectionDialog
                            open={openNewConnection}
                            onClose={handleCloseNewConnection}
                            workTrackingSystems={workTrackingSystems}
                            validateSettings={onValidateConnection}
                        />
                    </Grid>
                    <Grid item xs={12}>
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
        </InputGroup >
    )
}

export default WorkTrackingSystemConnectionSettings;
