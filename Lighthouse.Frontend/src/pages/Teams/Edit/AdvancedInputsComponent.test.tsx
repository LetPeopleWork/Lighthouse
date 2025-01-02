import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import AdvancedInputsComponent from "./AdvancedInputs";

describe("AdvancedInputsComponent", () => {
	it("renders correctly with provided teamSettings", () => {
		const teamSettings: ITeamSettings = {
			id: 0,
			name: "setting",
			throughputHistory: 2,
			featureWIP: 20,
			workItemQuery: "",
			workItemTypes: [],
			workTrackingSystemConnectionId: 12,
			relationCustomField: "Test Field",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
		};

		render(
			<AdvancedInputsComponent
				teamSettings={teamSettings}
				onTeamSettingsChange={vi.fn()}
			/>,
		);

		// Check if the TextField displays the correct value
		expect(screen.getByLabelText("Relation Custom Field")).toHaveValue(
			"Test Field",
		);
		expect(screen.getByLabelText("Feature WIP")).toHaveValue(20);
	});

	it("calls onTeamSettingsChange with the correct parameters when the TextField value changes", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings: ITeamSettings = {
			id: 0,
			name: "setting",
			throughputHistory: 2,
			featureWIP: 2,
			workItemQuery: "",
			workItemTypes: [],
			workTrackingSystemConnectionId: 12,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
		};

		render(
			<AdvancedInputsComponent
				teamSettings={teamSettings}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		// Simulate typing in the TextField
		fireEvent.change(screen.getByLabelText("Relation Custom Field"), {
			target: { value: "New Value" },
		});

		// Check if the callback was called with the correct parameters
		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"relationCustomField",
			"New Value",
		);
	});

	it("calls onTeamSettingsChange with correct parameters when Feature WIP TextField value changes", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings: ITeamSettings = {
			id: 0,
			name: "setting",
			throughputHistory: 2,
			featureWIP: 2,
			workItemQuery: "",
			workItemTypes: [],
			workTrackingSystemConnectionId: 12,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
		};

		render(
			<AdvancedInputsComponent
				teamSettings={teamSettings}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Feature WIP"), {
			target: { value: "25" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith("featureWIP", 25);
	});

	it("calls onTeamSettingsChange with correct parameters when automaticallyAdjustFeatureWIP checkbox is toggled", () => {
		const onTeamSettingsChange = vi.fn();
		const teamSettings: ITeamSettings = {
			id: 0,
			name: "setting",
			throughputHistory: 2,
			featureWIP: 2,
			workItemQuery: "",
			workItemTypes: [],
			workTrackingSystemConnectionId: 12,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
		};

		render(
			<AdvancedInputsComponent
				teamSettings={teamSettings}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.click(
			screen.getByLabelText(
				"Automatically Adjust Feature WIP based on actual WIP",
			),
		);

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"automaticallyAdjustFeatureWIP",
			true,
		);
	});
});
