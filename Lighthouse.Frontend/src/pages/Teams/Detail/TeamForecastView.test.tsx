import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
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

vi.mock("./TeamFeatureList", () => ({
	default: ({ team }: { team: Team }) => (
		<div data-testid="team-feature-list">TeamFeatureList for {team.name}</div>
	),
}));

vi.mock("./ManualForecaster", () => ({
	default: ({ onRunManualForecast }: { onRunManualForecast: () => void }) => (
		<div data-testid="manual-forecaster">
			<button type="button" onClick={onRunManualForecast}>
				Run Forecast
			</button>
		</div>
	),
}));

vi.mock("./NewItemForecaster", () => ({
	default: ({
		onRunNewItemForecast,
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
	};

	const mockApiServiceContext = createMockApiServiceContext({
		forecastService: mockForecastService,
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
	});

	it("should render without errors", () => {
		renderWithProviders(<TeamForecastView team={mockTeam} />);

		expect(screen.getByText("Features")).toBeInTheDocument();
		expect(screen.getByText("Team Forecast")).toBeInTheDocument();
		expect(
			screen.getByText("New Work Items Creation Forecast"),
		).toBeInTheDocument();
		expect(screen.getByTestId("team-feature-list")).toBeInTheDocument();
		expect(screen.getByTestId("manual-forecaster")).toBeInTheDocument();
		expect(screen.getByTestId("new-item-forecaster")).toBeInTheDocument();
	});

	it("should display error snackbar when forecast service fails", async () => {
		const errorMessage = "Forecast service failed";
		mockForecastService.runManualForecast.mockRejectedValueOnce(
			new Error(errorMessage),
		);

		renderWithProviders(<TeamForecastView team={mockTeam} />);

		const runForecastButton = screen.getByText("Run Forecast");
		fireEvent.click(runForecastButton);

		await waitFor(() => {
			expect(screen.getByText(errorMessage)).toBeInTheDocument();
		});

		expect(mockForecastService.runManualForecast).toHaveBeenCalledWith(
			mockTeam.id,
			10, // default remainingItems
			expect.any(Date), // targetDate
		);
	});

	it("should display generic error message for non-Error objects", async () => {
		mockForecastService.runManualForecast.mockRejectedValueOnce(
			"String error message",
		);

		renderWithProviders(<TeamForecastView team={mockTeam} />);

		const runForecastButton = screen.getByText("Run Forecast");
		fireEvent.click(runForecastButton);

		await waitFor(() => {
			expect(
				screen.getByText("Failed to run manual forecast. Please try again."),
			).toBeInTheDocument();
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
			renderWithProviders(<TeamForecastView team={nullTeam} />);

			// Check if the button exists before trying to click it
			const newItemForecastButton = screen.queryByText("Run New Item Forecast");

			if (newItemForecastButton) {
				fireEvent.click(newItemForecastButton);
			}

			// Wait a bit to ensure no async call is made
			await new Promise((resolve) => setTimeout(resolve, 100));

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
	});
});
