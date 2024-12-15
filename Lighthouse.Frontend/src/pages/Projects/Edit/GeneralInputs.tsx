import React from 'react';
import { TextField } from '@mui/material';
import Grid from '@mui/material/Grid2'
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';

interface GeneralInputsComponentProps {
    projectSettings: IProjectSettings | null;
    onProjectSettingsChange: (key: keyof IProjectSettings, value: string | number) => void;
}

const GeneralInputsComponent: React.FC<GeneralInputsComponentProps> = ({
    projectSettings,
    onProjectSettingsChange
}) => {
    return (
        <InputGroup title={'General Configuration'} >
            <Grid  size={{ xs: 12 }}>
                <TextField
                    label="Name"
                    fullWidth
                    margin="normal"
                    value={projectSettings?.name ?? ''}
                    onChange={(e) => onProjectSettingsChange('name', e.target.value)}
                />
            </Grid>            
            <Grid  size={{ xs: 12 }}>
                <TextField
                    label="Work Item Query"
                    multiline
                    rows={4}
                    fullWidth
                    margin="normal"
                    value={projectSettings?.workItemQuery ?? ''}
                    onChange={(e) => onProjectSettingsChange('workItemQuery', e.target.value)}
                />
            </Grid>
        </InputGroup>
    );
};

export default GeneralInputsComponent;