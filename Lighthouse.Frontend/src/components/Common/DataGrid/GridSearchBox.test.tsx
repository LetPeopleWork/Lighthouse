import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import GridSearchBox from "./GridSearchBox";

describe("GridSearchBox", () => {
	it("offers no clear button while the box is empty", () => {
		render(
			<GridSearchBox placeholder="Search" text="" onTextChange={vi.fn()} />,
		);

		expect(
			screen.queryByRole("button", { name: "Clear search" }),
		).not.toBeInTheDocument();
	});

	it("offers a clear button once text is typed, and clearing empties the text", () => {
		const onTextChange = vi.fn();
		render(
			<GridSearchBox
				placeholder="Search"
				text="abc"
				onTextChange={onTextChange}
			/>,
		);

		fireEvent.click(screen.getByRole("button", { name: "Clear search" }));

		expect(onTextChange).toHaveBeenCalledWith("");
	});
});
