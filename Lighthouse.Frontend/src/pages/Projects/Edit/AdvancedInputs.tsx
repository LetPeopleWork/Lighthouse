import React from 'react';
import { TextField, Grid } from '@mui/material';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';

interface AdvancedInputsComponentProps {
    projectSettings: IProjectSettings | null;
    onProjectSettingsChange: (key: keyof IProjectSettings, value: string | number) => void;
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
    projectSettings,
    onProjectSettingsChange
}) => {
    return (
        <InputGroup title={'Advanced Configuration'} initiallyExpanded={false} >
            <Grid item xs={12}>
                <TextField
                    label="Unparented Work Items Query"
                    fullWidth
                    multiline
                    rows={4}
                    margin="normal"
                    value={projectSettings?.unparentedItemsQuery || ''}
                    onChange={(e) => onProjectSettingsChange('unparentedItemsQuery', e.target.value)}
                />
            </Grid>
            <Grid item xs={12}>
                <TextField
                    label="Default Number of Items per Feature"
                    type="number"
                    fullWidth
                    margin="normal"
                    value={projectSettings?.defaultAmountOfWorkItemsPerFeature || ''}
                    onChange={(e) => onProjectSettingsChange('defaultAmountOfWorkItemsPerFeature', parseInt(e.target.value, 10))}
                />
            </Grid>
            <Grid item xs={12}>
                <TextField
                    label="Size Estimate Field"
                    fullWidth
                    margin="normal"
                    value={projectSettings?.sizeEstimateField || ''}
                    onChange={(e) => onProjectSettingsChange('sizeEstimateField', e.target.value)}
                />
            </Grid>
        </InputGroup>
    );
};

export default AdvancedInputsComponent;