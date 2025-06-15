import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../../models/Project/ProjectSettings";
import UnparentedItemsComponent from "./UnparentedItemsComponent";

describe("UnparentedItemsComponent", () => {
	const initialSettings: IProjectSettings = {
		id: 1,
		name: "Settings",
		workItemTypes: [],
		milestones: [],
		workItemQuery: "Initial Query",
		unparentedItemsQuery: "Unparented Query",
		usePercentileToCalculateDefaultAmountOfWorkItems: false,
		defaultAmountOfWorkItemsPerFeature: 10,
		defaultWorkItemPercentile: 85,
		historicalFeaturesWorkItemQuery: "Historical Feature Query",
		workTrackingSystemConnectionId: 12,
		sizeEstimateField: "",
		tags: [],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
		overrideRealChildCountStates: [""],
		involvedTeams: [],
		serviceLevelExpectationProbability: 85,
		serviceLevelExpectationRange: 30,
		systemWipLimit: 0,
	};

	const mockOnProjectSettingsChange = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders correctly with initial settings", () => {
		render(
			<UnparentedItemsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(screen.getByLabelText(/Unparented Work Items Query/i)).toHaveValue(
			"Unparented Query",
		);
	});

	it("calls onProjectSettingsChange with correct arguments when unparentedItemsQuery changes", () => {
		render(
			<UnparentedItemsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Unparented Work Items Query/i), {
			target: { value: "Updated Query" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"unparentedItemsQuery",
			"Updated Query",
		);
	});
});
