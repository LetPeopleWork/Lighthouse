import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, vi } from "vitest";
import FilterBar from "./FilterBar";

describe("FilterBar", () => {
	test("renders the input with the correct placeholder and value", () => {
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
			expect.stringContaining("Search by project or team name"),
		);
		expect(inputElement).toHaveValue(filterText);
	});

	test("calls onFilterTextChange when the input value changes", () => {
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
});
