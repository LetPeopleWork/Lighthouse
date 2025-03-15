import { CircularProgress } from "@mui/material";
import {
  ChartsReferenceLine,
  ChartsTooltip,
  ChartsXAxis,
  ChartsYAxis,
  ResponsiveChartContainer,
  ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import type { ITeam } from "../../../models/Team/Team";

interface CycleTimeScatterPlotChartProps {
  team: ITeam;
}

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
  team,
}) => {
  // Hardcoded data for now
  const generateMockData = () => {
    const today = new Date();
    const data = [];

    // Generate 20 random data points within the last 90 days
    for (let i = 0; i < 20; i++) {
      const daysAgo = Math.floor(Math.random() * 90);
      const cycleTime = Math.floor(Math.random() * 20) + 1; // 1 to 20 days

      const date = new Date(today);
      date.setDate(date.getDate() - daysAgo);

      data.push({
        x: date.getTime(), // timestamp
        y: cycleTime, // days
        id: `item-${i}`,
        url: `https://example.com/work-item/${i}`,
      });
    }

    return data;
  };

  const data = generateMockData();

  // Calculate percentiles (50th, 70th, 85th, 95th)
  const calculatePercentiles = (data: any[]) => {
    const values = [...data.map((item) => item.y)].sort((a, b) => a - b);
    const getPercentile = (p: number) => {
      const pos = (values.length * p) / 100 - 1;
      const index = Math.ceil(pos);

      if (index < 0) return values[0];
      if (index >= values.length) return values[values.length - 1];

      return values[index];
    };

    return {
      p50: getPercentile(50),
      p70: getPercentile(70),
      p85: getPercentile(85),
      p95: getPercentile(95),
    };
  };

  const percentiles = calculatePercentiles(data);

  const handleItemClick = (itemId: number) => {
    const item = data.find((d) => d.id === `item-${itemId}`);
    if (item) {
      window.open(item.url, "_blank");
    }
  };

  const formatValue = (value: any) => {
    if (value && typeof value.x === "number") {
      return `Date: ${new Date(value.x).toLocaleDateString()}, Cycle Time: ${value.y} days`;
    }
    return "";
  };

  return data.length > 0 ? (
    <ResponsiveChartContainer
      height={500}
      
      xAxis={[
        {
          id: "timeAxis",
          scaleType: 'time',
          label: 'Date',
        }
      ]}
      yAxis={[
        {
          id: "cycleTimeAxis",
          scaleType: 'linear',
          label: 'Cycle Time (days)',
          min: 0,
        }
      ]}
      series={[
        {
          type: 'scatter',
          data: data,
          xAxisId: "timeAxis",
          yAxisId: "cycleTimeAxis",
          color: "rgba(48, 87, 78, 1)",
          valueFormatter: formatValue,
          markerSize: 6,
          // Customize marker appearance
          highlightScope: { highlighted: 'item', faded: 'global' },
        }
      ]}
    >
      {/* Add reference lines for each percentile */}
      <ChartsReferenceLine 
        y={percentiles.p50} 
        label={`50th percentile: ${percentiles.p50} days`}
        labelAlign="start"
        lineStyle={{ stroke: 'green', strokeWidth: 1, strokeDasharray: '5 5' }}
      />
      <ChartsReferenceLine 
        y={percentiles.p70} 
        label={`70th percentile: ${percentiles.p70} days`}
        labelAlign="start"
        lineStyle={{ stroke: 'blue', strokeWidth: 1, strokeDasharray: '5 5' }}
      />
      <ChartsReferenceLine 
        y={percentiles.p85} 
        label={`85th percentile: ${percentiles.p85} days`}
        labelAlign="start"
        lineStyle={{ stroke: 'orange', strokeWidth: 1, strokeDasharray: '5 5' }}
      />
      <ChartsReferenceLine 
        y={percentiles.p95} 
        label={`95th percentile: ${percentiles.p95} days`}
        labelAlign="start"
        lineStyle={{ stroke: 'red', strokeWidth: 1, strokeDasharray: '5 5' }}
      />
      
      <ChartsXAxis />
      <ChartsYAxis />
      <ScatterPlot
        onItemClick={(event, itemData) => {
          console.log('Item clicked:', itemData);
          if (itemData?.dataIndex) {
            handleItemClick(itemData.dataIndex);
          }
        }}
      />
      <ChartsTooltip 
        trigger="item" 
      />
    </ResponsiveChartContainer>
  ) : (
    <CircularProgress />
  );
};

export default CycleTimeScatterPlotChart;