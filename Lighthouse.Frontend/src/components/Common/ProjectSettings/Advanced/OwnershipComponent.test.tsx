import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../../models/EntityReference";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../../models/Team/Team";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
import OwnershipComponent from "./OwnershipComponent";

describe("OwnershipComponent", () => {
	const mockAdditionalFields: IAdditionalFieldDefinition[] = [
		{ id: 1, displayName: "Feature Owner", reference: "custom.owner" },
		{ id: 2, displayName: "Lead Developer", reference: "custom.lead" },
	];

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
			workItemTypes: ["User Story", "Bug"],
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
			workItemTypes: ["Task", "Feature"],
		},
	];

	const initialSettings = createMockProjectSettings();
	initialSettings.featureOwnerAdditionalFieldDefinitionId = 1;

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
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		// There should be two comboboxes: Owning Team and Feature Owner Field
		const comboboxes = screen.getAllByRole("combobox");
		expect(comboboxes).toHaveLength(2);
	});

	it("calls onProjectSettingsChange with correct arguments when featureOwnerField changes", () => {
		render(
			<OwnershipComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		// Get the Feature Owner Field dropdown (second combobox)
		const comboboxes = screen.getAllByRole("combobox");
		const featureOwnerSelect = comboboxes[1];
		fireEvent.mouseDown(featureOwnerSelect);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("Lead Developer"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"featureOwnerAdditionalFieldDefinitionId",
			2,
		);
	});

	it("calls onProjectSettingsChange with correct team when owning team changes", () => {
		render(
			<OwnershipComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.mouseDown(screen.getAllByRole("combobox")[0]);
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

		const settingsWithTeam: IPortfolioSettings = {
			...initialSettings,
			owningTeam: owningTeam,
		};

		render(
			<OwnershipComponent
				projectSettings={settingsWithTeam}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				currentInvolvedTeams={mockTeams}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		// Expand the collapsible section first
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.mouseDown(screen.getAllByRole("combobox")[0]);
		fireEvent.click(screen.getByText("None"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"owningTeam",
			null,
		);
	});
});
