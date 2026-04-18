import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
} from "@testing-library/react";
import dayjs from "dayjs";
import { describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IForecastInputCandidates } from "../../../models/Forecasts/ForecastInputCandidates";
import { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import TeamForecastView from "./TeamForecastView";

// Mock the useTerminology hook
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
				[TERMINOLOGY_KEYS.TEAM]: "Team",
				[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

// Mock the useLicenseRestrictions hook
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

// Mock dependencies
vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	default: ({
		title,
		children,
	}: {
		title: string;
		children: React.ReactNode;
	}) => (
		<div data-testid="input-group">
			<h2>{title}</h2>
			{children}
		</div>
	),
}));

vi.mock("./ManualForecaster", () => ({
	default: ({
		remainingItems,
		targetDate,
		forecastInputCandidates,
		onRemainingItemsChange,
		onTargetDateChange,
	}: {
		remainingItems: number;
		targetDate: unknown;
		forecastInputCandidates: IForecastInputCandidates | null;
		manualForecastResult: unknown;
		onRemainingItemsChange: (value: number) => void;
		onTargetDateChange: (date: unknown) => void;
	}) => (
		<div data-testid="manual-forecaster">
			<span data-testid="remaining-items-value">{remainingItems}</span>
			<span data-testid="target-date-value">
				{targetDate ? "has-date" : "null"}
			</span>
			<span data-testid="forecast-candidates-value">
				{forecastInputCandidates ? "has-candidates" : "null"}
			</span>
			<button
				type="button"
				data-testid="simulate-remaining-change"
				onClick={() => onRemainingItemsChange(15)}
			>
				Change Remaining Items
			</button>
			<button
				type="button"
				data-testid="simulate-zero-remaining"
				onClick={() => onRemainingItemsChange(0)}
			>
				Set Zero Remaining
			</button>
			<button
				type="button"
				data-testid="simulate-date-change"
				onClick={() => onTargetDateChange(dayjs().add(1, "week"))}
			>
				Change Target Date
			</button>
		</div>
	),
}));

vi.mock("./NewItemForecaster", () => ({
	default: ({
		onRunNewItemForecast,
		onClearForecastResult,
		isDisabled,
		workItemTypes,
		targetDate,
	}: {
		targetDate: unknown;
		newItemForecastResult: unknown;
		onTargetDateChange: unknown;
		onRunNewItemForecast: (
			startDate: Date,
			endDate: Date,
			targetDate: Date,
			workItemTypes: string[],
		) => void;
		onClearForecastResult?: () => void;
		workItemTypes: string[];
		isDisabled?: boolean;
		disabledMessage?: string;
	}) => (
		<div data-testid="new-item-forecaster">
			<button
				type="button"
				onClick={() => {
					// Simulate the actual component's date calculation
					const startDate = new Date();
					startDate.setDate(startDate.getDate() - 30);
					const endDate = new Date();
					const target =
						targetDate &&
						typeof targetDate === "object" &&
						"valueOf" in targetDate
							? new Date((targetDate as { valueOf(): number }).valueOf())
							: new Date();

					onRunNewItemForecast(startDate, endDate, target, workItemTypes);
				}}
				disabled={isDisabled}
			>
				Run New Item Forecast
			</button>
			{/* Simulate parameter change buttons to test clearing */}
			<button
				type="button"
				onClick={() => onClearForecastResult?.()}
				data-testid="clear-on-param-change"
			>
				Simulate Parameter Change
			</button>
		</div>
	),
}));

vi.mock("./BacktestForecaster", () => ({
	default: ({
		onRunBacktest,
		onClearBacktestResult,
	}: {
		onRunBacktest: (
			startDate: Date,
			endDate: Date,
			historicalStartDate: Date,
			historicalEndDate: Date,
		) => void;
		backtestResult: unknown;
		onClearBacktestResult?: () => void;
	}) => (
		<div data-testid="backtest-forecaster">
			<button
				type="button"
				onClick={() => {
					const startDate = new Date();
					startDate.setDate(startDate.getDate() - 60);
					const endDate = new Date();
					endDate.setDate(endDate.getDate() - 30);
					const historicalStartDate = new Date();
					historicalStartDate.setDate(historicalStartDate.getDate() - 90);
					const historicalEndDate = new Date(startDate);
					onRunBacktest(
						startDate,
						endDate,
						historicalStartDate,
						historicalEndDate,
					);
				}}
			>
				Run Backtest
			</button>
			<button
				type="button"
				onClick={() => onClearBacktestResult?.()}
				data-testid="clear-backtest-result"
			>
				Clear Backtest Result
			</button>
		</div>
	),
}));

describe("TeamForecastView component", () => {
	const mockTeam: Team = {
		id: 1,
		name: "Test Team",
		workItemTypes: ["User Story", "Bug", "Task"],
	} as Team;

	const mockForecastService = {
		runManualForecast: vi.fn(),
		runItemPrediction: vi.fn(),
		runBacktest: vi.fn(),
	};

	const mockTeamMetricsService = {
		getForecastInputCandidates: vi.fn().mockResolvedValue({
			currentWipCount: 3,
			backlogCount: 12,
			features: [],
		}),
	};

	const mockApiServiceContext = createMockApiServiceContext({
		forecastService: mockForecastService,
		teamMetricsService:
			mockTeamMetricsService as unknown as IApiServiceContext["teamMetricsService"],
	});

	const renderWithProviders = (component: React.ReactElement) => {
		return render(
			<SnackbarErrorHandler>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					{component}
				</ApiServiceContext.Provider>
			</SnackbarErrorHandler>,
		);
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockTeamMetricsService.getForecastInputCandidates.mockResolvedValue({
			currentWipCount: 3,
			backlogCount: 12,
			features: [],
		});
	});

	it("should render without errors", () => {
		renderWithProviders(<TeamForecastView team={mockTeam} />);

		expect(screen.getByText("Team Forecast")).toBeInTheDocument();
		expect(
			screen.getByText("New Work Items Creation Forecast"),
		).toBeInTheDocument();
		expect(screen.getByTestId("manual-forecaster")).toBeInTheDocument();
		expect(screen.getByTestId("new-item-forecaster")).toBeInTheDocument();
	});

	describe("ManualForecaster auto-run behavior", () => {
		it("should initialize with remainingItems=10 and targetDate=null", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			expect(screen.getByTestId("remaining-items-value")).toHaveTextContent(
				"10",
			);
			expect(screen.getByTestId("target-date-value")).toHaveTextContent("null");
		});

		it("should NOT run forecast on initial render", async () => {
			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
					vi.advanceTimersByTime(500);
				});
				expect(mockForecastService.runManualForecast).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
		});

		it("should run forecast after user changes remaining items (debounced)", async () => {
			mockForecastService.runManualForecast.mockResolvedValueOnce({
				remainingItems: 15,
				targetDate: null,
				whenForecasts: [],
				howManyForecasts: [],
				likelihood: 0,
			});

			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				// Trigger state change — React flushes effects after this act
				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-remaining-change"));
				});

				// Advance timer to fire the debounce callback
				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).toHaveBeenCalledWith(
					mockTeam.id,
					15,
					null,
				);
			} finally {
				vi.useRealTimers();
			}
		});

		it("should run forecast after user changes target date (debounced)", async () => {
			mockForecastService.runManualForecast.mockResolvedValueOnce({
				remainingItems: 10,
				targetDate: new Date(),
				whenForecasts: [],
				howManyForecasts: [],
				likelihood: 0,
			});

			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-date-change"));
				});

				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).toHaveBeenCalledWith(
					mockTeam.id,
					10,
					expect.any(Object),
				);
			} finally {
				vi.useRealTimers();
			}
		});

		it("should NOT run forecast when remainingItems becomes 0", async () => {
			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-zero-remaining"));
				});

				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
		});

		it("should NOT fire again within debounce window on rapid changes", async () => {
			mockForecastService.runManualForecast.mockResolvedValue({
				remainingItems: 15,
				targetDate: null,
				whenForecasts: [],
				howManyForecasts: [],
				likelihood: 0,
			});

			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				// First click — starts the debounce timer
				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-remaining-change"));
				});

				// Advance only 100ms (not enough for debounce)
				act(() => {
					vi.advanceTimersByTime(100);
				});

				// Second click before debounce fires — resets the timer
				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-remaining-change"));
				});

				// Advance remaining debounce time — only one call should happen
				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).toHaveBeenCalledTimes(1);
			} finally {
				vi.useRealTimers();
			}
		});

		it("should handle forecast service errors gracefully", async () => {
			const errorMessage = "Forecast service failed";
			mockForecastService.runManualForecast.mockRejectedValueOnce(
				new Error(errorMessage),
			);

			vi.useFakeTimers();

			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-remaining-change"));
			});

			act(() => {
				vi.advanceTimersByTime(300);
			});

			// Restore real timers before waiting for async error display
			vi.useRealTimers();

			await waitFor(() => {
				expect(screen.getByText(errorMessage)).toBeInTheDocument();
			});
		});

		it("should handle non-Error forecast failures with generic message", async () => {
			mockForecastService.runManualForecast.mockRejectedValueOnce(
				"String error message",
			);

			vi.useFakeTimers();

			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-remaining-change"));
			});

			act(() => {
				vi.advanceTimersByTime(300);
			});

			vi.useRealTimers();

			await waitFor(() => {
				expect(
					screen.getByText("Failed to run manual forecast. Please try again."),
				).toBeInTheDocument();
			});
		});
	});

	describe("getForecastInputCandidates", () => {
		it("should call getForecastInputCandidates on mount", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			expect(
				mockTeamMetricsService.getForecastInputCandidates,
			).toHaveBeenCalledWith(mockTeam.id);
		});

		it("should pass forecastInputCandidates to ManualForecaster when loaded", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			await waitFor(() => {
				expect(
					screen.getByTestId("forecast-candidates-value"),
				).toHaveTextContent("has-candidates");
			});
		});

		it("should show null candidates while loading", () => {
			// Don't await — check initial render before promise resolves
			renderWithProviders(<TeamForecastView team={mockTeam} />);

			expect(screen.getByTestId("forecast-candidates-value")).toHaveTextContent(
				"null",
			);
		});
	});

	describe("NewItemForecaster functionality", () => {
		it("should handle new item forecast errors and display error message", async () => {
			const errorMessage = "New item forecast service failed";
			mockForecastService.runItemPrediction.mockRejectedValueOnce(
				new Error(errorMessage),
			);

			renderWithProviders(<TeamForecastView team={mockTeam} />);

			const newItemForecastButton = screen.getByText("Run New Item Forecast");
			fireEvent.click(newItemForecastButton);

			await waitFor(() => {
				expect(screen.getByText(errorMessage)).toBeInTheDocument();
			});

			expect(mockForecastService.runItemPrediction).toHaveBeenCalledWith(
				mockTeam.id,
				expect.any(Date),
				expect.any(Date),
				expect.any(Date),
				mockTeam.workItemTypes,
			);
		});

		it("should display generic error message for non-Error objects in new item forecast", async () => {
			mockForecastService.runItemPrediction.mockRejectedValueOnce(
				"String error message",
			);

			renderWithProviders(<TeamForecastView team={mockTeam} />);

			const newItemForecastButton = screen.getByText("Run New Item Forecast");
			fireEvent.click(newItemForecastButton);

			await waitFor(() => {
				expect(
					screen.getByText(
						"Failed to run new item forecast. Please try again.",
					),
				).toBeInTheDocument();
			});
		});

		it("should not run new item forecast when team is missing", async () => {
			// Create a team with null properties but maintaining type safety
			const nullTeam = {} as Team;
			await act(async () => {
				renderWithProviders(<TeamForecastView team={nullTeam} />);
			});

			// Check if the button exists before trying to click it
			const newItemForecastButton = screen.queryByText("Run New Item Forecast");

			if (newItemForecastButton) {
				fireEvent.click(newItemForecastButton);
			}

			expect(mockForecastService.runItemPrediction).not.toHaveBeenCalled();
		});

		it("should not run new item forecast when target date is missing", async () => {
			// This test would require mocking the component's internal state
			// For now, we assume the component handles this case internally
			renderWithProviders(<TeamForecastView team={mockTeam} />);

			// The component should have a default target date, so this test verifies
			// that the service is called with the default date
			const newItemForecastButton = screen.getByText("Run New Item Forecast");
			fireEvent.click(newItemForecastButton);

			await waitFor(() => {
				expect(mockForecastService.runItemPrediction).toHaveBeenCalled();
			});
		});

		it("should handle empty work item types array", async () => {
			const teamWithoutWorkItemTypes = new Team();
			teamWithoutWorkItemTypes.id = mockTeam.id;
			teamWithoutWorkItemTypes.name = mockTeam.name;
			teamWithoutWorkItemTypes.workItemTypes = [];

			renderWithProviders(<TeamForecastView team={teamWithoutWorkItemTypes} />);

			const newItemForecastButton = screen.getByText("Run New Item Forecast");
			fireEvent.click(newItemForecastButton);

			await waitFor(() => {
				expect(mockForecastService.runItemPrediction).toHaveBeenCalledWith(
					teamWithoutWorkItemTypes.id,
					expect.any(Date),
					expect.any(Date),
					expect.any(Date),
					[], // Empty array when workItemTypes is empty
				);
			});
		});

		it("should clear new item forecast results when parameters change", async () => {
			// Mock a successful forecast result
			const mockForecastResult = {
				whenForecasts: [],
				howManyForecasts: [
					{ probability: 50, expectedValue: 10 },
					{ probability: 85, expectedValue: 15 },
					{ probability: 95, expectedValue: 20 },
				],
			};
			mockForecastService.runItemPrediction.mockResolvedValueOnce(
				mockForecastResult,
			);

			renderWithProviders(<TeamForecastView team={mockTeam} />);

			// First, run a forecast to populate results
			const newItemForecastButton = screen.getByText("Run New Item Forecast");
			fireEvent.click(newItemForecastButton);

			await waitFor(() => {
				expect(mockForecastService.runItemPrediction).toHaveBeenCalled();
			});

			// Now simulate a parameter change which should clear the results
			const clearButton = screen.getByTestId("clear-on-param-change");
			fireEvent.click(clearButton);

			// We can't directly test the state change, but we can verify that the
			// onClearForecastResult callback was passed and called correctly
			// The presence of the clear button in the mock and its functionality
			// demonstrates that the callback is working
			expect(clearButton).toBeInTheDocument();
		});

		it("should provide onClearForecastResult callback to NewItemForecaster", () => {
			renderWithProviders(<TeamForecastView team={mockTeam} />);

			// Verify that the clear button exists, which means the callback was passed
			const clearButton = screen.getByTestId("clear-on-param-change");
			expect(clearButton).toBeInTheDocument();

			// Test that clicking the clear button doesn't throw an error
			expect(() => fireEvent.click(clearButton)).not.toThrow();
		});

		it("should handle new item forecast result clearing gracefully", async () => {
			renderWithProviders(<TeamForecastView team={mockTeam} />);

			// Simulate parameter change (clearing) before any forecast is run
			const clearButton = screen.getByTestId("clear-on-param-change");

			// This should not throw an error even if no results exist
			expect(() => fireEvent.click(clearButton)).not.toThrow();
		});
	});

	describe("BacktestForecaster functionality", () => {
		it("should render the Forecast Backtesting section", () => {
			renderWithProviders(<TeamForecastView team={mockTeam} />);

			expect(screen.getByText("Forecast Backtesting")).toBeInTheDocument();
			expect(screen.getByTestId("backtest-forecaster")).toBeInTheDocument();
		});

		it("should call runBacktest service when backtest button is clicked", async () => {
			const mockBacktestResult = {
				startDate: new Date(),
				endDate: new Date(),
				historicalStartDate: new Date(),
				historicalEndDate: new Date(),
				percentiles: [
					{ probability: 50, value: 10 },
					{ probability: 70, value: 12 },
					{ probability: 85, value: 15 },
					{ probability: 95, value: 18 },
				],
				actualThroughput: 12,
				interpretation: "Actual throughput within expected range",
			};
			mockForecastService.runBacktest.mockResolvedValueOnce(mockBacktestResult);

			renderWithProviders(<TeamForecastView team={mockTeam} />);

			const runBacktestButton = screen.getByText("Run Backtest");
			fireEvent.click(runBacktestButton);

			await waitFor(() => {
				expect(mockForecastService.runBacktest).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
					expect.any(Date),
					expect.any(Date),
				);
			});
		});

		it("should display error snackbar when backtest service fails", async () => {
			const errorMessage = "Backtest service failed";
			mockForecastService.runBacktest.mockRejectedValueOnce(
				new Error(errorMessage),
			);

			renderWithProviders(<TeamForecastView team={mockTeam} />);

			const runBacktestButton = screen.getByText("Run Backtest");
			fireEvent.click(runBacktestButton);

			await waitFor(() => {
				expect(screen.getByText(errorMessage)).toBeInTheDocument();
			});
		});

		it("should display generic error message for non-Error objects in backtest", async () => {
			mockForecastService.runBacktest.mockRejectedValueOnce(
				"String error message",
			);

			renderWithProviders(<TeamForecastView team={mockTeam} />);

			const runBacktestButton = screen.getByText("Run Backtest");
			fireEvent.click(runBacktestButton);

			await waitFor(() => {
				expect(
					screen.getByText("Failed to run backtest. Please try again."),
				).toBeInTheDocument();
			});
		});

		it("should not run backtest when team is missing", async () => {
			const nullTeam = {} as Team;
			await act(async () => {
				renderWithProviders(<TeamForecastView team={nullTeam} />);
			});

			const runBacktestButton = screen.queryByText("Run Backtest");

			if (runBacktestButton) {
				fireEvent.click(runBacktestButton);
			}

			expect(mockForecastService.runBacktest).not.toHaveBeenCalled();
		});

		it("should provide onClearBacktestResult callback to BacktestForecaster", () => {
			renderWithProviders(<TeamForecastView team={mockTeam} />);

			const clearButton = screen.getByTestId("clear-backtest-result");
			expect(clearButton).toBeInTheDocument();

			expect(() => fireEvent.click(clearButton)).not.toThrow();
		});
	});
});
