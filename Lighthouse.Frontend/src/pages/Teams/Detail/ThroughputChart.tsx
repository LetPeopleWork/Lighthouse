import React from 'react';
import { BarChart } from '@mui/x-charts';
import { Throughput } from '../../../models/Forecasts/Throughput';
import { CircularProgress } from '@mui/material';

interface ThroughputBarChartProps {
    throughputData: number[];
}

const ThroughputBarChart: React.FC<ThroughputBarChartProps> = ({ throughputData }) => {

    const throughput = new Throughput(throughputData);

    const data = Array.from({ length: throughput.history }, (_, index) => {
        const targetDate = new Date();

        const dayIndex = throughput.history - 1 - index;
        targetDate.setDate(targetDate.getDate() - dayIndex);
    
        return {
            day: targetDate.toLocaleDateString(),
            throughput: throughput.getThroughputOnDay(index),
        };
    });    

    return (
        throughput.history > 0 ?
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
