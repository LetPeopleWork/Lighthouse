import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen } from "@testing-library/react";
import type React from "react";
import { describe, expect, it } from "vitest";
import { appColors } from "../../../utils/theme/colors";
import LoadBalanceMatrixChart from "./LoadBalanceMatrixChart";

type MatrixPoint = {
	readonly date: Date;
	readonly dayOffset: number;
	readonly dateLabel: string;
	readonly wip: number;
	readonly totalWorkItemAge: number;
	readonly opacity: number;
};

type MatrixData = {
	readonly baselineAvailable: boolean;
	readonly averageWip: number | null;
	readonly averageTotalWorkItemAge: number | null;
	readonly points: ReadonlyArray<MatrixPoint>;
};

const theme = createTheme();

const renderWithTheme = (ui: React.ReactElement) =>
	render(<ThemeProvider theme={theme}>{ui}</ThemeProvider>);

function createPoints(input: {
	readonly wip: number;
	readonly startAge: number;
	readonly dailyAgeIncrease: number;
	readonly days?: number;
}): MatrixPoint[] {
	const days = input.days ?? 5;
	return Array.from({ length: days + 1 }, (_, dayOffset) => {
		const pointDate = new Date(Date.UTC(2026, 3, 20 + dayOffset));
		return {
			date: pointDate,
			dayOffset,
			dateLabel: pointDate.toISOString().slice(0, 10),
			wip: input.wip,
			totalWorkItemAge: input.startAge + input.dailyAgeIncrease * dayOffset,
			opacity: Math.max(0.25, 1 - dayOffset * 0.15),
		};
	});
}

function createData(overrides: Partial<MatrixData> = {}): MatrixData {
	return {
		baselineAvailable: true,
		averageWip: 5,
		averageTotalWorkItemAge: 40,
		points: createPoints({ wip: 3, startAge: 10, dailyAgeIncrease: 3 }),
		...overrides,
	};
}

describe("LoadBalanceMatrixChart", () => {
	it("renders chart title, axes labels, and matrix svg", () => {
		renderWithTheme(<LoadBalanceMatrixChart data={createData()} />);

		expect(screen.getByText("Load Balance Matrix")).toBeInTheDocument();
		expect(screen.getByText("Total Work Item Age")).toBeInTheDocument();
		expect(screen.getByText("WIP")).toBeInTheDocument();
		expect(screen.getByTestId("load-balance-matrix-svg")).toHaveAttribute(
			"preserveAspectRatio",
			"none",
		);
	});

	it("renders baseline divider labels and lines when baseline is available", () => {
		renderWithTheme(<LoadBalanceMatrixChart data={createData()} />);

		expect(screen.getByText("WIP Avg: 5")).toBeInTheDocument();
		expect(screen.getByText("TWIA Average: 40")).toBeInTheDocument();
		expect(screen.getByTestId("load-balance-baseline-x")).toHaveAttribute(
			"stroke-dasharray",
			"6 6",
		);
		expect(screen.getByTestId("load-balance-baseline-y")).toHaveAttribute(
			"stroke-dasharray",
			"6 6",
		);
	});

	it("uses a neutral plot background without quadrant status fills", () => {
		renderWithTheme(<LoadBalanceMatrixChart data={createData()} />);

		const svg = screen.getByTestId("load-balance-matrix-svg");
		expect(
			svg.querySelector(`rect[fill="${appColors.status.success}"]`),
		).toBeNull();
		expect(
			svg.querySelector(`rect[fill="${appColors.status.warning}"]`),
		).toBeNull();
		expect(
			svg.querySelector(`rect[fill="${appColors.status.error}"]`),
		).toBeNull();
	});

	it("hides baseline lines when baseline is missing", () => {
		renderWithTheme(
			<LoadBalanceMatrixChart
				data={createData({
					baselineAvailable: false,
					averageWip: null,
					averageTotalWorkItemAge: null,
				})}
			/>,
		);

		expect(screen.queryByText(/WIP Avg:/)).toBeNull();
		expect(screen.queryByText(/TWIA Average:/)).toBeNull();
		expect(screen.queryByTestId("load-balance-baseline-x")).toBeNull();
		expect(screen.queryByTestId("load-balance-baseline-y")).toBeNull();
	});
});
