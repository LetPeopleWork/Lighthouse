import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
} from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import PortfolioMetricsView from "./PortfolioMetricsView";

interface BaseMetricsViewProps {
	doingStates: string[];
	doingStatesStatus?: string;
}

vi.mock("../../Common/MetricsView/BaseMetricsView", () => ({
	BaseMetricsView: ({
		doingStates,
		doingStatesStatus,
	}: BaseMetricsViewProps) => (
		<div>
			<div data-testid="doing-columns">{doingStates.join("|")}</div>
			<div data-testid="doing-columns-status">{doingStatesStatus}</div>
		</div>
	),
}));

const aPortfolio = () => {
	const portfolio = new Portfolio();
	portfolio.id = 3;
	portfolio.name = "Ocean Explorer";
	return portfolio;
};

describe("PortfolioMetricsView aging columns", () => {
	const getPortfolioSettings = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	const view = () => {
		const portfolioService = createMockPortfolioService();
		portfolioService.getPortfolioSettings = getPortfolioSettings;
		return (
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ portfolioService })}
			>
				<PortfolioMetricsView portfolio={aPortfolio()} />
			</ApiServiceContext.Provider>
		);
	};

	it.skip("says the columns are loading, and shows none, while the settings are on their way", () => {
		getPortfolioSettings.mockReturnValue(new Promise(() => {}));

		render(view());

		expect(screen.getByTestId("doing-columns-status")).toHaveTextContent(
			"loading",
		);
		expect(screen.getByTestId("doing-columns")).toBeEmptyDOMElement();
	});

	it.skip("says the columns could not be loaded when the settings fetch fails", async () => {
		getPortfolioSettings.mockRejectedValue(new Error("offline"));

		render(view());

		await waitFor(() =>
			expect(screen.getByTestId("doing-columns-status")).toHaveTextContent(
				"error",
			),
		);
	});

	it.skip("shows the columns as the settings display them, groups in place of their states", async () => {
		getPortfolioSettings.mockResolvedValue({
			...createMockTeamSettings(),
			doingStates: ["Analysis", "Code Review", "Testing"],
			doingDisplayStates: ["Analysis", "Review"],
		});

		render(view());

		await waitFor(() =>
			expect(screen.getByTestId("doing-columns-status")).toHaveTextContent(
				"ready",
			),
		);
		expect(screen.getByTestId("doing-columns")).toHaveTextContent(
			"Analysis|Review",
		);
	});
});
