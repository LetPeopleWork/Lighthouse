import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import AdvancedInputsComponent from "./AdvancedInputs";

describe("AdvancedInputsComponent", () => {
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
	};

	const mockOnProjectSettingsChange = vi.fn();

	it("renders correctly with initial settings", () => {
		render(
			<AdvancedInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(screen.getByLabelText(/Unparented Work Items Query/i)).toHaveValue(
			"Unparented Query",
		);
		expect(
			screen.getByLabelText(/Default Number of Items per Feature/i),
		).toHaveValue(10);
		expect(screen.getByLabelText(/Size Estimate Field/i)).toHaveValue("");
	});

	it("calls onProjectSettingsChange with correct arguments when unparentedItemsQuery changes", () => {
		render(
			<AdvancedInputsComponent
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

	it("calls onProjectSettingsChange with correct arguments when defaultAmountOfWorkItemsPerFeature changes", () => {
		render(
			<AdvancedInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(
			screen.getByLabelText(/Default Number of Items per Feature/i),
			{ target: { value: "20" } },
		);

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"defaultAmountOfWorkItemsPerFeature",
			20,
		);
	});

	it("calls onProjectSettingsChange with correct arguments when sizeEstimateField changes", () => {
		render(
			<AdvancedInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Size Estimate Field/i), {
			target: { value: "customfield_133742" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"sizeEstimateField",
			"customfield_133742",
		);
	});

	it("toggles the usePercentileToCalculateDefaultAmountOfWorkItems switch", () => {
		render(
			<AdvancedInputsComponent
				projectSettings={initialSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		const toggleSwitch = screen.getByLabelText(
			/Use Historical Feature Size To Calculate Default/i,
		);

		expect(toggleSwitch).not.toBeChecked();
		fireEvent.click(toggleSwitch);
		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"usePercentileToCalculateDefaultAmountOfWorkItems",
			true,
		);
	});

	it("renders Feature Size Percentile and Historical Features Work Item Query when switch is on", () => {
		const updatedSettings: IProjectSettings = {
			...initialSettings,
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
		};

		render(
			<AdvancedInputsComponent
				projectSettings={updatedSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		expect(
			screen.getByLabelText(/Feature Size Percentile/i),
		).toBeInTheDocument();
		expect(
			screen.getByLabelText(/Historical Features Work Item Query/i),
		).toBeInTheDocument();
	});

	it("calls onProjectSettingsChange with correct arguments when defaultWorkItemPercentile changes", () => {
		const updatedSettings: IProjectSettings = {
			...initialSettings,
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
		};

		render(
			<AdvancedInputsComponent
				projectSettings={updatedSettings}
				onProjectSettingsChange={mockOnProjectSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText(/Feature Size Percentile/i), {
			target: { value: "90" },
		});

		expect(mockOnProjectSettingsChange).toHaveBeenCalledWith(
			"defaultWorkItemPercentile",
			90,
		);
	});
});
