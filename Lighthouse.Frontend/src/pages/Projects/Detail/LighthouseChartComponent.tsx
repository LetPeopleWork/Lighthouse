import React, { useContext, useEffect, useState } from 'react';
import {
    ChartComponent,
    SeriesCollectionDirective,
    SeriesDirective,
    Inject,
    ColumnSeries,
    Tooltip,
    Legend,
    Highlight,
    DateTime,
    RangeAreaSeries,
    StepLineSeries,
} from '@syncfusion/ej2-react-charts';
import dayjs, { Dayjs } from 'dayjs';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { ILighthouseChartData, ILighthouseChartFeatureData } from '../../../models/Charts/LighthouseChartData';
import { Grid } from '@mui/material';
import SampleFrequencySelector from './SampleFrequencySelector';

interface LighthouseChartComponentProps {
    projectId: number;
}

const LighthouseChartComponent: React.FC<LighthouseChartComponentProps> = ({ projectId }) => {
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [chartData, setChartData] = useState<ILighthouseChartData>();
    const [startDate, setStartDate] = useState<dayjs.Dayjs>(dayjs().subtract(30, 'day'));
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

    function createForecastForFeature(feature: ILighthouseChartFeatureData): { x: Date, high: number, low: number }[] {
        const forecast = [];

        const lastBurndownEntry = feature.remainingItemsTrend[feature.remainingItemsTrend.length - 1];
        forecast.push({
            x: lastBurndownEntry.date,
            high: lastBurndownEntry.remainingItems,
            low: lastBurndownEntry.remainingItems
        });

        const earliestForecastDate = feature.forecasts[0];
        const latestForecastDate = feature.forecasts[feature.forecasts.length - 1];

        const highSecondEntry = lastBurndownEntry.remainingItems * (1 - (earliestForecastDate.getTime() - lastBurndownEntry.date.getTime()) / (latestForecastDate.getTime() - lastBurndownEntry.date.getTime()));

        forecast.push({
            x: earliestForecastDate,
            high: highSecondEntry,
            low: 0
        });

        forecast.push({
            x: latestForecastDate,
            high: 0,
            low: 0
        });

        return forecast;
    }

    return (

        <Grid container spacing={3}>

            <Grid item xs={6}>
                <LocalizationProvider dateAdapter={AdapterDayjs}>
                    <DatePicker
                        label="Burndown Start Date"
                        value={startDate}
                        onChange={onStartDateChanged}
                        maxDate={dayjs()}
                        sx={{ width: '100%' }}
                    />
                </LocalizationProvider>
            </Grid>
            <SampleFrequencySelector
                sampleEveryNthDay={sampleRate}
                onSampleEveryNthDayChange={setSampleRate}
            />

            <LoadingAnimation hasError={hasError} isLoading={isLoading}>
                <Grid item xs={12}>
                    <ChartComponent
                        id='lighthouse-chart'
                        theme='Material3'
                        legendSettings={{ enableHighlight: true, position: 'Top' }}
                        primaryXAxis={{
                            labelRotation: 0,
                            valueType: 'DateTime',
                            labelFormat: 'dd.MM.yyyy',
                            minimum: chartData?.startDate,
                            maximum: chartData?.endDate,
                            interval: 7,
                            intervalType: 'Days',
                            majorGridLines: { width: 0 },
                            majorTickLines: { width: 0 }
                        }}
                        primaryYAxis={{ title: '# Of Items Left', maximum: chartData?.maxRemainingItems }}
                        title='Feature Burndown'
                        tooltip={{ enable: true, header: "<b>${point.tooltip}</b>", shared: false }}
                    >
                        <Inject services={[ColumnSeries, StepLineSeries, Legend, Tooltip, DateTime, RangeAreaSeries, Highlight]} />

                        <SeriesCollectionDirective>
                            {chartData?.features.map((feature, index) => (
                                <SeriesDirective
                                    key={index}
                                    dataSource={feature.remainingItemsTrend.map(entry => ({
                                        x: entry.date,
                                        y: entry.remainingItems,
                                        name: feature.name
                                    }))}
                                    tooltipMappingName='name'
                                    xName='x'
                                    yName='y'
                                    name={feature.name}
                                    type='Column'
                                    fill={feature.color}
                                    columnSpacing={0.1}
                                />
                            ))}

                            {chartData?.features.map((feature, index) => {
                                if (feature.forecasts.length > 0) {
                                    const forecastData = createForecastForFeature(feature);
                                    return (
                                        <SeriesDirective
                                            key={index}
                                            dataSource={forecastData}
                                            tooltipMappingName='name'
                                            xName='x'
                                            high='high'
                                            low='low'
                                            name={`${feature.name} Forecast`}
                                            type='RangeArea'
                                            fill={feature.color}
                                            opacity={0.5}
                                            visible={false}
                                            dashArray='1'
                                        />
                                    );
                                }
                            })}

                            {chartData?.milestones.map((milestone, index) => (
                                <SeriesDirective
                                    key={index}
                                    dataSource={[
                                        { x: milestone.date, y: chartData.maxRemainingItems, name: milestone.name },
                                        { x: milestone.date, y: 0, name: milestone.name }
                                    ]}
                                    xName='x'
                                    yName='y'
                                    name={milestone.name}
                                    fill="orange"
                                    opacity={0.7}
                                    type='StepLine'
                                    width={2}
                                    dashArray='5'
                                    marker={{ isFilled: false, visible: true, width: 4, height: 4 }}
                                    tooltipMappingName='name'
                                    tooltipFormat='${point.x}'
                                />
                            ))}
                        </SeriesCollectionDirective>
                    </ChartComponent>
                </Grid>
            </LoadingAnimation>
        </Grid>
    );
};

export default LighthouseChartComponent;