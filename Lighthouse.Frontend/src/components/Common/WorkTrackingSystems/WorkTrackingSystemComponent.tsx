import React, { useState, useEffect } from 'react';
import { FormControl, InputLabel, Select, MenuItem, Button } from '@mui/material';
import { SelectChangeEvent } from '@mui/material';
import { IWorkTrackingSystemConnection } from '../../../models/WorkTracking/WorkTrackingSystemConnection';
import ModifyTrackingSystemConnectionDialog from '../../../pages/Settings/Connections/ModifyTrackingSystemConnectionDialog';
import InputGroup from '../InputGroup/InputGroup';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';

interface WorkTrackingSystemComponentProps {
    workTrackingSystems: IWorkTrackingSystemConnection[];
    selectedWorkTrackingSystem: IWorkTrackingSystemConnection | null;
    onWorkTrackingSystemChange: (event: SelectChangeEvent<string>) => void;
    onNewWorkTrackingSystemConnectionAdded: (newConnection: IWorkTrackingSystemConnection) => void;
}

const WorkTrackingSystemComponent: React.FC<WorkTrackingSystemComponentProps> = ({
    workTrackingSystems,
    selectedWorkTrackingSystem,
    onWorkTrackingSystemChange,
    onNewWorkTrackingSystemConnectionAdded,
}) => {
    const [defaultWorkTrackingSystems, setDefaultWorkTrackingSystems] = useState<IWorkTrackingSystemConnection[]>([]);

    const [openDialog, setOpenDialog] = useState<boolean>(false);
    const apiService = ApiServiceProvider.getApiService();

    const handleDialogOpen = () => {
        setOpenDialog(true);
    };

    const handleDialogClose = async (newConnection: IWorkTrackingSystemConnection | null) => {
        setOpenDialog(false);
        if (newConnection) {
            const addedConnection = await apiService.addNewWorkTrackingSystemConnection(newConnection);
            onNewWorkTrackingSystemConnectionAdded(addedConnection);
        }
    }

    const onValidateConnection = async (settings: IWorkTrackingSystemConnection) => {
        return await apiService.validateWorkTrackingSystemConnection(settings);
    }

    useEffect(() => {
        const fetchDefaultSystems = async () => {
            try {
                const systems = await apiService.getWorkTrackingSystems();
                setDefaultWorkTrackingSystems(systems);
            } catch (error) {
                console.error('Error fetching default work tracking systems', error);
            }
        };

        fetchDefaultSystems();
    }, [apiService]);

    return (
        <InputGroup title="Work Tracking System">
            <FormControl fullWidth margin="normal">
                <InputLabel>Select Work Tracking System</InputLabel>
                <Select
                    value={selectedWorkTrackingSystem?.name ?? ''}
                    onChange={onWorkTrackingSystemChange}
                    label="Select Work Tracking System"
                >
                    {workTrackingSystems.map(system => (
                        <MenuItem key={system.id} value={system.name}>
                            {system.name}
                        </MenuItem>
                    ))}
                </Select>
            </FormControl>
            <Button variant="contained" color="primary" onClick={handleDialogOpen}>
                Add New Work Tracking System
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