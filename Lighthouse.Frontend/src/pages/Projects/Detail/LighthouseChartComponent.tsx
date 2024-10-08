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
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { ILighthouseChartData, ILighthouseChartFeatureData } from '../../../models/Charts/LighthouseChartData';

interface LighthouseChartComponentProps {
    projectId: number;
}

const LighthouseChartComponent: React.FC<LighthouseChartComponentProps> = ({ projectId }) => {
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [chartData, setChartData] = useState<ILighthouseChartData>();

    const { chartService } = useContext(ApiServiceContext);

    const fetchLighthouseData = async () => {
        try {
            setIsLoading(true);
            const lighthouseChartData = await chartService.getLighthouseChartData(projectId)

            if (lighthouseChartData) {
                setChartData(lighthouseChartData);
            }
            else {
                setHasError(true);
            }

            setIsLoading(false);
        } catch (error) {
            console.error('Error fetching project data:', error);
            setHasError(true);
        }
    }

    useEffect(() => {
        fetchLighthouseData();
    }, []);


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
        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
            {chartData ? (
                <ChartComponent
                    id='lighthouse-chart'
                    theme='Material3'
                    legendSettings={{ enableHighlight: true, position: 'Top' }}
                    primaryXAxis={{ labelRotation: 0, valueType: 'DateTime', labelFormat: 'dd.MM.yyyy', minimum: chartData?.startDate, maximum: chartData?.endDate, interval: 7, intervalType: 'Days', majorGridLines: { width: 0 }, majorTickLines: { width: 0 } }}
                    primaryYAxis={{ title: '# Of Items Left', maximum: chartData?.maxRemainingItems }}
                    title='Feature Burndown'
                    tooltip={{ enable: true, header: "<b>${point.tooltip}</b>", shared: false }}
                >
                    <Inject services={[ColumnSeries, StepLineSeries, Legend, Tooltip, DateTime, RangeAreaSeries, Highlight ]} />

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
            ) : (<></>)}
        </LoadingAnimation>
    );
};

export default LighthouseChartComponent;
