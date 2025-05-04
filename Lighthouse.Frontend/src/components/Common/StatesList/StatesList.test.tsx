import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import StatesList from "./StatesList";

describe("StatesList", () => {
	const mockOnAddToDoState = vi.fn();
	const mockOnRemoveToDoState = vi.fn();
	const mockOnAddDoingState = vi.fn();
	const mockOnRemoveDoingState = vi.fn();
	const mockOnAddDoneState = vi.fn();
	const mockOnRemoveDoneState = vi.fn();

	const toDoStates = ["Task 1", "Task 2"];
	const doingStates = ["In Progress"];
	const doneStates = ["Completed"];

	beforeEach(() => {
		mockOnAddToDoState.mockClear();
		mockOnRemoveToDoState.mockClear();
		mockOnAddDoingState.mockClear();
		mockOnRemoveDoingState.mockClear();
		mockOnAddDoneState.mockClear();
		mockOnRemoveDoneState.mockClear();
	});

	it("renders the title correctly", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		expect(screen.getByText("States")).toBeInTheDocument();
	});

	it("renders to do states correctly", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		for (const state of toDoStates) {
			expect(screen.getByText(state)).toBeInTheDocument();
		}
	});

	it("calls onAddToDoState when a new to do state is added", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		const input = screen.getByLabelText("New To Do States");

		fireEvent.change(input, { target: { value: "Task 3" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mockOnAddToDoState).toHaveBeenCalledWith("Task 3");
	});

	it("calls onRemoveToDoState when an item is removed", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Task 1").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnRemoveToDoState).toHaveBeenCalledWith("Task 1");
	});

	it("renders doing states correctly", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		for (const state of doingStates) {
			expect(screen.getByText(state)).toBeInTheDocument();
		}
	});

	it("calls onAddDoingState when a new doing state is added", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		const input = screen.getByLabelText("New Doing States");

		fireEvent.change(input, { target: { value: "Task 4" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mockOnAddDoingState).toHaveBeenCalledWith("Task 4");
	});

	it("calls onRemoveDoingState when an item is removed", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen
			.getByText("In Progress")
			.closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnRemoveDoingState).toHaveBeenCalledWith("In Progress");
	});

	it("renders done states correctly", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		for (const state of doneStates) {
			expect(screen.getByText(state)).toBeInTheDocument();
		}
	});

	it("calls onAddDoneState when a new done state is added", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		const input = screen.getByLabelText("New Done States");

		fireEvent.change(input, { target: { value: "Task 5" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mockOnAddDoneState).toHaveBeenCalledWith("Task 5");
	});

	it("calls onRemoveDoneState when an item is removed", () => {
		render(
			<StatesList
				toDoStates={toDoStates}
				onAddToDoState={mockOnAddToDoState}
				onRemoveToDoState={mockOnRemoveToDoState}
				doingStates={doingStates}
				onAddDoingState={mockOnAddDoingState}
				onRemoveDoingState={mockOnRemoveDoingState}
				doneStates={doneStates}
				onAddDoneState={mockOnAddDoneState}
				onRemoveDoneState={mockOnRemoveDoneState}
			/>,
		);

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Completed").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnRemoveDoneState).toHaveBeenCalledWith("Completed");
	});
});
