import React from 'react';
import { TextField, Grid } from '@mui/material';
import { ITeamSettings } from '../../../models/Team/TeamSettings';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';

interface GeneralInputsComponentProps {
    teamSettings: ITeamSettings | null;
    onTeamSettingsChange: (key: keyof ITeamSettings, value: string | number) => void;
}

const GeneralInputsComponent: React.FC<GeneralInputsComponentProps> = ({
    teamSettings,
    onTeamSettingsChange
}) => {
    return (
        <InputGroup title={'General Configuration'} >
            <Grid item xs={12}>
                <TextField
                    label="Name"
                    fullWidth
                    margin="normal"
                    value={teamSettings?.name || ''}
                    onChange={(e) => onTeamSettingsChange('name', e.target.value)}
                />
            </Grid>
            <Grid item xs={12}>
                <TextField
                    label="Throughput History"
                    type="number"
                    fullWidth
                    margin="normal"
                    value={teamSettings?.throughputHistory || ''}
                    onChange={(e) => onTeamSettingsChange('throughputHistory', parseInt(e.target.value, 10))}
                />
            </Grid>
            <Grid item xs={12}>
                <TextField
                    label="Feature WIP"
                    type="number"
                    fullWidth
                    margin="normal"
                    value={teamSettings?.featureWIP || 1}
                    InputProps={{
                        inputProps: { 
                            min: 1 
                        }
                    }}
                    onChange={(e) => onTeamSettingsChange('featureWIP', parseInt(e.target.value, 10))}
                />
            </Grid>
            <Grid item xs={12}>
                <TextField
                    label="Work Item Query"
                    multiline
                    rows={4}
                    fullWidth
                    margin="normal"
                    value={teamSettings?.workItemQuery || ''}
                    onChange={(e) => onTeamSettingsChange('workItemQuery', e.target.value)}
                />
            </Grid>
        </InputGroup>
    );
};

export default GeneralInputsComponent;