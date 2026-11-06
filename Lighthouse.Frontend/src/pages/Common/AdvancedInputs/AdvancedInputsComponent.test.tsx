import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

const aTeamOnAzureDevOps = () => ({
	...createMockTeamSettings(),
	workTrackingSystem: "AzureDevOps" as const,
});

describe("AdvancedInputsComponent", () => {
	const mockAdditionalFields: IAdditionalFieldDefinition[] = [
		{ id: 1, displayName: "Parent Link Field", reference: "System.ParentLink" },
		{ id: 2, displayName: "Custom Parent", reference: "custom.parent" },
	];

	it("renders correctly with provided teamSettings", () => {
		const teamSettings = aTeamOnAzureDevOps();
		teamSettings.parentOverrideSource = "Own";
		teamSettings.parentSources = [
			{ kind: "AdditionalField", additionalFieldDefinitionId: 1 },
		];
		teamSettings.doneItemsCutoffDays = 180;

		render(
			<AdvancedInputsComponent
				settings={teamSettings}
				onSettingsChange={vi.fn()}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		expect(screen.getAllByRole("listitem")).toHaveLength(1);
		expect(screen.getByText("Parent Link Field")).toBeInTheDocument();
		expect(screen.getByLabelText("Closed Items Cutoff (days)")).toHaveValue(
			180,
		);
	});

	// This form is the one a team and a portfolio share, so it is where a dependency setting would leak
	// onto a team's settings page. A dependency runs between two Features and Features are fetched per
	// portfolio, so a team-level copy of either setting would have nothing to act on.
	it("offers no dependency settings, because this form is the one a team also sees", () => {
		render(
			<AdvancedInputsComponent
				settings={createMockTeamSettings()}
				onSettingsChange={vi.fn()}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		expect(screen.queryByText("Dependency Field")).not.toBeInTheDocument();
		expect(
			screen.queryByLabelText("Ignore Dependencies"),
		).not.toBeInTheDocument();
	});

	it("calls onTeamSettingsChange with numeric value when cutoff days changes", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings = aTeamOnAzureDevOps();

		render(
			<AdvancedInputsComponent
				settings={teamSettings}
				onSettingsChange={onTeamSettingsChange}
				parentOverrideDefault="parentOverrideForTeams"
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		fireEvent.change(screen.getByLabelText("Closed Items Cutoff (days)"), {
			target: { value: "365" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"doneItemsCutoffDays",
			365,
		);
	});
});
