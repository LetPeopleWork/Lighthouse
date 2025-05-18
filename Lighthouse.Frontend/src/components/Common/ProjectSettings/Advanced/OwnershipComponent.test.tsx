import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../../models/Project/ProjectSettings";
import { type ITeam, Team } from "../../../../models/Team/Team";
import OwnershipComponent from "./OwnershipComponent";

describe("OwnershipComponent", () => {
	const mockTeams: ITeam[] = [
		{
			id: 1,
			name: "Team A",
			projects: [],
			featureWip: 0,
			lastUpdated: new Date(),
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(),
			throughputEndDate: new Date(),
			features: [],
			remainingFeatures: 0,
			tags: [],
			remainingWork: 0,
			totalWork: 0,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
		},
		{
			id: 2,
			name: "Team B",
			projects: [],
			featureWip: 0,
			lastUpdated: new Date(),
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(),
			throughputEndDate: new Date(),
			features: [],
			remainingFeatures: 0,
			tags: [],
			remainingWork: 0,
			totalWork: 0,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
		},
	];

	const initialSettings: IProjectSettings = {
		id: 1,
		name: "Settings",
		workItemTypes: [],
		milestones: [],
		workItemQuery: "Initial Query",
		unparentedItemsQuery: "Unparented Query",
		usePercentileToCalculateDefaultAmountOfWorkItems: false,
		defaultAmountOfWorkItemsPerFeature: 10,
		defaultWorkItemPercentile: 85,
		historicalFeaturesWorkItemQuery: "Historical Feature Query",
		workTrackingSystemConnectionId: 12,
		sizeEstimateField: "",
		tags: [],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
		overrideRealChildCountStates: [""],
		involvedTeams: [],
		serviceLevelExpectationProbability: 85,
		serviceLevelExpectationRange: 30,
		featureOwnerField: "custom.owner",
		owningTeam: new Team(),
	};

	const mockOnProjectSettingsChange = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly with initial settings", () => {
		render(
			<OwnershipComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		expect(screen.getByLabelText(/Feature Owner Field/i)).toHaveValue(
			"custom.owner",
		);
		expect(screen.getByDisplayValue(/custom.owner/)).toBeInTheDocument();
	});

	it("calls onProjectSettingsChange with correct arguments when featureOwnerField changes", () => {
		render(
			<OwnershipComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.change(screen.getByLabelText(/Feature Owner Field/i), {
			target: { value: "custom.newowner" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"featureOwnerField",
			"custom.newowner",
		);
	});

	it("calls onProjectSettingsChange with correct team when owning team changes", () => {
		render(
			<OwnershipComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.mouseDown(screen.getByRole("combobox"));
		fireEvent.click(screen.getByText("Team A"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"owningTeam",
			mockTeams[0],
		);
	});

	it("sets owningTeam to null when 'None' is selected", () => {
		const settingsWithTeam: IProjectSettings = {
			...initialSettings,
			owningTeam: mockTeams[0],
		};

		render(
			<OwnershipComponent
				projectSettings={settingsWithTeam}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.mouseDown(screen.getByRole("combobox"));
		fireEvent.click(screen.getByText("None"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"owningTeam",
			null,
		);
	});
});
