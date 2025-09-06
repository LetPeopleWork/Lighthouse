import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { Team } from "../../../models/Team/Team";
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
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
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

describe("TeamForecastView component", () => {
	const mockTeam: Team = {
		id: 1,
		name: "Test Team",
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
		expect(screen.getByTestId("team-feature-list")).toBeInTheDocument();
		expect(screen.getByTestId("manual-forecaster")).toBeInTheDocument();
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
});
