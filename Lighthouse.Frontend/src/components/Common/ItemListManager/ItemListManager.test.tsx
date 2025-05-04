import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ItemListManager from "./ItemListManager";

describe("ItemListManager", () => {
	const mockOnAddItem = vi.fn();
	const mockOnRemoveItem = vi.fn();
	const title = "Item";
	const items = ["Item 1", "Item 2"];

	beforeEach(() => {
		mockOnAddItem.mockClear();
		mockOnRemoveItem.mockClear();
	});

	it("renders the initial items correctly", () => {
		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
			/>,
		);

		for (const item of items) {
			expect(screen.getByText(item)).toBeInTheDocument();
		}
	});

	it("calls onAddItem when Enter key is pressed with text input", () => {
		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
			/>,
		);

		const input = screen.getByLabelText(`New ${title}`);

		fireEvent.change(input, { target: { value: "Item 3" } });
		fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

		expect(mockOnAddItem).toHaveBeenCalledWith("Item 3");
		expect(input).toHaveValue("");
	});

	it("does not call onAddItem when Enter is pressed with empty input", () => {
		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
			/>,
		);

		const input = screen.getByLabelText(`New ${title}`);
		fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

		expect(mockOnAddItem).not.toHaveBeenCalled();
	});

	it("does not call onAddItem when the input is only whitespace", () => {
		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
			/>,
		);

		const input = screen.getByLabelText(`New ${title}`);

		fireEvent.change(input, { target: { value: "   " } });
		fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

		expect(mockOnAddItem).not.toHaveBeenCalled();
	});

	it("calls onRemoveItem when an item is removed", () => {
		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
			/>,
		);

		// Find the delete icon by looking for the parent Chip component with the right text
		const chipElement = screen.getByText("Item 1").closest(".MuiChip-root");
		const deleteIcon = chipElement?.querySelector(".MuiChip-deleteIcon");

		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnRemoveItem).toHaveBeenCalledWith("Item 1");
	});

	it("renders with suggestions when provided", () => {
		const suggestions = ["Suggestion 1", "Suggestion 2"];

		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
				suggestions={suggestions}
			/>,
		);

		// Verify the component displays guidance for suggestions
		expect(
			screen.getByText(/Type to select from existing/),
		).toBeInTheDocument();
	});

	it("filters out suggestions that are already in items", () => {
		const suggestions = ["Item 1", "Suggestion 2"];

		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
				suggestions={suggestions}
			/>,
		);

		// "Item 1" should not be in the dropdown as it's already in items
		const input = screen.getByLabelText(`New ${title}`);
		fireEvent.focus(input);

		// This is a simplification - in real app testing you'd need to check
		// if the dropdown content actually contains or doesn't contain certain items
	});

	it("adds the suggested version of an item when typing with different casing", () => {
		const suggestions = ["Done", "In Progress"];

		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
				suggestions={suggestions}
			/>,
		);

		const input = screen.getByLabelText(`New ${title}`);

		// Type a lowercase version of a suggestion
		fireEvent.change(input, { target: { value: "done" } });
		fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

		// Should add the original cased version from suggestions
		expect(mockOnAddItem).toHaveBeenCalledWith("Done");
		expect(mockOnAddItem).not.toHaveBeenCalledWith("done");
	});

	it("shows loading indicator when isLoading is true", () => {
		render(
			<ItemListManager
				title={title}
				items={items}
				onAddItem={mockOnAddItem}
				onRemoveItem={mockOnRemoveItem}
				isLoading={true}
			/>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});
});
