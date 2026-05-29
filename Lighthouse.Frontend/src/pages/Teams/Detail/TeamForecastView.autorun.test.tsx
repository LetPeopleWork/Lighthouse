import { act, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import TeamForecastView from "./TeamForecastView";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUseNewItemForecaster: true,
		newItemForecasterTooltip: "",
		isLoading: false,
		licenseStatus: {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		},
	}),
}));

vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	default: ({ children }: { children: React.ReactNode }) => (
		<div data-testid="input-group">{children}</div>
	),
}));

vi.mock("./ManualForecaster", () => ({
	default: () => <div data-testid="manual-forecaster" />,
}));

vi.mock("./NewItemForecaster", () => ({
	default: (props: Record<string, unknown>) => {
		const onInputChange = props.onInputChange as
			| ((complete: boolean) => void)
			| undefined;
		return (
			<div data-testid="new-item-forecaster">
				<button
					type="button"
					data-testid="widen-new-item-window"
					onClick={() => onInputChange?.(true)}
				>
					Widen historical window
				</button>
				<button
					type="button"
					data-testid="clear-new-item-types"
					onClick={() => onInputChange?.(false)}
				>
					Clear work item types
				</button>
			</div>
		);
	},
}));

vi.mock("./BacktestForecaster", () => ({
	default: (props: Record<string, unknown>) => {
		const onInputChange = props.onInputChange as
			| ((complete: boolean) => void)
			| undefined;
		return (
			<div data-testid="backtest-forecaster">
				<button
					type="button"
					data-testid="adjust-backtest-window"
					onClick={() => onInputChange?.(true)}
				>
					Adjust historical window
				</button>
				<button
					type="button"
					data-testid="toggle-backtest-mode-incomplete"
					onClick={() => onInputChange?.(false)}
				>
					Switch mode mid-edit
				</button>
			</div>
		);
	},
}));

const DEBOUNCE_MS = 300;

const atlasTeam: Team = {
	id: 42,
	name: "Atlas",
	workItemTypes: ["User Story", "Bug"],
} as Team;

const forecastService = {
	runManualForecast: vi.fn(),
	runItemPrediction: vi.fn().mockResolvedValue({}),
	runBacktest: vi.fn().mockResolvedValue({}),
};

const teamMetricsService = {
	getForecastInputCandidates: vi.fn().mockResolvedValue({
		currentWipCount: 3,
		backlogCount: 12,
		features: [],
	}),
};

const apiServiceContext = createMockApiServiceContext({
	forecastService,
	teamMetricsService:
		teamMetricsService as unknown as IApiServiceContext["teamMetricsService"],
});

const renderForecastView = () =>
	render(
		<SnackbarErrorHandler>
			<ApiServiceContext.Provider value={apiServiceContext}>
				<TeamForecastView team={atlasTeam} />
			</ApiServiceContext.Provider>
		</SnackbarErrorHandler>,
	);

void TERMINOLOGY_KEYS;

describe("@US-05 @in-memory auto-run new-item forecast", () => {
	beforeEach(() => {
		vi.useFakeTimers();
		vi.clearAllMocks();
		forecastService.runItemPrediction.mockResolvedValue({});
	});

	it("@US-05 recomputes the new-item forecast automatically after a valid input change", async () => {
		renderForecastView();
		await act(async () => {
			await vi.runAllTimersAsync();
		});
		forecastService.runItemPrediction.mockClear();

		act(() => {
			screen.getByTestId("widen-new-item-window").click();
		});
		act(() => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(forecastService.runItemPrediction).toHaveBeenCalledTimes(1);
	});

	it.skip("@US-05 @error fires no run on page load until an input changes", async () => {
		renderForecastView();
		await screen.findByTestId("new-item-forecaster");

		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(forecastService.runItemPrediction).not.toHaveBeenCalled();
	});

	it.skip("@US-05 @error fires no run when the inputs become incomplete", async () => {
		renderForecastView();
		await screen.findByTestId("new-item-forecaster");
		forecastService.runItemPrediction.mockClear();

		act(() => {
			screen.getByTestId("clear-new-item-types").click();
		});
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(forecastService.runItemPrediction).not.toHaveBeenCalled();
	});

	it.skip("@US-05 @error shows only the latest run when input changes arrive in rapid succession", async () => {
		renderForecastView();
		await screen.findByTestId("new-item-forecaster");
		forecastService.runItemPrediction.mockClear();

		act(() => {
			screen.getByTestId("widen-new-item-window").click();
			screen.getByTestId("widen-new-item-window").click();
		});
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() =>
			expect(forecastService.runItemPrediction).toHaveBeenCalledTimes(1),
		);
	});
});

describe.skip("@US-06 @in-memory auto-run backtest", () => {
	beforeEach(() => {
		vi.useFakeTimers();
		vi.clearAllMocks();
		forecastService.runBacktest.mockResolvedValue({});
	});

	it("@US-06 recomputes the backtest automatically after a valid input change", async () => {
		renderForecastView();
		await screen.findByTestId("backtest-forecaster");
		forecastService.runBacktest.mockClear();

		act(() => {
			screen.getByTestId("adjust-backtest-window").click();
		});
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		await waitFor(() =>
			expect(forecastService.runBacktest).toHaveBeenCalledTimes(1),
		);
	});

	it("@US-06 @error fires no run when the mode is switched while inputs are incomplete", async () => {
		renderForecastView();
		await screen.findByTestId("backtest-forecaster");
		forecastService.runBacktest.mockClear();

		act(() => {
			screen.getByTestId("toggle-backtest-mode-incomplete").click();
		});
		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS);
		});

		expect(forecastService.runBacktest).not.toHaveBeenCalled();
	});

	it("@US-06 @error fires no backtest run on page load until an input changes", async () => {
		renderForecastView();
		await screen.findByTestId("backtest-forecaster");

		await act(async () => {
			vi.advanceTimersByTime(DEBOUNCE_MS * 2);
		});

		expect(forecastService.runBacktest).not.toHaveBeenCalled();
	});
});
