import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
} from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import { COULD_NOT_LOAD_MESSAGE } from "../../Common/MetricsView/WidgetShell";
import PortfolioMetricsView from "./PortfolioMetricsView";

interface BaseMetricsViewProps {
	doingStates: string[];
	doingStatesStatus?: "loading" | "error" | "ready";
}

// The aging widget's frame is the real one, so the words on screen are the words a reader sees.
vi.mock("../../Common/MetricsView/BaseMetricsView", async () => {
	const { default: WidgetShell } = await import(
		"../../Common/MetricsView/WidgetShell"
	);
	return {
		BaseMetricsView: ({
			doingStates,
			doingStatesStatus,
		}: BaseMetricsViewProps) => (
			<WidgetShell widgetKey="aging" status={doingStatesStatus}>
				<div data-testid="doing-columns">{doingStates.join("|")}</div>
			</WidgetShell>
		),
	};
});

const aPortfolio = () => {
	const portfolio = new Portfolio();
	portfolio.id = 3;
	portfolio.name = "Ocean Explorer";
	return portfolio;
};

const statusOfTheFrame = () =>
	screen.getByTestId("widget-shell-aging").dataset.widgetStatus;

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

	it("shows a loading frame, and no columns, while the settings are on their way", () => {
		getPortfolioSettings.mockReturnValue(new Promise(() => {}));

		render(view());

		expect(statusOfTheFrame()).toBe("loading");
		expect(screen.getByTestId("doing-columns")).toBeEmptyDOMElement();
	});

	it("says the columns could not be loaded when the settings fetch fails", async () => {
		getPortfolioSettings.mockRejectedValue(new Error("offline"));

		render(view());

		await waitFor(() => expect(statusOfTheFrame()).toBe("error"));
		expect(screen.getByText(COULD_NOT_LOAD_MESSAGE)).toBeVisible();
	});

	it("shows the columns as the settings display them, groups in place of their states", async () => {
		getPortfolioSettings.mockResolvedValue({
			...createMockTeamSettings(),
			doingStates: ["Analysis", "Code Review", "Testing"],
			doingDisplayStates: ["Analysis", "Review"],
		});

		render(view());

		await waitFor(() => expect(statusOfTheFrame()).toBe("ready"));
		expect(screen.getByTestId("doing-columns")).toHaveTextContent(
			"Analysis|Review",
		);
	});
});
