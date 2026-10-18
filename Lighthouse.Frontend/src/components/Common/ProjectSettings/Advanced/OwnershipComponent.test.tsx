import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../../models/EntityReference";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../../models/Team/Team";
import type { IAdditionalFieldDefinition } from "../../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";
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
			portfolios: [],
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
			hasThroughputBlackoutOverlap: false,
			hasForecastFilter: false,
			refinementConfigured: false,
		},
		{
			id: 2,
			name: "Team B",
			portfolios: [],
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
			hasThroughputBlackoutOverlap: false,
			hasForecastFilter: false,
			refinementConfigured: false,
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
				availableTeams={mockTeams}
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
				availableTeams={mockTeams}
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
				availableTeams={mockTeams}
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
				availableTeams={mockTeams}
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

	describe.each([
		{ tracker: "Jira", says: "Jira" },
		{ tracker: "Csv", says: "CSV" },
	] as const satisfies readonly {
		tracker: WorkTrackingSystemType;
		says: string;
	}[])(
		"on $tracker, whose connection holds the Feature Owner Field default",
		({ tracker, says }) => {
			const on = (settings: Partial<IPortfolioSettings>) => {
				render(
					<OwnershipComponent
						projectSettings={{
							...initialSettings,
							workTrackingSystem: tracker,
							...settings,
						}}
						onProjectSettingsChange={mockOnProjectSettingsChange}
						availableTeams={mockTeams}
						additionalFieldDefinitions={mockAdditionalFields}
					/>,
				);
				fireEvent.click(screen.getByLabelText("toggle"));
			};

			const theFeatureOwnerField = () =>
				screen.getByRole("combobox", { name: "Feature Owner Field" });

			it("a follower shows the connection default first, with where it is set", () => {
				on({
					featureOwnerSource: "ConnectionDefault",
					featureOwnerDefaultName: "Lead Developer",
				});

				expect(theFeatureOwnerField()).toHaveTextContent(
					/^Connection default \(Lead Developer\)$/,
				);
				expect(
					screen.getByText(`Set on the ${says} connection.`),
				).toBeInTheDocument();
				fireEvent.mouseDown(theFeatureOwnerField());
				const entries = within(screen.getByRole("listbox")).getAllByRole(
					"option",
				);
				expect(entries.map((entry) => entry.textContent)).toEqual([
					"Connection default (Lead Developer)",
					"None",
					"Feature Owner",
					"Lead Developer",
				]);
			});

			it("picking a field makes the Portfolio own the setting", () => {
				on({
					featureOwnerSource: "ConnectionDefault",
					featureOwnerDefaultName: "Lead Developer",
				});

				fireEvent.mouseDown(theFeatureOwnerField());
				fireEvent.click(
					within(screen.getByRole("listbox")).getByText("Feature Owner"),
				);

				expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
					"featureOwnerSource",
					"Own",
				);
				expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
					"featureOwnerAdditionalFieldDefinitionId",
					1,
				);
			});

			it("an owner picks the entry to follow the connection again", () => {
				on({
					featureOwnerSource: "Own",
					featureOwnerAdditionalFieldDefinitionId: 1,
					featureOwnerDefaultName: "Lead Developer",
				});

				fireEvent.mouseDown(theFeatureOwnerField());
				fireEvent.click(
					within(screen.getByRole("listbox")).getByText(
						"Connection default (Lead Developer)",
					),
				);

				expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
					"featureOwnerSource",
					"ConnectionDefault",
				);
				expect(mockOnProjectSettingsChange).not.toHaveBeenCalledWith(
					"featureOwnerAdditionalFieldDefinitionId",
					expect.anything(),
				);
			});

			it("the Owning Team offers no connection default", () => {
				on({ featureOwnerSource: "ConnectionDefault" });

				fireEvent.mouseDown(
					screen.getByRole("combobox", { name: "Owning Team" }),
				);

				expect(
					within(screen.getByRole("listbox")).queryByText(
						/^Connection default/,
					),
				).not.toBeInTheDocument();
			});
		},
	);

	it("offers no connection default on a tracker whose connection holds none", () => {
		render(
			<OwnershipComponent
				projectSettings={{
					...initialSettings,
					workTrackingSystem: "Linear",
					featureOwnerSource: "ConnectionDefault",
				}}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				availableTeams={mockTeams}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);
		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.mouseDown(
			screen.getByRole("combobox", { name: "Feature Owner Field" }),
		);

		expect(
			within(screen.getByRole("listbox")).queryByText(/^Connection default/),
		).not.toBeInTheDocument();
	});

	it("sets featureOwnerAdditionalFieldDefinitionId to null when 'None' is selected", () => {
		const settingsWithField: IPortfolioSettings = {
			...initialSettings,
			featureOwnerAdditionalFieldDefinitionId: 1,
		};

		render(
			<OwnershipComponent
				projectSettings={settingsWithField}
				onProjectSettingsChange={mockOnProjectSettingsChange}
				availableTeams={mockTeams}
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
		fireEvent.click(listbox.getByText("None"));

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"featureOwnerAdditionalFieldDefinitionId",
			null,
		);
	});
});
