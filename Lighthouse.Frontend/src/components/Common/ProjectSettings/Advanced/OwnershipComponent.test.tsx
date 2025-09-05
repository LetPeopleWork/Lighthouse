import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../../models/EntityReference";
import type { IProjectSettings } from "../../../../models/Project/ProjectSettings";
import type { ITeam } from "../../../../models/Team/Team";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
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
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
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
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
		},
	];

	const initialSettings = createMockProjectSettings();
	initialSettings.featureOwnerField = "custom.owner";

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
		const owningTeam: IEntityReference = {
			id: mockTeams[0].id,
			name: mockTeams[0].name,
		};

		const settingsWithTeam: IProjectSettings = {
			...initialSettings,
			owningTeam: owningTeam,
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
