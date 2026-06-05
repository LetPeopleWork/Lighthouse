import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFlowEfficiencyInfo } from "../../../models/Metrics/FlowEfficiencyInfo";
import type { ITeamMetricsService } from "../../../services/Api/MetricsService";
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

function createService(info: IFlowEfficiencyInfo): ITeamMetricsService {
	const service = createMockTeamMetricsService();
	service.getFlowEfficiencyInfoForTeam = vi.fn().mockResolvedValue(info);
	service.getFlowEfficiencyInfoForPortfolio = vi.fn().mockResolvedValue(info);
	return service;
}

function renderWidget(info: IFlowEfficiencyInfo): ITeamMetricsService {
	const service = createService(info);
	render(
		<TestWrapper>
			<FlowEfficiencyOverviewWidget
				entityId={7}
				metricsService={service}
				ownerType="team"
				startDate={new Date("2023-01-01")}
				endDate={new Date("2023-01-31")}
			/>
		</TestWrapper>,
	);
	return service;
}

describe("FlowEfficiencyOverviewWidget", () => {
	it("renders the aggregate efficiency percentage with the green inverted-RAG colour", async () => {
		renderWidget(getMockFlowEfficiencyInfo({ efficiencyPercent: 72 }));

		await waitFor(() => {
			const value = screen.getByTestId("flow-efficiency-percent");
			expect(value).toHaveTextContent("72%");
			expect(value).toHaveStyle({ color: "#2e7d32" });
		});
	});

	it("colours a low efficiency percentage red because higher efficiency is better", async () => {
		renderWidget(getMockFlowEfficiencyInfo({ efficiencyPercent: 25 }));

		await waitFor(() => {
			const value = screen.getByTestId("flow-efficiency-percent");
			expect(value).toHaveTextContent("25%");
			expect(value).toHaveStyle({ color: "#d32f2f" });
		});
	});

	it("shows a distinct not-configured read that never reports 100%", async () => {
		renderWidget(
			getMockFlowEfficiencyInfo({
				isConfigured: false,
				hasDataInScope: false,
				efficiencyPercent: 0,
			}),
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("flow-efficiency-not-configured"),
			).toBeInTheDocument();
		});
		expect(screen.queryByTestId("flow-efficiency-percent")).toBeNull();
		expect(screen.queryByText("100%")).toBeNull();
	});

	it("shows a distinct no-data-in-scope read when configured but nothing is in scope", async () => {
		renderWidget(
			getMockFlowEfficiencyInfo({
				isConfigured: true,
				hasDataInScope: false,
				efficiencyPercent: 0,
			}),
		);

		await waitFor(() => {
			expect(screen.getByTestId("flow-efficiency-no-data")).toBeInTheDocument();
		});
		expect(screen.queryByTestId("flow-efficiency-not-configured")).toBeNull();
		expect(screen.queryByTestId("flow-efficiency-percent")).toBeNull();
	});

	it("fetches whole-set efficiency through the portfolio port when owned by a portfolio", async () => {
		const info = getMockFlowEfficiencyInfo();
		const service = createService(info);
		render(
			<TestWrapper>
				<FlowEfficiencyOverviewWidget
					entityId={9}
					metricsService={service}
					ownerType="portfolio"
					startDate={new Date("2023-01-01")}
					endDate={new Date("2023-01-31")}
				/>
			</TestWrapper>,
		);

		await waitFor(() => {
			expect(service.getFlowEfficiencyInfoForPortfolio).toHaveBeenCalledWith(
				9,
				new Date("2023-01-01"),
				new Date("2023-01-31"),
			);
		});
		expect(service.getFlowEfficiencyInfoForTeam).not.toHaveBeenCalled();
	});
});
