import React from 'react';
import { TextField, Grid } from '@mui/material';
import { ITeamSettings } from '../../../models/Team/TeamSettings';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';

interface AdvancedInputsComponentProps {
    teamSettings: ITeamSettings | null;
    onTeamSettingsChange: (key: keyof ITeamSettings, value: string | number) => void;
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
    teamSettings,
    onTeamSettingsChange
}) => {
    return (
        <InputGroup title={'Advanced Configuration'} initiallyExpanded={false} >            
            <Grid item xs={12}>
                <TextField
                    label="Feature WIP"
                    type="number"
                    fullWidth
                    margin="normal"
                    value={teamSettings?.featureWIP ?? 1}
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
                    label="Relation Custom Field"
                    fullWidth
                    margin="normal"
                    value={teamSettings?.relationCustomField ?? ''}
                    onChange={(e) => onTeamSettingsChange('relationCustomField', e.target.value)}
                />
            </Grid>
        </InputGroup>
    );
};

export default AdvancedInputsComponent;