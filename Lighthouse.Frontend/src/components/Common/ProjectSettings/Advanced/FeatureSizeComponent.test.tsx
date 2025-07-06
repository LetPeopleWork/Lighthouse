import { act, fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../../models/Project/ProjectSettings";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockSuggestionService,
} from "../../../../tests/MockApiServiceProvider";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
import FeatureSizeComponent from "./FeatureSizeComponent";

describe("FeatureSizeComponent", () => {
	const initialSettings = createMockProjectSettings();
	initialSettings.usePercentileToCalculateDefaultAmountOfWorkItems = false;
	initialSettings.defaultAmountOfWorkItemsPerFeature = 10;
	initialSettings.defaultWorkItemPercentile = 85;
	initialSettings.historicalFeaturesWorkItemQuery = "Historical Feature Query";
	initialSettings.sizeEstimateField = "";

	const mockOnProjectSettingsChange = vi.fn();

	// Mock suggestion service with state data
	const mockSuggestionService = createMockSuggestionService();
	const mockProjectStates = {
		toDoStates: ["New", "Ready", "Backlog"],
		doingStates: ["Active", "In Progress", "In Review"],
		doneStates: ["Done", "Closed", "Completed"],
	};

	const mockGetStatesForProjects = vi.fn().mockResolvedValue(mockProjectStates);
	mockSuggestionService.getStatesForProjects = mockGetStatesForProjects;

	const mockApiContext = createMockApiServiceContext({
		suggestionService: mockSuggestionService,
	});

	const renderWithContext = () => {
		return render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<FeatureSizeComponent
					projectSettings={initialSettings}
					onProjectSettingsChange={mockOnProjectSettingsChange}
				/>
			</ApiServiceContext.Provider>,
		);
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly with initial settings", () => {
		render(
			<FeatureSizeComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(
			screen.getByLabelText(/Default Number of Items per Feature/i),
		).toHaveValue(10);
		expect(screen.getByLabelText(/Size Estimate Field/i)).toHaveValue("");
	});

	it("calls onProjectSettingsChange with correct arguments when defaultAmountOfWorkItemsPerFeature changes", () => {
		render(
			<FeatureSizeComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(
			screen.getByLabelText(/Default Number of Items per Feature/i),
			{ target: { value: "20" } },
		);

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"defaultAmountOfWorkItemsPerFeature",
			20,
		);
	});

	it("calls onProjectSettingsChange with correct arguments when sizeEstimateField changes", () => {
		render(
			<FeatureSizeComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Size Estimate Field/i), {
			target: { value: "customfield_133742" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"sizeEstimateField",
			"customfield_133742",
		);
	});

	it("toggles the usePercentileToCalculateDefaultAmountOfWorkItems switch", () => {
		render(
			<FeatureSizeComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		const toggleSwitch = screen.getByLabelText(
			/Use Historical Feature Size To Calculate Default/i,
		);

		expect(toggleSwitch).not.toBeChecked();
		fireEvent.click(toggleSwitch);
		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"usePercentileToCalculateDefaultAmountOfWorkItems",
			true,
		);
	});

	it("renders Feature Size Percentile and Historical Features Work Item Query when switch is on", () => {
		const updatedSettings: IProjectSettings = {
			...initialSettings,
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
		};

		render(
			<FeatureSizeComponent
				projectSettings={updatedSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(
			screen.getByLabelText(/Feature Size Percentile/i),
		).toBeInTheDocument();
		expect(
			screen.getByLabelText(/Historical Features Work Item Query/i),
		).toBeInTheDocument();
	});

	it("calls onProjectSettingsChange with correct arguments when defaultWorkItemPercentile changes", () => {
		const updatedSettings: IProjectSettings = {
			...initialSettings,
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
		};

		render(
			<FeatureSizeComponent
				projectSettings={updatedSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Feature Size Percentile/i), {
			target: { value: "90" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"defaultWorkItemPercentile",
			90,
		);
	});

	it("fetches project states for suggestions when component mounts", async () => {
		await act(async () => {
			renderWithContext();
		});

		expect(mockSuggestionService.getStatesForProjects).toHaveBeenCalledTimes(1);
	});

	it("adds new override state when entered", async () => {
		await act(async () => {
			renderWithContext();
		});

		const input = screen.getByLabelText("New Size Override State");
		await act(async () => {
			fireEvent.change(input, { target: { value: "In Progress" } });
			fireEvent.keyDown(input, { key: "Enter" });
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"overrideRealChildCountStates",
			["", "In Progress"],
		);
	});

	it("removes an override state when delete is clicked", async () => {
		const updatedSettings: IProjectSettings = {
			...initialSettings,
			overrideRealChildCountStates: ["Ready", "In Progress"],
		};

		await act(async () => {
			render(
				<ApiServiceContext.Provider value={mockApiContext}>
					<FeatureSizeComponent
						projectSettings={updatedSettings}
						onProjectSettingsChange={mockOnProjectSettingsChange}
					/>
				</ApiServiceContext.Provider>,
			);
		});

		const chipElement = screen.getByText("Ready").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			await act(async () => {
				fireEvent.click(deleteIcon);
			});
		}

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"overrideRealChildCountStates",
			["In Progress"],
		);
	});

	it("displays error in console when fetching suggestions fails", async () => {
		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
		const mockError = new Error("Failed to fetch");
		mockGetStatesForProjects.mockRejectedValueOnce(mockError);

		await act(async () => {
			renderWithContext();
		});

		expect(consoleSpy).toHaveBeenCalledWith(
			"Failed to fetch states:",
			mockError,
		);

		consoleSpy.mockRestore();
	});
});
