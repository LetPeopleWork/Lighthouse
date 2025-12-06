import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import FilterBar from "./FilterBar";

// Mock for window event listeners
const addEventListenerMock = vi.spyOn(window, "addEventListener");
const removeEventListenerMock = vi.spyOn(window, "removeEventListener");

describe("FilterBar", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders the input with the correct placeholder and value", () => {
		const filterText = "test";
		const onFilterTextChange = vi.fn();

		render(
			<FilterBar
				filterText={filterText}
				onFilterTextChange={onFilterTextChange}
			/>,
		);

		const inputElement = screen.getByRole("textbox");
		expect(inputElement).toBeInTheDocument();
		expect(inputElement).toHaveAttribute(
			"placeholder",
			expect.stringContaining("Search by portfolio or Team name"),
		);
		expect(inputElement).toHaveValue(filterText);
	});

	it("calls onFilterTextChange when the input value changes", () => {
		const filterText = "test";
		const onFilterTextChange = vi.fn();

		render(
			<FilterBar
				filterText={filterText}
				onFilterTextChange={onFilterTextChange}
			/>,
		);

		const inputElement = screen.getByRole("textbox");
		fireEvent.change(inputElement, { target: { value: "new text" } });

		// Check if onFilterTextChange is called with the correct value
		expect(onFilterTextChange).toHaveBeenCalledTimes(1);
		expect(onFilterTextChange).toHaveBeenCalledWith("new text");
	});

	it("sets up keyboard event listeners", () => {
		const onFilterTextChange = vi.fn();

		render(<FilterBar filterText="" onFilterTextChange={onFilterTextChange} />);

		expect(addEventListenerMock).toHaveBeenCalledWith(
			"keydown",
			expect.any(Function),
		);
	});

	it("cleans up keyboard event listeners on unmount", () => {
		const onFilterTextChange = vi.fn();

		const { unmount } = render(
			<FilterBar filterText="" onFilterTextChange={onFilterTextChange} />,
		);

		unmount();
		expect(removeEventListenerMock).toHaveBeenCalledWith(
			"keydown",
			expect.any(Function),
		);
	});

	it("updates local filter text when external filter text changes", () => {
		const onFilterTextChange = vi.fn();
		const { rerender } = render(
			<FilterBar
				filterText="initial"
				onFilterTextChange={onFilterTextChange}
			/>,
		);

		// Check initial value
		expect(screen.getByRole("textbox")).toHaveValue("initial");

		// Update external filter text
		rerender(
			<FilterBar
				filterText="updated"
				onFilterTextChange={onFilterTextChange}
			/>,
		);

		// Verify component updated to match new external value
		expect(screen.getByRole("textbox")).toHaveValue("updated");
	});

	it("clears the filter when clear button is clicked", () => {
		const onFilterTextChange = vi.fn();
		render(
			<FilterBar filterText="test" onFilterTextChange={onFilterTextChange} />,
		);

		// Find and click the clear button
		const clearButton = screen.getByRole("button", { name: /clear search/i });
		fireEvent.click(clearButton);

		// Verify onFilterTextChange was called with empty string
		expect(onFilterTextChange).toHaveBeenCalledWith("");
	});
});
