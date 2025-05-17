import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import ServiceLevelExpectationConfigurationComponent from "./ServiceLevelExpectationConfigurationComponent";

describe("ServiceLevelExpectationConfigurationComponent", () => {
	const mockOnSettingsChange = vi.fn();

	const testSettings: IBaseSettings = {
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
	};

	const disabledSettings: IBaseSettings = {
		...testSettings,
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
	};

	beforeEach(() => {
		mockOnSettingsChange.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("renders correctly with provided settings (enabled state)", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(
			screen.getByLabelText("Set Service Level Expectation"),
		).toBeChecked();

		// The input field contains a number, so we need to convert our expected values to numbers
		const probabilityInput = screen.getByLabelText("Probability (%)");
		const rangeInput = screen.getByLabelText("Range (in days)");

		expect(Number(probabilityInput.getAttribute("value"))).toBe(85);
		expect(Number(rangeInput.getAttribute("value"))).toBe(30);
	});

	it("renders correctly with disabled settings", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={disabledSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(
			screen.getByLabelText("Set Service Level Expectation"),
		).not.toBeChecked();
		expect(screen.queryByLabelText("Probability (%)")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("Range (in days)")).not.toBeInTheDocument();
	});

	it("enables SLE with default values when checkbox is checked", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={disabledSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.click(screen.getByLabelText("Set Service Level Expectation"));

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			80,
		);
		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			10,
		);
	});

	it("disables SLE when checkbox is unchecked", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.click(screen.getByLabelText("Set Service Level Expectation"));

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			0,
		);
		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			0,
		);
	});

	it("limits probability to minimum value of 50", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Probability (%)"), {
			target: { value: "30" },
		});

		vi.advanceTimersByTime(1000);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			50,
		);
	});

	it("limits probability to maximum value of 95", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Probability (%)"), {
			target: { value: "100" },
		});

		vi.advanceTimersByTime(1000);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			95,
		);
	});

	it("limits range to minimum value of 1", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Range (in days)"), {
			target: { value: "0" },
		});

		vi.advanceTimersByTime(1000);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			1,
		);
	});

	it("handles normal probability input changes", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Probability (%)"), {
			target: { value: "75" },
		});

		vi.advanceTimersByTime(1000);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationProbability",
			75,
		);
	});

	it("handles normal range input changes", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Range (in days)"), {
			target: { value: "15" },
		});

		vi.advanceTimersByTime(1000);

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"serviceLevelExpectationRange",
			15,
		);
	});

	it("handles null settings gracefully", () => {
		render(
			<ServiceLevelExpectationConfigurationComponent
				settings={null}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(
			screen.getByLabelText("Set Service Level Expectation"),
		).not.toBeChecked();
	});
});
