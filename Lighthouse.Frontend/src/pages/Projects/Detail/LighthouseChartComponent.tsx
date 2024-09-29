import React from 'react';
import {
    ChartComponent,
    SeriesCollectionDirective,
    SeriesDirective,
    Inject,
    Category,
    ColumnSeries,
    Highlight,
    DataLabel,
    Tooltip,
    Legend,
    LineSeries,
    AreaSeries,
    ChartAnnotation,
    DateTime,
    RangeAreaSeries,
    StepLineSeries,
} from '@syncfusion/ej2-react-charts';

const LighthouseChartComponent: React.FC = () => {

    const column1 = [
        { x: new Date("2024-09-01"), y: 20, name:'Feature 1' },
        { x: new Date("2024-09-08"), y: 17 },
        { x: new Date("2024-09-15"), y: 11 },
        { x: new Date("2024-09-22"), y: 7 },
        { x: new Date("2024-09-29"), y: 4 },
        { x: new Date("2024-10-15"), y: 0 }
    ];

    const column2 = [
        { x: new Date("2024-09-01"), y: 35 },
        { x: new Date("2024-09-08"), y: 35 },
        { x: new Date("2024-09-15"), y: 29 },
        { x: new Date("2024-09-22"), y: 25 },
        { x: new Date("2024-09-29"), y: 19 },
        { x: new Date("2024-10-15"), y: 0 }
    ];

    const column3 = [
        { x: new Date("2024-09-01"), y: 15 },
        { x: new Date("2024-09-08"), y: 11 },
        { x: new Date("2024-09-15"), y: 5 },
        { x: new Date("2024-09-22"), y: 1 },
        { x: new Date("2024-09-29"), y: 0 },
        { x: new Date("2024-10-15"), y: 0 }
    ];

    const forecast = [
        {x: new Date("2024-09-29"), high: 4, low: 4 },
        {x: new Date("2024-10-01"), high:  2.85, low: 0 },
        {x: new Date("2024-10-07"), high: 0, low: 0 },
    ]

    const milestone = [
        {x: new Date("2024-10-05"), y: 50, name: "Milestone 1"},
        {x: new Date("2024-10-05"), y: 0, name: "Milestone 1" }
    ]

    return (
        <div>
            <ChartComponent
                id='lighthouse-chart'
                theme='Material3'
                legendSettings={{ enableHighlight: true, position: 'Top' }}
                primaryXAxis={{ labelRotation: 0, valueType: 'DateTime', labelFormat: 'dd.MM.yyyy', interval: 7, intervalType: 'Days', majorGridLines: { width: 0 }, majorTickLines: { width: 0 } }}
                primaryYAxis={{ title: '# Of Items Left', }}
                title='Feature Burndown'
                tooltip={{ enable: true, header: "<b>${point.tooltip}</b>", shared: false }}
            >
                <Inject services={[ColumnSeries, ChartAnnotation, AreaSeries, StepLineSeries, LineSeries, Legend, Tooltip, Category, DataLabel, Highlight, DateTime, RangeAreaSeries]} />

                <SeriesCollectionDirective>
                
                    <SeriesDirective dataSource={column1} tooltipMappingName='name' xName='x' columnSpacing={0.1} yName='y' name='Feature 1' type='Column' fill='purple' />
                    <SeriesDirective dataSource={forecast} tooltipMappingName='name' xName='x' high='high' low='low'  name='Feature 1 Forecast' type='RangeArea' fill="purple" opacity={0.5} dashArray='1' />

                    <SeriesDirective dataSource={column2} xName='x' columnSpacing={0.1} yName='y' name='Feature 2' type='Column' />
                    <SeriesDirective dataSource={column3} xName='x' columnSpacing={0.1} yName='y' name='Feature 3' type='Column' />

                    <SeriesDirective dataSource={milestone} xName='x' yName='y' name='Milestone 1' type='StepLine' width={2} dashArray='5' marker={{ isFilled: false, visible: true, width: 4, height: 4 }} tooltipMappingName='name' tooltipFormat='${point.x}'/>

                </SeriesCollectionDirective>
            </ChartComponent>
        </div>
    );
};

export default LighthouseChartComponent;
