import React, { useState } from 'react';
import { List, ListItem, Typography, IconButton, TextField, Button, Grid } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import InputGroup from '../InputGroup/InputGroup';

interface WorkItemTypesComponentProps {
    workItemTypes: string[];
    onAddWorkItemType: (type: string) => void;
    onRemoveWorkItemType: (type: string) => void;
}

const WorkItemTypesComponent: React.FC<WorkItemTypesComponentProps> = ({
    workItemTypes,
    onAddWorkItemType,
    onRemoveWorkItemType
}) => {
    const [newWorkItemType, setNewWorkItemType] = useState<string>('');

    const handleAddWorkItemType = () => {
        if (newWorkItemType.trim()) {
            onAddWorkItemType(newWorkItemType.trim());
            setNewWorkItemType('');
        }
    };

    const handleNewWorkItemTypeChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setNewWorkItemType(event.target.value);
    };

    return (
        <InputGroup title={'Work Item Types'} >
            <Grid container spacing={3}>
                <Grid item xs={3}>
                    <List>
                        {workItemTypes.map(type => (
                            <ListItem key={type}>
                                <Typography variant="body1">{type}</Typography>
                                <IconButton aria-label="delete" edge="end" onClick={() => onRemoveWorkItemType(type)}>
                                    <DeleteIcon  />
                                </IconButton>
                            </ListItem>
                        ))}
                    </List>
                </Grid>
                <Grid item xs={9}>
                    <TextField
                        label="New Work Item Type"
                        fullWidth
                        margin="normal"
                        value={newWorkItemType}
                        onChange={handleNewWorkItemTypeChange}
                    />
                    <Button variant="outlined" color="primary" onClick={handleAddWorkItemType}>
                        Add Work Item Type
                    </Button>
                </Grid>
            </Grid>
        </InputGroup>
    );
};

export default WorkItemTypesComponent;