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
import type {
	IFeatureCandidate,
	IForecastInputCandidates,
} from "../../../models/Forecasts/ForecastInputCandidates";
import type { Team } from "../../../models/Team/Team";
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
		mode,
		selectedFeatures,
		onModeChange,
		onFeatureSelectionChange,
	}: {
		remainingItems: number | null;
		targetDate: unknown;
		forecastInputCandidates: IForecastInputCandidates | null;
		manualForecastResult: unknown;
		onRemainingItemsChange: (value: number | null) => void;
		onTargetDateChange: (date: unknown) => void;
		mode: string;
		selectedFeatures: IFeatureCandidate[];
		onModeChange: (mode: string) => void;
		onFeatureSelectionChange: (features: IFeatureCandidate[]) => void;
	}) => (
		<div data-testid="manual-forecaster">
			<span data-testid="remaining-items-value">
				{remainingItems ?? "null"}
			</span>
			<span data-testid="target-date-value">
				{targetDate ? "has-date" : "null"}
			</span>
			<span data-testid="forecast-candidates-value">
				{forecastInputCandidates ? "has-candidates" : "null"}
			</span>
			<span data-testid="forecast-mode-value">{mode}</span>
			<span data-testid="selected-features-count">
				{selectedFeatures?.length ?? 0}
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
				data-testid="simulate-null-remaining"
				onClick={() => onRemainingItemsChange(null)}
			>
				Clear Remaining Items
			</button>
			<button
				type="button"
				data-testid="simulate-date-change"
				onClick={() => onTargetDateChange(dayjs().add(1, "week"))}
			>
				Change Target Date
			</button>
			<button
				type="button"
				data-testid="simulate-switch-to-features"
				onClick={() => onModeChange("features")}
			>
				Switch to Features
			</button>
			<button
				type="button"
				data-testid="simulate-switch-to-manual"
				onClick={() => onModeChange("manual")}
			>
				Switch to Manual
			</button>
			<button
				type="button"
				data-testid="simulate-feature-selection"
				onClick={() =>
					onFeatureSelectionChange([
						{ id: 1, name: "Feature Alpha", remainingWork: 5 },
					])
				}
			>
				Select Feature
			</button>
			<button
				type="button"
				data-testid="simulate-feature-selection-multi"
				onClick={() =>
					onFeatureSelectionChange([
						{ id: 1, name: "Feature Alpha", remainingWork: 5 },
						{ id: 2, name: "Feature Beta", remainingWork: 8 },
					])
				}
			>
				Select Two Features
			</button>
			<button
				type="button"
				data-testid="simulate-zero-feature-selection"
				onClick={() => onFeatureSelectionChange([])}
			>
				Clear Feature Selection
			</button>
		</div>
	),
}));

vi.mock("./NewItemForecaster", () => ({
	default: ({
		onInputChange,
	}: {
		onInputChange: (complete: boolean) => void;
	}) => (
		<div data-testid="new-item-forecaster">
			<button
				type="button"
				onClick={() => onInputChange(true)}
				data-testid="new-item-valid-change"
			>
				Valid new item input change
			</button>
			<button
				type="button"
				onClick={() => onInputChange(false)}
				data-testid="new-item-incomplete-change"
			>
				Incomplete new item input change
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
		it("should initialize with remainingItems=null and targetDate=null", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			expect(screen.getByTestId("remaining-items-value")).toHaveTextContent(
				"null",
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
					undefined,
				);
			} finally {
				vi.useRealTimers();
			}
		});

		it("should run forecast after user changes target date (debounced)", async () => {
			mockForecastService.runManualForecast.mockResolvedValueOnce({
				remainingItems: 0,
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
					undefined,
					expect.any(Object),
					undefined,
				);
			} finally {
				vi.useRealTimers();
			}
		});

		it("should NOT run forecast when remainingItems is cleared and targetDate is not set", async () => {
			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-null-remaining"));
				});

				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).not.toHaveBeenCalled();
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
		it("should surface an error message when the auto-run new item forecast fails", async () => {
			const errorMessage = "New item forecast service failed";
			mockForecastService.runItemPrediction.mockRejectedValueOnce(
				new Error(errorMessage),
			);

			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("new-item-valid-change"));
				});
				await act(async () => {
					await vi.advanceTimersByTimeAsync(300);
				});

				expect(mockForecastService.runItemPrediction).toHaveBeenCalledTimes(1);
				expect(screen.getByText(errorMessage)).toBeInTheDocument();
			} finally {
				vi.useRealTimers();
			}
		});

		it("should not run the new item forecast while inputs are incomplete", async () => {
			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("new-item-incomplete-change"));
				});
				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runItemPrediction).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
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
					undefined,
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

	describe("Feature mode orchestration", () => {
		it("should default to Manual mode and pass mode to ManualForecaster", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});
			expect(screen.getByTestId("forecast-mode-value")).toHaveTextContent(
				"manual",
			);
		});

		it("should switch to Features mode when onModeChange called with features", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});
			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-switch-to-features"));
			});
			expect(screen.getByTestId("forecast-mode-value")).toHaveTextContent(
				"features",
			);
		});

		it("should run debounced forecast with feature aggregate when features selected and aggregate > 0", async () => {
			mockForecastService.runManualForecast.mockResolvedValueOnce({
				remainingItems: 5,
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

				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-switch-to-features"));
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-feature-selection"));
				});

				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).toHaveBeenCalledWith(
					mockTeam.id,
					5,
					null,
					undefined,
				);
			} finally {
				vi.useRealTimers();
			}
		});

		it("should NOT run forecast when feature selection results in aggregate of 0", async () => {
			vi.useFakeTimers();
			try {
				await act(async () => {
					renderWithProviders(<TeamForecastView team={mockTeam} />);
				});

				await act(async () => {
					fireEvent.click(screen.getByTestId("simulate-switch-to-features"));
				});

				await act(async () => {
					fireEvent.click(
						screen.getByTestId("simulate-zero-feature-selection"),
					);
				});

				act(() => {
					vi.advanceTimersByTime(300);
				});

				expect(mockForecastService.runManualForecast).not.toHaveBeenCalled();
			} finally {
				vi.useRealTimers();
			}
		});

		it("should pass selectedFeatures to ManualForecaster", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});
			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-switch-to-features"));
			});
			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-feature-selection"));
			});
			expect(screen.getByTestId("selected-features-count")).toHaveTextContent(
				"1",
			);
		});

		it("should not leak feature selections into manual mode remaining-items state", async () => {
			await act(async () => {
				renderWithProviders(<TeamForecastView team={mockTeam} />);
			});

			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-switch-to-features"));
			});

			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-feature-selection-multi"));
			});

			await act(async () => {
				fireEvent.click(screen.getByTestId("simulate-switch-to-manual"));
			});

			// Manual remaining-items state must remain null (initial), not 13 (feature aggregate)
			expect(screen.getByTestId("remaining-items-value")).toHaveTextContent(
				"null",
			);
		});
	});
});
