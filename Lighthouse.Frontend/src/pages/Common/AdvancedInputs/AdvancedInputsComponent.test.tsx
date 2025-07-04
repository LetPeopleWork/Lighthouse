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
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 20,
			workItemQuery: "",
			workItemTypes: [],
			tags: [],
			workTrackingSystemConnectionId: 12,
			parentOverrideField: "Test Field",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
		};

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
		const teamSettings: ITeamSettings = {
			id: 0,
			name: "setting",
			throughputHistory: 2,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 2,
			workItemQuery: "",
			workItemTypes: [],
			tags: [],
			workTrackingSystemConnectionId: 12,
			parentOverrideField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
		};

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
