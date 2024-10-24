import React, { useContext, useEffect, useState } from 'react';
import { Alert, Grid } from '@mui/material';
import LighthouseChart from './LighthouseChart';
import dayjs, { Dayjs } from 'dayjs';
import { ILighthouseChartData } from '../../../../../models/Charts/LighthouseChartData';
import { ApiServiceContext } from '../../../../../services/Api/ApiServiceContext';
import DatePickerComponent from '../../../../../components/Common/DatePicker/DatePickerComponent';
import SampleFrequencySelector from '../../SampleFrequencySelector';
import LoadingAnimation from '../../../../../components/Common/LoadingAnimation/LoadingAnimation';

interface LighthouseChartComponentProps {
    projectId: number;
}

const LighthouseChartComponent: React.FC<LighthouseChartComponentProps> = ({ projectId }) => {
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [chartData, setChartData] = useState<ILighthouseChartData>();
    const [startDate, setStartDate] = useState<Dayjs>(dayjs().subtract(30, 'day'));
    const [sampleRate, setSampleRate] = useState<number>(1);

    const { chartService } = useContext(ApiServiceContext);

    const fetchLighthouseData = async () => {
        try {
            setIsLoading(true);
            const lighthouseChartData = await chartService.getLighthouseChartData(projectId, startDate.toDate(), sampleRate);

            if (lighthouseChartData) {
                setChartData(lighthouseChartData);
            } else {
                setHasError(true);
            }

            setIsLoading(false);
        } catch (error) {
            console.error('Error fetching project data:', error);
            setHasError(true);
        }
    };

    useEffect(() => {
        fetchLighthouseData();
    }, [startDate, sampleRate]);

    const onStartDateChanged = (newStartDate: Dayjs | null) => {
        if (newStartDate) {
            setStartDate(newStartDate);
        }
    }

    return (

        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
            {chartData?.features && chartData.features.length > 0 ? (
                <Grid container spacing={3}>
                    <Grid item xs={6}>
                        <DatePickerComponent label='Burndown Start Date' value={startDate} onChange={onStartDateChanged} />
                    </Grid>
                    <SampleFrequencySelector
                        sampleEveryNthDay={sampleRate}
                        onSampleEveryNthDayChange={setSampleRate}
                    />
                    <Grid item xs={12}>
                        <LighthouseChart data={chartData} />
                    </Grid>
                </Grid>) : (
                <Alert severity="warning">Can't display Burndown as no Feature information is available for this Project.</Alert>
            )}
        </LoadingAnimation>

    );
};

export default LighthouseChartComponent;
