import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import GeneralSettingsComponent from "./GeneralSettingsComponent";

describe("GeneralSettingsComponent", () => {
	const mockOnSettingsChange = vi.fn();

	const testSettings = createMockTeamSettings();
	testSettings.name = "Test Settings";
	testSettings.workItemQuery = "Test Query";

	beforeEach(() => {
		mockOnSettingsChange.mockClear();
	});

	it("renders correctly with provided settings", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(screen.getByLabelText("Name")).toHaveValue("Test Settings");
		expect(screen.getByLabelText("Work Item Query")).toHaveValue("Test Query");
	});

	it("calls onSettingsChange with correct arguments when name changes", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Name"), {
			target: { value: "Updated Settings" },
		});

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"name",
			"Updated Settings",
		);
	});

	it("calls onSettingsChange with correct arguments when work item query changes", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Work Item Query"), {
			target: { value: "Updated Query" },
		});

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"workItemQuery",
			"Updated Query",
		);
	});

	it("renders with custom title when provided", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
				title="Custom Title"
			/>,
		);

		expect(screen.getByText("Custom Title")).toBeInTheDocument();
	});

	it("handles null settings gracefully", () => {
		render(
			<GeneralSettingsComponent
				settings={null}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(screen.getByLabelText("Name")).toHaveValue("");
		expect(screen.getByLabelText("Work Item Query")).toHaveValue("");
	});
});
