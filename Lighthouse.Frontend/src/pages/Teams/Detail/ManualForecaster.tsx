import React from 'react';
import dayjs from 'dayjs';
import { Grid, TextField } from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { ManualForecast } from '../../../models/Forecasts/ManualForecast';
import ForecastInfoList from '../../../components/Common/Forecasts/ForecastInfoList';
import ForecastLikelihood from '../../../components/Common/Forecasts/ForecastLikelihood';
import ActionButton from '../../../components/Common/ActionButton/ActionButton';

interface ManualForecasterProps {
    remainingItems: number;
    targetDate: dayjs.Dayjs | null;
    manualForecastResult: ManualForecast | null;
    waitingForResults: boolean;
    onRemainingItemsChange: (value: number) => void;
    onTargetDateChange: (date: dayjs.Dayjs | null) => void;
    onRunManualForecast: () => void;
}

const ManualForecaster: React.FC<ManualForecasterProps> = ({
    remainingItems,
    targetDate,
    manualForecastResult,
    waitingForResults,
    onRemainingItemsChange,
    onTargetDateChange,
    onRunManualForecast
}) => {
    return (
        <Grid container spacing={3}>
            <Grid item xs={12}>
                <Grid container spacing={2}>
                    <Grid item xs={4}>
                        <TextField
                            label="Number of Items to Forecast"
                            type="number"
                            fullWidth
                            value={remainingItems}
                            onChange={(e) => onRemainingItemsChange(Number(e.target.value))}
                        />
                    </Grid>
                    <Grid item xs={4}>
                        <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <DatePicker
                                label="Target Date"
                                value={targetDate}
                                onChange={onTargetDateChange}
                                minDate={dayjs()}
                                sx={{ width: '100%' }}
                            />
                        </LocalizationProvider>
                    </Grid>
                    <Grid item xs={4}>
                        <ActionButton onClickHandler={onRunManualForecast} buttonText='Forecast' isWaiting={waitingForResults}  />
                    </Grid>
                </Grid>
            </Grid>
            <Grid item xs={12}>
                {manualForecastResult != null ? (
                    <Grid container spacing={2}>
                        <Grid item xs={4}>
                            <ForecastInfoList
                                title={`When will ${manualForecastResult.remainingItems} items be done?`}
                                forecasts={manualForecastResult.whenForecasts}
                            />
                        </Grid>
                        <Grid item xs={4}>
                            <ForecastInfoList
                                title={`How Many Items will you get done till ${manualForecastResult.targetDate.toLocaleDateString()}?`}
                                forecasts={manualForecastResult.howManyForecasts}
                            />
                        </Grid>
                        {manualForecastResult.likelihood > 0 && (
                            <Grid item xs={4}>
                                <ForecastLikelihood
                                    howMany={remainingItems}
                                    when={manualForecastResult.targetDate}
                                    likelihood={manualForecastResult.likelihood}
                                />
                            </Grid>
                        )}
                    </Grid>) : (<></>)}
            </Grid>
        </Grid>
    );
};

export default ManualForecaster;
