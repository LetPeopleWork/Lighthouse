import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import GeneralInputsComponent from "./GeneralInputs";

describe("GeneralInputsComponent", () => {
	const initialSettings: IProjectSettings = {
		id: 1,
		name: "Project Name",
		workItemTypes: [],
		milestones: [],
		workItemQuery: "Initial Query",
		unparentedItemsQuery: "Unparented Query",
		usePercentileToCalculateDefaultAmountOfWorkItems: false,
		defaultAmountOfWorkItemsPerFeature: 10,
		defaultWorkItemPercentile: 85,
		historicalFeaturesWorkItemQuery: "",
		workTrackingSystemConnectionId: 12,
		sizeEstimateField: "",
		tags: [],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
		overrideRealChildCountStates: [""],
		involvedTeams: [],
	};

	const mockOnProjectSettingsChange = vi.fn();

	it("renders correctly with initial settings", () => {
		render(
			<GeneralInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(screen.getByLabelText(/Name/i)).toHaveValue("Project Name");
		expect(screen.getByLabelText(/Work Item Query/i)).toHaveValue(
			"Initial Query",
		);
	});

	it("calls onProjectSettingsChange with correct arguments when name changes", () => {
		render(
			<GeneralInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Name/i), {
			target: { value: "New Project Name" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"name",
			"New Project Name",
		);
	});

	it("calls onProjectSettingsChange with correct arguments when workItemQuery changes", () => {
		render(
			<GeneralInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Work Item Query/i), {
			target: { value: "Updated Query" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"workItemQuery",
			"Updated Query",
		);
	});

	it("handles null projectSettings correctly", () => {
		render(
			<GeneralInputsComponent
				projectSettings={null}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(screen.getByLabelText(/Name/i)).toHaveValue("");
		expect(screen.getByLabelText(/Work Item Query/i)).toHaveValue("");

		fireEvent.change(screen.getByLabelText(/Name/i), {
			target: { value: "New Name" },
		});
		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"name",
			"New Name",
		);

		fireEvent.change(screen.getByLabelText(/Work Item Query/i), {
			target: { value: "New Query" },
		});
		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"workItemQuery",
			"New Query",
		);
	});
});
