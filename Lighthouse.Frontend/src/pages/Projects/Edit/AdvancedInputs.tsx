import React from 'react';
import { TextField, Grid, FormControlLabel, Switch } from '@mui/material';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';

interface AdvancedInputsComponentProps {
    projectSettings: IProjectSettings | null;
    onProjectSettingsChange: (key: keyof IProjectSettings, value: string | number | boolean) => void; // Updated type to include boolean
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
    projectSettings,
    onProjectSettingsChange
}) => {
    return (
        <InputGroup title={'Advanced Configuration'} initiallyExpanded={false}>
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
                <FormControlLabel
                    control={
                        <Switch
                            checked={projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems}
                            onChange={(e) => onProjectSettingsChange('usePercentileToCalculateDefaultAmountOfWorkItems', e.target.checked)}
                        />
                    }
                    label="Use Percentile to Calculate Default Amount of Work Items"
                />
            </Grid>

            {projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems ? (
                <>
                    <Grid item xs={12}>
                        <TextField
                            label="Default Work Item Percentile"
                            type="number"
                            fullWidth
                            margin="normal"
                            value={projectSettings?.defaultWorkItemPercentile || ''}
                            InputProps={{
                                inputProps: { 
                                    max: 95, min: 50 
                                }
                            }}
                            onChange={(e) => onProjectSettingsChange('defaultWorkItemPercentile', parseInt(e.target.value, 10))}
                        />
                    </Grid>
                    <Grid item xs={12}>
                        <TextField
                            label="Historical Features Work Item Query"
                            fullWidth
                            multiline
                            rows={4}
                            margin="normal"
                            value={projectSettings?.historicalFeaturesWorkItemQuery || ''}
                            onChange={(e) => onProjectSettingsChange('historicalFeaturesWorkItemQuery', e.target.value)}
                        />
                    </Grid>
                </>
            ) : (
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
            )}

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