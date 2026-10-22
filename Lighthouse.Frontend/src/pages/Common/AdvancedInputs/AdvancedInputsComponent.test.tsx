import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

describe("AdvancedInputsComponent", () => {
	const mockAdditionalFields: IAdditionalFieldDefinition[] = [
		{ id: 1, displayName: "Parent Link Field", reference: "System.ParentLink" },
		{ id: 2, displayName: "Custom Parent", reference: "custom.parent" },
	];

	it("renders correctly with provided teamSettings", () => {
		const teamSettings = createMockTeamSettings();
		teamSettings.parentOverrideAdditionalFieldDefinitionId = 1;
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

		expect(
			screen.getByRole("combobox", { name: "Parent Override Field" }),
		).toHaveTextContent("Parent Link Field");
		expect(screen.getByLabelText("Closed Items Cutoff (days)")).toHaveValue(
			180,
		);
	});

	it("calls onTeamSettingsChange with the correct parameters when the Select value changes", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings = createMockTeamSettings();

		render(
			<AdvancedInputsComponent
				settings={teamSettings}
				onSettingsChange={onTeamSettingsChange}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		const select = screen.getByRole("combobox");
		fireEvent.mouseDown(select);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("Parent Link Field"));

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"parentOverrideAdditionalFieldDefinitionId",
			1,
		);
	});

	it("calls onTeamSettingsChange with null when None is selected", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings = createMockTeamSettings();
		teamSettings.parentOverrideAdditionalFieldDefinitionId = 1;

		render(
			<AdvancedInputsComponent
				settings={teamSettings}
				onSettingsChange={onTeamSettingsChange}
				parentOverrideDefault="parentOverrideForTeams"
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		const select = screen.getByRole("combobox");
		fireEvent.mouseDown(select);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("None"));

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"parentOverrideAdditionalFieldDefinitionId",
			null,
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
		const teamSettings = createMockTeamSettings();

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
