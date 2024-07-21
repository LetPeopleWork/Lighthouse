import React, { useState } from 'react';
import { List, ListItem, IconButton, TextField, Button, Grid } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { IMilestone, Milestone } from '../../../models/Project/Milestone';
import InputGroup from '../InputGroup/InputGroup';

interface MilestonesComponentProps {
    milestones: IMilestone[];
    onAddMilestone: (milestone: IMilestone) => void;
    onRemoveMilestone: (name: string) => void;
    onUpdateMilestone: (name: string, updatedMilestone: Partial<IMilestone>) => void;
}

const MilestonesComponent: React.FC<MilestonesComponentProps> = ({
    milestones,
    onAddMilestone,
    onRemoveMilestone,
    onUpdateMilestone
}) => {
    const [newMilestoneName, setNewMilestoneName] = useState<string>('');
    const [newMilestoneDate, setNewMilestoneDate] = useState<string>('');

    const handleAddMilestone = () => {
        if (newMilestoneName.trim() && newMilestoneDate) {
            const newMilestone = new Milestone(0, newMilestoneName.trim(), new Date(newMilestoneDate));

            onAddMilestone(newMilestone);
            setNewMilestoneName('');
            setNewMilestoneDate('');
        }
    };

    const handleMilestoneNameChange = (oldName: string, newName: string) => {
        onUpdateMilestone(oldName, { name: newName });
    };

    const handleMilestoneDateChange = (name: string, newDate: string) => {
        const updatedDate = new Date(newDate);
        onUpdateMilestone(name, { date: updatedDate });
    };

    return (
        <InputGroup title={'Milestones'} >
            <Grid container spacing={2}>
                <Grid item xs={6}>
                    <List>
                        {milestones.map(milestone => (
                            <ListItem key={milestone.name}>
                                <Grid container spacing={2} alignItems="center">
                                    <Grid item xs={4}>
                                        <TextField
                                            fullWidth
                                            label="Milestone Name"
                                            value={milestone.name}
                                            onChange={(e) => handleMilestoneNameChange(milestone.name, e.target.value)}
                                        />
                                    </Grid>
                                    <Grid item xs={4}>
                                        <TextField
                                            fullWidth
                                            label="Milestone Date"
                                            type="date"
                                            InputLabelProps={{ shrink: true }}
                                            value={milestone.date.toISOString().slice(0, 10)} // Convert date to yyyy-MM-dd format
                                            onChange={(e) => handleMilestoneDateChange(milestone.name, e.target.value)}
                                        />
                                    </Grid>
                                    <Grid item xs={4}>
                                        <IconButton aria-label="delete" onClick={() => onRemoveMilestone(milestone.name)}>
                                            <DeleteIcon />
                                        </IconButton>
                                    </Grid>
                                </Grid>
                            </ListItem>
                        ))}
                    </List>
                </Grid>
                <Grid item xs={6}>
                    <TextField
                        fullWidth
                        label="New Milestone Name"
                        margin="normal"
                        value={newMilestoneName}
                        onChange={(e) => setNewMilestoneName(e.target.value)}
                    />
                    <TextField
                        fullWidth
                        label="New Milestone Date"
                        type="date"
                        margin="normal"
                        InputLabelProps={{ shrink: true }}
                        value={newMilestoneDate}
                        onChange={(e) => setNewMilestoneDate(e.target.value)}
                    />
                    <Button
                        variant="contained"
                        color="primary"
                        startIcon={<AddIcon />}
                        onClick={handleAddMilestone}
                        fullWidth
                        sx={{ marginTop: 2 }}
                    >
                        Add Milestone
                    </Button>
                </Grid>
            </Grid>
            </InputGroup>
    );
};

export default MilestonesComponent;