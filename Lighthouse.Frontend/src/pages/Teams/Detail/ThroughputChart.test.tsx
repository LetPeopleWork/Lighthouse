import { render, screen } from "@testing-library/react";
import type { ITeam } from "../../../models/Team/Team";
import ThroughputBarChart from "./ThroughputChart";

describe("ThroughputBarChart component", () => {
	it("should render BarChart when throughputData.history > 0", () => {
		const mockThroughputData = [10, 20, 30];

		const team: ITeam = {
			name: "Team 1",
			id: 1,
			projects: [],
			features: [],
			featureWip: 0,
			featuresInProgress: [],
			lastUpdated: new Date(),
			throughput: mockThroughputData,
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(),
			throughputEndDate: new Date(),
			remainingFeatures: 0,
			remainingWork: 0,
			totalWork: 0,
		};

		render(<ThroughputBarChart team={team} />);

		const svgElement = document.querySelector(
			".css-1evyvmv-MuiChartsSurface-root",
		);
		const barElements = svgElement?.querySelectorAll("rect");
		expect(barElements?.length).toBeGreaterThan(0);
	});

	it("should render CircularProgress when throughputData.history <= 0", () => {
		const mockThroughputData: number[] = [];
		const team: ITeam = {
			name: "Team 1",
			id: 1,
			projects: [],
			features: [],
			featureWip: 0,
			featuresInProgress: [],
			lastUpdated: new Date(),
			throughput: mockThroughputData,
			throughputStartDate: new Date(),
			throughputEndDate: new Date(),
			useFixedDatesForThroughput: false,
			remainingFeatures: 0,
			remainingWork: 0,
			totalWork: 0,
		};

		render(<ThroughputBarChart team={team} />);

		const circularProgressElement = screen.getByRole("progressbar");
		expect(circularProgressElement).toBeInTheDocument();
	});
});
