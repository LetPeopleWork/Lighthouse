import React from 'react';
import { TextField, FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import Grid from '@mui/material/Grid2';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import { ITeam } from '../../../models/Team/Team';

interface OwnershipComponentProps {
    projectSettings: IProjectSettings | null;
    onProjectSettingsChange: (key: keyof IProjectSettings, value: string | ITeam | null) => void;
    currentInvolvedTeams: ITeam[];
}

const OwnershipComponent: React.FC<OwnershipComponentProps> = ({
    projectSettings,
    onProjectSettingsChange,
    currentInvolvedTeams
}) => {
    return (
        <InputGroup title={'Ownership Settings'} initiallyExpanded={false}>
            <Grid size={{ xs: 12 }}>
                <FormControl fullWidth margin="normal">
                    <InputLabel>Owning Team</InputLabel>
                    <Select
                        value={projectSettings?.owningTeam?.id ?? ''}
                        label="Owning Team"
                        onChange={(e) => {
                            const teamId = e.target.value;
                            const team = currentInvolvedTeams.find(t => t.id === teamId);
                            onProjectSettingsChange('owningTeam', team || null);
                        }}
                    >
                        <MenuItem value=""><em>None</em></MenuItem>
                        {currentInvolvedTeams.map(team => (
                            <MenuItem key={team.id} value={team.id}>
                                {team.name}
                            </MenuItem>
                        ))}
                    </Select>
                </FormControl>
            </Grid>
            <Grid size={{ xs: 12 }}>
                <TextField
                    label="Feature Owner Field"
                    fullWidth
                    margin="normal"
                    value={projectSettings?.featureOwnerField ?? ''}
                    onChange={(e) => onProjectSettingsChange('featureOwnerField', e.target.value)}
                />
            </Grid>
        </InputGroup>
    );
};

export default OwnershipComponent;
