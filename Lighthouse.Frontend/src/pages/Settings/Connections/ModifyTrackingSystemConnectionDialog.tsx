import React, { useState, useEffect } from "react";
import { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { SelectChangeEvent, Dialog, DialogTitle, DialogContent, DialogActions, TextField, MenuItem, Select, InputLabel, FormControl, Button } from "@mui/material";
import { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import ActionButton from "../../../components/Common/ActionButton/ActionButton";

interface ModifyWorkTrackingSystemConnectionDialogProps {
    open: boolean;
    onClose: (value: IWorkTrackingSystemConnection | null) => void;
    workTrackingSystems: IWorkTrackingSystemConnection[];
    validateSettings: (connection: IWorkTrackingSystemConnection) => Promise<boolean>;
}

const ModifyTrackingSystemConnectionDialog: React.FC<ModifyWorkTrackingSystemConnectionDialogProps> = ({ open, onClose, workTrackingSystems, validateSettings }) => {
    const [name, setName] = useState<string>('');
    const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] = useState<IWorkTrackingSystemConnection | null>(null);
    const [selectedOptions, setSelectedOptions] = useState<IWorkTrackingSystemOption[]>([]);
    const [formValid, setFormValid] = useState<boolean>(false);
    const [isValidating, setIsValidating] = useState<boolean>(false);

    useEffect(() => {
        if (open && workTrackingSystems.length > 0) {
            const firstSystem = workTrackingSystems[0];
            setSelectedWorkTrackingSystem(firstSystem);
            setName(firstSystem.name);
            setSelectedOptions(firstSystem.options.map(option => ({
                key: option.key,
                value: option.value,
                isSecret: option.isSecret
            })));
        }
    }, [open, workTrackingSystems]);

    const handleSystemChange = (event: SelectChangeEvent<string>) => {
        const system = workTrackingSystems.find(system => system.workTrackingSystem === event.target.value);
        if (system) {
            setSelectedWorkTrackingSystem(system);
            setName(system.name);
            setSelectedOptions(system.options.map(option => ({
                key: option.key,
                value: option.value,
                isSecret: option.isSecret
            })));
        }
    };

    const handleOptionChange = (changedOption: IWorkTrackingSystemOption, newValue: string) => {
        setSelectedOptions(prevOptions =>
            prevOptions.map(option =>
                option.key === changedOption.key ? { ...option, value: newValue } : option
            )
        );
        
        setFormValid(false);
    };

    const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setName(event.target.value);
        setFormValid(false);
    };

    const handleValidate = async () => {
        if (selectedWorkTrackingSystem) {
            const settings: IWorkTrackingSystemConnection = {
                id: selectedWorkTrackingSystem.id,
                name,
                workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
                options: selectedOptions
            };

            setIsValidating(true);
            const isValid = await validateSettings(settings);
            setIsValidating(false);

            if (isValid) {
                setFormValid(true);
                return;
            } 
        }
        
        setFormValid(false);
    };

    const handleSubmit = () => {
        if (selectedWorkTrackingSystem) {
            const updatedSystem: IWorkTrackingSystemConnection = {
                id: selectedWorkTrackingSystem.id,
                name: name,
                workTrackingSystem: selectedWorkTrackingSystem.workTrackingSystem,
                options: selectedOptions
            };
            onClose(updatedSystem);
        } else {
            onClose(null);
        }
    };

    const handleClose = () => {
        onClose(null);
    };

    return (
        <Dialog onClose={handleClose} open={open} fullWidth>
            <DialogTitle>{workTrackingSystems.length === 1 ? "Edit Connection" : "Create New Connection"}</DialogTitle>
            <DialogContent>
                <TextField
                    label="Connection Name"
                    fullWidth
                    margin="normal"
                    value={name}
                    onChange={handleNameChange}
                />

                <FormControl fullWidth margin="normal">
                    <InputLabel>Select Work Tracking System</InputLabel>
                    <Select value={selectedWorkTrackingSystem?.workTrackingSystem || ''} onChange={handleSystemChange} label="Select Work Tracking System">
                        {workTrackingSystems.map(system => (
                            <MenuItem key={system.workTrackingSystem} value={system.workTrackingSystem}>
                                {system.workTrackingSystem}
                            </MenuItem>
                        ))}
                    </Select>
                </FormControl>

                {selectedOptions.map(option => (
                    <TextField
                        key={option.key}
                        label={option.key}
                        type={option.isSecret ? "password" : "text"}
                        fullWidth
                        margin="normal"
                        value={option.value}
                        onChange={(e) => handleOptionChange(option, e.target.value)} />
                ))}
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} variant="outlined" >Cancel</Button>
                <ActionButton
                    buttonText='Validate'
                    isWaiting={isValidating}
                    onClickHandler={handleValidate}
                    buttonVariant="outlined"
                />
                <Button onClick={handleSubmit} variant="outlined" disabled={!formValid}>
                    {workTrackingSystems.length === 1 ? "Save" : "Create"}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ModifyTrackingSystemConnectionDialog;
