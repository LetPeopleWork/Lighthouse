import React, { useState } from 'react';
import { Grid, FormControl, InputLabel, Select, MenuItem, SelectChangeEvent, TextField } from '@mui/material';

interface SampleFrequencySelectorProps {
    sampleEveryNthDay: number;
    onSampleEveryNthDayChange: (value: number) => void;
}

const SampleFrequencySelector: React.FC<SampleFrequencySelectorProps> = ({ sampleEveryNthDay, onSampleEveryNthDayChange }) => {
    const [isCustom, setIsCustom] = useState<boolean>(false);

    const handlePredefinedChange = (event: SelectChangeEvent<string>) => {
        const value = event.target.value;
        if (value === 'custom') {
            setIsCustom(true);
            onSampleEveryNthDayChange(sampleEveryNthDay);
        } else {
            setIsCustom(false);
            onSampleEveryNthDayChange(parseInt(value, 10));
        }
    };

    const handleCustomChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const customValue = parseInt(event.target.value, 10);
        if (!isNaN(customValue)) {
            onSampleEveryNthDayChange(customValue);
        }
    };

    return (
        <Grid item xs={6}>
            <FormControl fullWidth>
                <InputLabel id="sample-frequency-label">Sampling Frequency</InputLabel>
                <Select
                    labelId="sample-frequency-label"
                    value={isCustom ? 'custom' : sampleEveryNthDay.toString()}
                    label="Sampling Frequency"
                    onChange={handlePredefinedChange}
                >
                    <MenuItem value={1}>Daily</MenuItem>
                    <MenuItem value={7}>Weekly</MenuItem>
                    <MenuItem value={30}>Monthly</MenuItem>
                    <MenuItem value="custom">Custom</MenuItem>
                </Select>
            </FormControl>
            
            {isCustom && (
                <TextField
                    label="Custom Sampling Interval (Days)"
                    type="number"
                    value={sampleEveryNthDay}
                    onChange={handleCustomChange}
                    fullWidth
                    margin="normal"
                    inputProps={{ min: 1 }}
                />
            )}
        </Grid>
    );
};

export default SampleFrequencySelector;