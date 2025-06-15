import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import FlowMetricsConfigurationComponent from "./FlowMetricsConfigurationComponent";

describe("FlowMetricsConfigurationComponent", () => {
	const mockOnSettingsChange = vi.fn();

	const fullTestSettings: IBaseSettings & {
		featureWIP?: number;
		automaticallyAdjustFeatureWIP?: boolean;
	} = {
		id: 1,
		name: "Test Settings",
		workItemQuery: "Test Query",
		workItemTypes: [],
		toDoStates: [],
		doingStates: [],
		doneStates: [],
		tags: [],
		workTrackingSystemConnectionId: 1,
		serviceLevelExpectationProbability: 85,
		serviceLevelExpectationRange: 30,
		systemWipLimit: 5,
		featureWIP: 3,
		automaticallyAdjustFeatureWIP: true,
	};

	const disabledSettings: IBaseSettings & {
		featureWIP?: number;
		automaticallyAdjustFeatureWIP?: boolean;
	} = {
		...fullTestSettings,
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWipLimit: 0,
		featureWIP: 0,
		automaticallyAdjustFeatureWIP: false,
	};

	beforeEach(() => {
		mockOnSettingsChange.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("renders correctly with provided settings (all enabled)", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		// Check WIP Limit section
		expect(screen.getByLabelText("Set System WIP Limit")).toBeChecked();
		expect(screen.getByLabelText("WIP Limit")).toHaveValue(5);

		// Check SLE section
		expect(
			screen.getByLabelText("Set Service Level Expectation"),
		).toBeChecked();
		expect(screen.getByLabelText("Probability (%)")).toHaveValue(85);
		expect(screen.getByLabelText("Range (in days)")).toHaveValue(30);
	});

	it("renders correctly with disabled settings", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={disabledSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		// Check WIP Limit section
		expect(screen.getByLabelText("Set System WIP Limit")).not.toBeChecked();
		expect(screen.queryByLabelText("WIP Limit")).not.toBeInTheDocument();

		// Check SLE section
		expect(
			screen.getByLabelText("Set Service Level Expectation"),
		).not.toBeChecked();
		expect(screen.queryByLabelText("Probability (%)")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("Range (in days)")).not.toBeInTheDocument();
	});

	it("enables WIP Limit when checkbox is clicked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={disabledSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const checkbox = screen.getByLabelText("Set System WIP Limit");
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith("systemWipLimit", 5);
		expect(checkbox).toBeChecked();
	});

	it("disables WIP Limit when checkbox is unchecked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const checkbox = screen.getByLabelText("Set System WIP Limit");
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith("systemWipLimit", 0);
		expect(checkbox).not.toBeChecked();
	});

	it("updates WIP Limit when input changes", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const wipLimitInput = screen.getByLabelText("WIP Limit");
		fireEvent.change(wipLimitInput, { target: { value: "10" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith("systemWipLimit", 10);
	});

	it("enforces minimum WIP Limit value of 1", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const wipLimitInput = screen.getByLabelText("WIP Limit");
		fireEvent.change(wipLimitInput, { target: { value: "-5" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith("systemWipLimit", 1);
	});

	it("enables SLE when checkbox is clicked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={disabledSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const checkbox = screen.getByLabelText("Set Service Level Expectation");
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			80,
		);
		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			10,
		);
		expect(checkbox).toBeChecked();
	});

	it("disables SLE when checkbox is unchecked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const checkbox = screen.getByLabelText("Set Service Level Expectation");
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			0,
		);
		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			0,
		);
		expect(checkbox).not.toBeChecked();
	});

	it("updates probability when input changes", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const probabilityInput = screen.getByLabelText("Probability (%)");
		fireEvent.change(probabilityInput, { target: { value: "90" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			90,
		);
	});

	it("enforces minimum probability value of 50", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const probabilityInput = screen.getByLabelText("Probability (%)");
		fireEvent.change(probabilityInput, { target: { value: "40" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			50,
		);
	});

	it("enforces maximum probability value of 95", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const probabilityInput = screen.getByLabelText("Probability (%)");
		fireEvent.change(probabilityInput, { target: { value: "99" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			95,
		);
	});

	it("updates range when input changes", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const rangeInput = screen.getByLabelText("Range (in days)");
		fireEvent.change(rangeInput, { target: { value: "15" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			15,
		);
	});

	it("enforces minimum range value of 1", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		const rangeInput = screen.getByLabelText("Range (in days)");
		fireEvent.change(rangeInput, { target: { value: "0" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			1,
		);
	});

	it("renders feature WIP section when showFeatureWip is true", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={true}
			/>,
		);

		// Feature WIP checkbox should be shown and checked
		expect(screen.getByLabelText("Set Feature WIP")).toBeChecked();
		expect(screen.getByLabelText("Feature WIP")).toHaveValue(3);
		expect(
			screen.getByLabelText(
				"Automatically Adjust Feature WIP based on actual WIP",
			),
		).toBeChecked();
	});

	it("doesn't render feature WIP section when showFeatureWip is false", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={false}
			/>,
		);

		// Feature WIP checkbox should not be shown
		expect(screen.queryByLabelText("Set Feature WIP")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("Feature WIP")).not.toBeInTheDocument();
		expect(
			screen.queryByLabelText(
				"Automatically Adjust Feature WIP based on actual WIP",
			),
		).not.toBeInTheDocument();
	});

	it("enables Feature WIP when checkbox is clicked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={disabledSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={true}
			/>,
		);

		const checkbox = screen.getByLabelText("Set Feature WIP");
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith("featureWIP", 5);
		expect(checkbox).toBeChecked();
	});

	it("disables Feature WIP when checkbox is unchecked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={true}
			/>,
		);

		const checkbox = screen.getByLabelText("Set Feature WIP");
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith("featureWIP", 0);
		expect(checkbox).not.toBeChecked();
	});

	it("updates Feature WIP when input changes", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={true}
			/>,
		);

		const featureWipInput = screen.getByLabelText("Feature WIP");
		fireEvent.change(featureWipInput, { target: { value: "10" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith("featureWIP", 10);
	});

	it("enforces minimum Feature WIP value of 1", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={true}
			/>,
		);

		const featureWipInput = screen.getByLabelText("Feature WIP");
		fireEvent.change(featureWipInput, { target: { value: "-5" } });

		vi.advanceTimersByTime(1100); // Wait for debounce

		expect(mockOnSettingsChange).toHaveBeenCalledWith("featureWIP", 1);
	});

	it("toggles automaticallyAdjustFeatureWIP when checkbox is clicked", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={fullTestSettings}
				onSettingsChange={mockOnSettingsChange}
				showFeatureWip={true}
			/>,
		);

		const checkbox = screen.getByLabelText(
			"Automatically Adjust Feature WIP based on actual WIP",
		);
		fireEvent.click(checkbox);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"automaticallyAdjustFeatureWIP",
			false,
		);
	});
});
