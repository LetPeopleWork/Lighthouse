import { render, screen } from '@testing-library/react';
import { Throughput } from '../../../models/Forecasts/Throughput';
import ThroughputBarChart from './ThroughputChart';

describe('ThroughputBarChart component', () => {
    it('should render BarChart when throughputData.history > 0', () => {
        const mockThroughputData = new Throughput([10, 20, 30]);

        render(<ThroughputBarChart throughputData={mockThroughputData} />);

        const svgElement = document.querySelector('.css-1evyvmv-MuiChartsSurface-root');
        const barElements = svgElement?.querySelectorAll('rect');
        expect(barElements?.length).toBeGreaterThan(0);
    });

    it('should render CircularProgress when throughputData.history <= 0', () => {
        const mockThroughputData = new Throughput([]);

        render(<ThroughputBarChart throughputData={mockThroughputData} />);

        const circularProgressElement = screen.getByRole('progressbar');
        expect(circularProgressElement).toBeInTheDocument();
    });
});
