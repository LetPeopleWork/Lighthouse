import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFlowEfficiencyInfo } from "../../../models/Metrics/FlowEfficiencyInfo";
import { createMockTeamMetricsService } from "../../../tests/MockApiServiceProvider";
import FlowEfficiencyOverviewWidget from "./FlowEfficiencyOverviewWidget";

const theme = createTheme();

const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
	<ThemeProvider theme={theme}>{children}</ThemeProvider>
);

function getMockFlowEfficiencyInfo(
	overrides?: Partial<IFlowEfficiencyInfo>,
): IFlowEfficiencyInfo {
	return {
		isConfigured: true,
		hasDataInScope: true,
		efficiencyPercent: 72,
		totalDoingDays: 40,
		waitDays: 11,
		...overrides,
	};
}

function renderWidget(info: IFlowEfficiencyInfo | null) {
	render(
		<TestWrapper>
			<FlowEfficiencyOverviewWidget info={info} />
		</TestWrapper>,
	);
}

describe("FlowEfficiencyOverviewWidget", () => {
	it("renders the aggregate efficiency percentage with the green inverted-RAG colour", () => {
		renderWidget(getMockFlowEfficiencyInfo({ efficiencyPercent: 72 }));

		const value = screen.getByTestId("flow-efficiency-percent");
		expect(value).toHaveTextContent("72%");
		expect(value).toHaveStyle({ color: "#2e7d32" });
	});

	it("colours a low efficiency percentage red because higher efficiency is better", () => {
		renderWidget(getMockFlowEfficiencyInfo({ efficiencyPercent: 25 }));

		const value = screen.getByTestId("flow-efficiency-percent");
		expect(value).toHaveTextContent("25%");
		expect(value).toHaveStyle({ color: "#d32f2f" });
	});

	it("shows a distinct not-configured read that never reports 100%", () => {
		renderWidget(
			getMockFlowEfficiencyInfo({
				isConfigured: false,
				hasDataInScope: false,
				efficiencyPercent: 0,
			}),
		);

		expect(
			screen.getByTestId("flow-efficiency-not-configured"),
		).toBeInTheDocument();
		expect(screen.queryByTestId("flow-efficiency-percent")).toBeNull();
		expect(screen.queryByText("100%")).toBeNull();
	});

	it("shows a distinct no-data-in-scope read when configured but nothing is in scope", () => {
		renderWidget(
			getMockFlowEfficiencyInfo({
				isConfigured: true,
				hasDataInScope: false,
				efficiencyPercent: 0,
			}),
		);

		expect(screen.getByTestId("flow-efficiency-no-data")).toBeInTheDocument();
		expect(screen.queryByTestId("flow-efficiency-not-configured")).toBeNull();
		expect(screen.queryByTestId("flow-efficiency-percent")).toBeNull();
	});

	it("renders from its props alone, without reaching for the metrics service", () => {
		// Story 5508 slice 05 / D7: the widget used to self-fetch in its own effect, which is
		// precisely why it had no RAG footer — buildWidgetFooters never saw its data. The fetch now
		// lives in useMetricsData, so the widget must be purely presentational.
		//
		// Superseded here: "fetches whole-set efficiency through the portfolio port when owned by a
		// portfolio". That routing is no longer the widget's job; it moved to useMetricsData and is
		// covered by BaseMetricsView.test.tsx scenario 46 ("fetches flow efficiency exactly once,
		// through the shared data layer"). Deleted rather than contorted — the coverage moved, it
		// was not lost.
		const service = createMockTeamMetricsService();
		service.getFlowEfficiencyInfoForTeam = vi.fn();
		service.getFlowEfficiencyInfoForPortfolio = vi.fn();

		renderWidget(getMockFlowEfficiencyInfo());

		expect(service.getFlowEfficiencyInfoForTeam).not.toHaveBeenCalled();
		expect(service.getFlowEfficiencyInfoForPortfolio).not.toHaveBeenCalled();
	});
});
