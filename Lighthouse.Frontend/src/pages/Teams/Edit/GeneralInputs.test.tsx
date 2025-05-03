import { fireEvent, render, screen } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import GeneralInputsComponent from "./GeneralInputs";

// Mock InputGroup component
vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	__esModule: true,
	default: ({
		title,
		children,
	}: { title: string; children: React.ReactNode }) => (
		<div>
			<h2>{title}</h2>
			{children}
		</div>
	),
}));

describe("GeneralInputsComponent", () => {
	const onTeamSettingsChange = vi.fn();

	const teamSettings: ITeamSettings = {
		id: 0,
		name: "Test Name",
		throughputHistory: 10,
		useFixedDatesForThroughput: false,
		throughputHistoryStartDate: new Date(),
		throughputHistoryEndDate: new Date(),
		featureWIP: 20,
		workItemQuery: "Test Query",
		workItemTypes: [],
		tags: [],
		workTrackingSystemConnectionId: 12,
		relationCustomField: "Test Field",
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
		automaticallyAdjustFeatureWIP: false,
	};

	it("renders correctly with provided teamSettings", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		// Check if the TextFields display the correct values
		expect(screen.getByLabelText("Name")).toHaveValue("Test Name");
		expect(screen.getByLabelText("Throughput History")).toHaveValue(10);
		expect(screen.getByLabelText("Work Item Query")).toHaveValue("Test Query");
	});

	it("calls onTeamSettingsChange with correct parameters when the Name TextField value changes", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Name"), {
			target: { value: "New Name" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith("name", "New Name");
	});

	it("calls onTeamSettingsChange with correct parameters when Throughput History TextField value changes", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Throughput History"), {
			target: { value: "15" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith("throughputHistory", 15);
	});

	it("calls onTeamSettingsChange with correct parameters when Work Item Query TextField value changes", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Work Item Query"), {
			target: { value: "New Query" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"workItemQuery",
			"New Query",
		);
	});

	it("renders date inputs when useFixedDatesForThroughput is true", () => {
		const settingsWithFixedDates = {
			...teamSettings,
			useFixedDatesForThroughput: true,
		};

		render(
			<GeneralInputsComponent
				teamSettings={settingsWithFixedDates}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(screen.getByLabelText("Start Date")).toBeInTheDocument();
		expect(screen.getByLabelText("End Date")).toBeInTheDocument();
		expect(
			screen.queryByLabelText("Throughput History"),
		).not.toBeInTheDocument();
	});

	it("renders throughput history input when useFixedDatesForThroughput is false", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(screen.queryByLabelText("Start Date")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("End Date")).not.toBeInTheDocument();
		expect(screen.getByLabelText("Throughput History")).toBeInTheDocument();
	});

	it("calls onTeamSettingsChange when useFixedDatesForThroughput switch is toggled", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		const switchElement = screen.getByRole("checkbox", {
			name: "Use Fixed Dates for Throughput",
		});
		fireEvent.click(switchElement);

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"useFixedDatesForThroughput",
			true,
		);
	});

	it("calls onTeamSettingsChange when date inputs are changed", () => {
		const settingsWithFixedDates = {
			...teamSettings,
			useFixedDatesForThroughput: true,
		};

		render(
			<GeneralInputsComponent
				teamSettings={settingsWithFixedDates}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Start Date"), {
			target: { value: "2023-01-01" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"throughputHistoryStartDate",
			expect.any(Date),
		);

		fireEvent.change(screen.getByLabelText("End Date"), {
			target: { value: "2023-01-15" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"throughputHistoryEndDate",
			expect.any(Date),
		);
	});

	it("does not render fixed dates switch when isDefaultSettings is true", () => {
		render(
			<GeneralInputsComponent
				teamSettings={teamSettings}
				isDefaultSettings={true}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(
			screen.queryByRole("checkbox", {
				name: "Use Fixed Dates for Throughput",
			}),
		).not.toBeInTheDocument();
	});
});
