import React from 'react';
import { TextField, Grid, FormControlLabel, Switch, Typography } from '@mui/material';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import ItemListManager from '../../../components/Common/ItemListManager/ItemListManager';

interface AdvancedInputsComponentProps {
    projectSettings: IProjectSettings | null;
    onProjectSettingsChange: (key: keyof IProjectSettings, value: string | number | boolean | string[]) => void;
}

const AdvancedInputsComponent: React.FC<AdvancedInputsComponentProps> = ({
    projectSettings,
    onProjectSettingsChange
}) => {

    const handleAddOverrideChildCountState = (overrideChildCountState: string) => {
        if (overrideChildCountState.trim()) {
            const newStates = projectSettings
                ? [...(projectSettings.overrideChildCountStates || []), overrideChildCountState.trim()]
                : [overrideChildCountState.trim()];

            onProjectSettingsChange('overrideChildCountStates', newStates);
        }
    };

    const handleRemoveOverrideChildCountState = (overrideChildCountState: string) => {
        const newStates = projectSettings
            ? (projectSettings.overrideChildCountStates || []).filter(item => item !== overrideChildCountState)
            : [];

        onProjectSettingsChange('overrideChildCountStates', newStates);
    };

    return (
        <>
            <InputGroup title={'Unparented Work Items'} initiallyExpanded={false}>
                <Grid item xs={12}>
                    <TextField
                        label="Unparented Work Items Query"
                        fullWidth
                        multiline
                        rows={4}
                        margin="normal"
                        value={projectSettings?.unparentedItemsQuery ?? ''}
                        onChange={(e) => onProjectSettingsChange('unparentedItemsQuery', e.target.value)}
                    />
                </Grid>
            </InputGroup>

            <InputGroup title={'Default Feature Size'} initiallyExpanded={false}>

                <Grid item xs={12}>
                    <FormControlLabel
                        control={
                            <Switch
                                checked={projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems}
                                onChange={(e) => onProjectSettingsChange('usePercentileToCalculateDefaultAmountOfWorkItems', e.target.checked)}
                            />
                        }
                        label="Use Historical Feature Size To Calculate Default"
                    />
                </Grid>

                {
                    projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems ? (
                        <>
                            <Grid item xs={12}>
                                <TextField
                                    label="Feature Size Percentile"
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
                                value={projectSettings?.defaultAmountOfWorkItemsPerFeature ?? ''}
                                onChange={(e) => onProjectSettingsChange('defaultAmountOfWorkItemsPerFeature', parseInt(e.target.value, 10))}
                            />
                        </Grid>
                    )
                }

                <Grid item xs={12}>
                    <TextField
                        label="Size Estimate Field"
                        fullWidth
                        margin="normal"
                        value={projectSettings?.sizeEstimateField ?? ''}
                        onChange={(e) => onProjectSettingsChange('sizeEstimateField', e.target.value)}
                    />
                </Grid>

                <Grid item xs={12}>
                    <Typography variant='body1'>Use Default Size instead of real Child Items for Features in these States:</Typography>
                    <ItemListManager
                        title='Size Override State'
                        items={projectSettings?.overrideChildCountStates ?? []}
                        onAddItem={handleAddOverrideChildCountState}
                        onRemoveItem={handleRemoveOverrideChildCountState}
                    />
                </Grid>
            </InputGroup >
        </>
    );
};

export default AdvancedInputsComponent;