import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import { WorkTrackingSystemConnection } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { actAndSettle } from "../../../../tests/actAndSettle";
import {
	createMockApiServiceContext,
	createMockSuggestionService,
} from "../../../../tests/MockApiServiceProvider";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
import FeatureSizeComponent from "./FeatureSizeComponent";

const openTheSizeEstimateFieldList = () => {
	fireEvent.mouseDown(screen.getByRole("combobox"));

	return within(screen.getByRole("listbox"));
};

describe("FeatureSizeComponent", () => {
	const mockAdditionalFields: IAdditionalFieldDefinition[] = [
		{ id: 1, displayName: "Size Estimate", reference: "custom.sizeEstimate" },
		{ id: 2, displayName: "Story Points", reference: "custom.storyPoints" },
	];

	const initialSettings = createMockProjectSettings();
	initialSettings.usePercentileToCalculateDefaultAmountOfWorkItems = false;
	initialSettings.defaultAmountOfWorkItemsPerFeature = 10;
	initialSettings.defaultWorkItemPercentile = 85;
	initialSettings.percentileHistoryInDays = 90;
	initialSettings.sizeEstimateAdditionalFieldDefinitionId = null;

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
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(
			screen.getByLabelText(/Default Number of Work Items per Feature/i),
		).toHaveValue(10);
		// Size Estimate Field is a combobox
		expect(screen.getByRole("combobox")).toBeInTheDocument();
	});

	// The page renders before the settings have arrived. Reaching into them would blank the form with a
	// crash rather than show it empty.
	it("renders before there are any settings to show", () => {
		render(
			<FeatureSizeComponent
				projectSettings={null}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(
			screen.getByRole("combobox", { name: "Size Estimate Field" }),
		).toBeInTheDocument();
	});

	it("calls onProjectSettingsChange with correct arguments when defaultAmountOfWorkItemsPerFeature changes", () => {
		render(
			<FeatureSizeComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.change(
			screen.getByLabelText(/Default Number of Work Items per Feature/i),
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
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		// Open the dropdown and select an option
		const select = screen.getByRole("combobox");
		fireEvent.mouseDown(select);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("Size Estimate"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"sizeEstimateAdditionalFieldDefinitionId",
			1,
		);
	});

	it("sets sizeEstimateAdditionalFieldDefinitionId to null when 'None' is selected", () => {
		const settingsWithField: IPortfolioSettings = {
			...initialSettings,
			sizeEstimateAdditionalFieldDefinitionId: 1,
		};

		render(
			<FeatureSizeComponent
				projectSettings={settingsWithField}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		// Open the dropdown and select None
		const select = screen.getByRole("combobox");
		fireEvent.mouseDown(select);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("None"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"sizeEstimateAdditionalFieldDefinitionId",
			null,
		);
	});

	describe("on a tracker whose connection holds the Size Estimate Field default", () => {
		const onJira = (settings: Partial<IPortfolioSettings>) => {
			render(
				<FeatureSizeComponent
					projectSettings={{
						...initialSettings,
						workTrackingSystem: "Jira",
						...settings,
					}}
					onProjectSettingsChange={mockOnProjectSettingsChange}
					additionalFieldDefinitions={mockAdditionalFields}
				/>,
			);
			fireEvent.click(screen.getByLabelText("toggle"));
		};

		it("a follower shows the connection default first, with where it is set", () => {
			onJira({
				sizeEstimateSource: "ConnectionDefault",
				sizeEstimateDefaultName: "Story Points",
			});

			expect(screen.getByRole("combobox")).toHaveTextContent(
				"Connection default (Story Points)",
			);
			expect(
				screen.getByText("Set on the Jira connection."),
			).toBeInTheDocument();
			const entries = openTheSizeEstimateFieldList().getAllByRole("option");
			expect(entries.map((entry) => entry.textContent)).toEqual([
				"Connection default (Story Points)",
				"None",
				"Size Estimate",
				"Story Points",
			]);
		});

		it("a follower whose default names nothing still sees the entry", () => {
			onJira({
				sizeEstimateSource: "ConnectionDefault",
				sizeEstimateDefaultName: null,
			});

			expect(screen.getByRole("combobox")).toHaveTextContent(
				"Connection default (None)",
			);
		});

		it("picking a field makes the Portfolio own the setting", () => {
			onJira({
				sizeEstimateSource: "ConnectionDefault",
				sizeEstimateDefaultName: "Story Points",
			});

			fireEvent.click(
				openTheSizeEstimateFieldList().getByText("Size Estimate"),
			);

			expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
				"sizeEstimateSource",
				"Own",
			);
			expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
				"sizeEstimateAdditionalFieldDefinitionId",
				1,
			);
		});

		it("an owner picks the entry to follow the connection again", () => {
			onJira({
				sizeEstimateSource: "Own",
				sizeEstimateAdditionalFieldDefinitionId: 1,
				sizeEstimateDefaultName: "Story Points",
			});

			fireEvent.click(
				openTheSizeEstimateFieldList().getByText(
					"Connection default (Story Points)",
				),
			);

			expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
				"sizeEstimateSource",
				"ConnectionDefault",
			);
			expect(mockOnProjectSettingsChange).not.toHaveBeenCalledWith(
				"sizeEstimateAdditionalFieldDefinitionId",
				expect.anything(),
			);
		});

		it("names the default of the connection chosen on the page, once another is chosen", () => {
			render(
				<FeatureSizeComponent
					projectSettings={{
						...initialSettings,
						workTrackingSystem: "Jira",
						workTrackingSystemConnectionId: 1,
						sizeEstimateSource: "ConnectionDefault",
						sizeEstimateDefaultName: "Story Points",
					}}
					onProjectSettingsChange={mockOnProjectSettingsChange}
					additionalFieldDefinitions={mockAdditionalFields}
					connection={
						new WorkTrackingSystemConnection({
							id: 2,
							name: "Azure DevOps Payments",
							workTrackingSystem: "AzureDevOps",
							options: [],
							additionalFieldDefinitions: mockAdditionalFields,
							defaults: { sizeEstimateFieldForPortfolios: 1 },
						})
					}
				/>,
			);
			fireEvent.click(screen.getByLabelText("toggle"));

			expect(screen.getByRole("combobox")).toHaveTextContent(
				"Connection default (Size Estimate)",
			);
			expect(
				screen.getByText("Set on the Azure DevOps connection."),
			).toBeInTheDocument();
		});
	});

	it("offers no connection default on a tracker whose connection holds none", () => {
		render(
			<FeatureSizeComponent
				projectSettings={{
					...initialSettings,
					workTrackingSystem: "Csv",
					sizeEstimateSource: "ConnectionDefault",
				}}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(
			openTheSizeEstimateFieldList().queryByText(/^Connection default/),
		).not.toBeInTheDocument();
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
		const updatedSettings: IPortfolioSettings = {
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
		expect(screen.getByLabelText(/History in Days/i)).toBeInTheDocument();
	});

	it.each([
		{ stored: 0, displayed: 90 },
		{ stored: 45, displayed: 45 },
	])(
		"shows a stored percentile history of $stored days as $displayed",
		({ stored, displayed }) => {
			render(
				<FeatureSizeComponent
					projectSettings={{
						...initialSettings,
						usePercentileToCalculateDefaultAmountOfWorkItems: true,
						percentileHistoryInDays: stored,
					}}
					onProjectSettingsChange={mockOnProjectSettingsChange}
				/>,
			);

			expect(screen.getByLabelText(/^History in Days/)).toHaveValue(displayed);
		},
	);

	it("calls onProjectSettingsChange with correct arguments when defaultWorkItemPercentile changes", () => {
		const updatedSettings: IPortfolioSettings = {
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
		await actAndSettle(() => {
			renderWithContext();
		});

		expect(mockSuggestionService.getStatesForProjects).toHaveBeenCalledTimes(1);
	});

	it("adds new override state when entered", async () => {
		await actAndSettle(() => {
			renderWithContext();
		});

		const input = screen.getByLabelText("New Size Override State");
		await actAndSettle(() => {
			fireEvent.change(input, { target: { value: "In Progress" } });
			fireEvent.keyDown(input, { key: "Enter" });
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"overrideRealChildCountStates",
			["", "In Progress"],
		);
	});

	it("removes an override state when delete is clicked", async () => {
		const updatedSettings: IPortfolioSettings = {
			...initialSettings,
			overrideRealChildCountStates: ["Ready", "In Progress"],
		};

		await actAndSettle(() => {
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
			await actAndSettle(() => {
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

		await actAndSettle(() => {
			renderWithContext();
		});

		expect(consoleSpy).toHaveBeenCalledWith(
			"Failed to fetch states:",
			mockError,
		);

		consoleSpy.mockRestore();
	});
});
