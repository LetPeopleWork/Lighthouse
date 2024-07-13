import React from 'react';
import { BarChart } from '@mui/x-charts';
import { IThroughput } from '../../../models/Forecasts/Throughput';
import { CircularProgress } from '@mui/material';

interface ThroughputBarChartProps {
    throughputData: IThroughput;
}

const ThroughputBarChart: React.FC<ThroughputBarChartProps> = ({ throughputData }) => {

    const data = Array.from({ length: throughputData.history }, (_, index) => {
        const currentDate = new Date();
        const targetDate = new Date(currentDate);
        const reversedIndex = throughputData.history - 1 - index;
        targetDate.setDate(currentDate.getDate() - reversedIndex - length);
    
        return {
            day: targetDate.toLocaleDateString(),
            throughput: throughputData.getThroughputOnDay(reversedIndex),
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
