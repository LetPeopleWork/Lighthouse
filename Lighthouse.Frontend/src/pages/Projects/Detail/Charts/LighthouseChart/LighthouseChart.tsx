import {
	ChartComponent,
	ColumnSeries,
	DateTime,
	Highlight,
	Inject,
	Legend,
	RangeAreaSeries,
	SeriesCollectionDirective,
	SeriesDirective,
	StepLineSeries,
	Tooltip,
} from "@syncfusion/ej2-react-charts";
import type React from "react";
import type {
	ILighthouseChartData,
	ILighthouseChartFeatureData,
} from "../../../../../models/Charts/LighthouseChartData";

interface LighthouseChartProps {
	data: ILighthouseChartData | undefined;
}

const LighthouseChart: React.FC<LighthouseChartProps> = ({ data }) => {
	function createForecastForFeature(
		feature: ILighthouseChartFeatureData,
	): { x: Date; high: number; low: number }[] {
		const forecast = [];
		const lastBurndownEntry =
			feature.remainingItemsTrend[feature.remainingItemsTrend.length - 1];

		forecast.push({
			x: lastBurndownEntry.date,
			high: lastBurndownEntry.remainingItems,
			low: lastBurndownEntry.remainingItems,
		});

		const earliestForecastDate = feature.forecasts[0];
		const latestForecastDate = feature.forecasts[feature.forecasts.length - 1];

		const highSecondEntry =
			lastBurndownEntry.remainingItems *
			(1 -
				(earliestForecastDate.getTime() - lastBurndownEntry.date.getTime()) /
					(latestForecastDate.getTime() - lastBurndownEntry.date.getTime()));

		forecast.push({
			x: earliestForecastDate,
			high: highSecondEntry,
			low: 0,
		});

		forecast.push({
			x: latestForecastDate,
			high: 0,
			low: 0,
		});

		return forecast;
	}

	return (
		<ChartComponent
			id="lighthouse-chart"
			theme="Material3"
			legendSettings={{ enableHighlight: true, position: "Top" }}
			primaryXAxis={{
				labelRotation: 0,
				valueType: "DateTime",
				labelFormat: "dd.MM.yyyy",
				minimum: data?.startDate,
				maximum: data?.endDate,
				interval: 7,
				intervalType: "Days",
				majorGridLines: { width: 0 },
				majorTickLines: { width: 0 },
			}}
			primaryYAxis={{
				title: "# Of Items Left",
				maximum: data?.maxRemainingItems,
			}}
			title="Feature Burndown"
			tooltip={{
				enable: true,
				header: "<b>${point.tooltip}</b>",
				shared: false,
			}}
		>
			<Inject
				services={[
					ColumnSeries,
					StepLineSeries,
					Legend,
					Tooltip,
					DateTime,
					RangeAreaSeries,
					Highlight,
				]}
			/>
			<SeriesCollectionDirective>
				{data?.features.map((feature) => (
					<SeriesDirective
						key={feature.name}
						dataSource={feature.remainingItemsTrend.map((entry) => ({
							x: entry.date,
							y: entry.remainingItems,
							name: feature.name,
						}))}
						tooltipMappingName="name"
						xName="x"
						yName="y"
						name={feature.name}
						type="Column"
						fill={feature.color}
						columnSpacing={0.1}
						data-testid={`feature-series-${feature.name}`}
					/>
				))}

				{data?.features.map((feature) => {
					if (feature.forecasts.length > 0) {
						const forecastData = createForecastForFeature(feature);
						return (
							<SeriesDirective
								key={feature.name}
								dataSource={forecastData}
								tooltipMappingName="name"
								xName="x"
								high="high"
								low="low"
								name={`${feature.name} Forecast`}
								type="RangeArea"
								fill={feature.color}
								opacity={0.5}
								visible={false}
								dashArray="1"
							/>
						);
					}
				})}

				{data?.milestones.map((milestone) => (
					<SeriesDirective
						key={milestone.id}
						dataSource={[
							{
								x: milestone.date,
								y: data.maxRemainingItems,
								name: milestone.name,
							},
							{ x: milestone.date, y: 0, name: milestone.name },
						]}
						xName="x"
						yName="y"
						name={milestone.name}
						fill="orange"
						opacity={0.7}
						type="StepLine"
						width={2}
						dashArray="5"
						marker={{ isFilled: false, visible: true, width: 4, height: 4 }}
						tooltipMappingName="name"
						tooltipFormat="${point.x}"
					/>
				))}
			</SeriesCollectionDirective>
		</ChartComponent>
	);
};

export default LighthouseChart;
