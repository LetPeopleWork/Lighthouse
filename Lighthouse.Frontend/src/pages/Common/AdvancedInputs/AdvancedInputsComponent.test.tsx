import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

describe("AdvancedInputsComponent", () => {
	it("renders correctly with provided teamSettings", () => {
		const teamSettings = createMockTeamSettings();
		teamSettings.parentOverrideField = "Test Field";

		render(
			<AdvancedInputsComponent
				settings={teamSettings}
				onSettingsChange={vi.fn()}
			/>,
		);

		// Check if the TextField displays the correct value
		expect(screen.getByLabelText("Parent Override Field")).toHaveValue(
			"Test Field",
		);
	});

	it("calls onTeamSettingsChange with the correct parameters when the TextField value changes", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings = createMockTeamSettings();

		render(
			<AdvancedInputsComponent
				settings={teamSettings}
				onSettingsChange={onTeamSettingsChange}
			/>,
		);

		// Simulate typing in the TextField
		fireEvent.change(screen.getByLabelText("Parent Override Field"), {
			target: { value: "New Value" },
		});

		// Check if the callback was called with the correct parameters
		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"parentOverrideField",
			"New Value",
		);
	});
});
