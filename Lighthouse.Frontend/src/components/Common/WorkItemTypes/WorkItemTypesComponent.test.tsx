import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import WorkItemTypesComponent from "./WorkItemTypesComponent";

describe("WorkItemTypesComponent", () => {
	const mockOnAddWorkItemType = vi.fn();
	const mockOnRemoveWorkItemType = vi.fn();

	const workItemTypes = ["Bug", "Feature", "Task"];

	beforeEach(() => {
		mockOnAddWorkItemType.mockClear();
		mockOnRemoveWorkItemType.mockClear();
	});

	it("renders correctly", () => {
		render(
			<WorkItemTypesComponent
				workItemTypes={workItemTypes}
				onAddWorkItemType={mockOnAddWorkItemType}
				onRemoveWorkItemType={mockOnRemoveWorkItemType}
			/>,
		);

		expect(screen.getByText("Work Item Types")).toBeInTheDocument();

		for (const type of workItemTypes) {
			expect(screen.getByText(type)).toBeInTheDocument();
		}

		expect(screen.getByLabelText("New Work Item Type")).toBeInTheDocument();
		// Check for the help text instead of a button since there isn't an actual "Add" button
		expect(
			screen.getByText(/Type a new work item type and press Enter to add/i),
		).toBeInTheDocument();
	});

	it("calls onAddWorkItemType when a new work item type is added", () => {
		render(
			<WorkItemTypesComponent
				workItemTypes={workItemTypes}
				onAddWorkItemType={mockOnAddWorkItemType}
				onRemoveWorkItemType={mockOnRemoveWorkItemType}
			/>,
		);

		const input = screen.getByLabelText("New Work Item Type");

		fireEvent.change(input, { target: { value: "Improvement" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mockOnAddWorkItemType).toHaveBeenCalledWith("Improvement");
		expect(mockOnAddWorkItemType).toHaveBeenCalledTimes(1);

		expect(input).toHaveValue("");
	});

	it("does not call onAddWorkItemType when the input is empty", () => {
		render(
			<WorkItemTypesComponent
				workItemTypes={workItemTypes}
				onAddWorkItemType={mockOnAddWorkItemType}
				onRemoveWorkItemType={mockOnRemoveWorkItemType}
			/>,
		);

		const input = screen.getByLabelText("New Work Item Type");

		fireEvent.change(input, { target: { value: "" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(mockOnAddWorkItemType).not.toHaveBeenCalled();
	});

	it("calls onRemoveWorkItemType when a work item type is removed", () => {
		render(
			<WorkItemTypesComponent
				workItemTypes={workItemTypes}
				onAddWorkItemType={mockOnAddWorkItemType}
				onRemoveWorkItemType={mockOnRemoveWorkItemType}
			/>,
		);

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Bug").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnRemoveWorkItemType).toHaveBeenCalledWith(workItemTypes[0]);
		expect(mockOnRemoveWorkItemType).toHaveBeenCalledTimes(1);
	});
});
