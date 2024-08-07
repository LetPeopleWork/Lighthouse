import React from 'react';
import { BarChart } from '@mui/x-charts';
import { IThroughput } from '../../../models/Forecasts/Throughput';
import { CircularProgress } from '@mui/material';

interface ThroughputBarChartProps {
    throughputData: IThroughput;
}

const ThroughputBarChart: React.FC<ThroughputBarChartProps> = ({ throughputData }) => {

    const data = Array.from({ length: throughputData.history }, (_, index) => {
        const targetDate = new Date();
        targetDate.setDate(targetDate.getDate() - index - length);
    
        return {
            day: targetDate.toLocaleDateString(),
            throughput: throughputData.getThroughputOnDay(index),
        };
    });    

    return (
        throughputData.history > 0 ?
            <BarChart
                dataset={data}
                xAxis={[{ scaleType: 'band', dataKey: 'day' }]}
                series={[
                    { dataKey: 'throughput', label: 'Throughput', color: 'rgba(48, 87, 78, 1)' },
                ]}
                height={500}
            />
            :
            <CircularProgress />
    );
};

export default ThroughputBarChart;
