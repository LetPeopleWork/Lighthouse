import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import CategorySelector from "./CategorySelector";

describe("CategorySelector", () => {
	it("renders a chip for each category", () => {
		render(
			<CategorySelector
				selectedCategory="flow-health"
				onSelectCategory={vi.fn()}
			/>,
		);
		expect(screen.getByTestId("category-chip-flow-health")).toBeInTheDocument();
		expect(
			screen.getByTestId("category-chip-aging-stability"),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("category-chip-predictability"),
		).toBeInTheDocument();
		expect(screen.getByTestId("category-chip-portfolio")).toBeInTheDocument();
		expect(screen.getByTestId("category-chip-overview")).toBeInTheDocument();
	});

	it("calls onSelectCategory when a chip is clicked", () => {
		const onSelect = vi.fn();
		render(
			<CategorySelector
				selectedCategory="flow-health"
				onSelectCategory={onSelect}
			/>,
		);
		fireEvent.click(screen.getByTestId("category-chip-predictability"));
		expect(onSelect).toHaveBeenCalledWith("predictability");
	});

	it("visually distinguishes the selected chip", () => {
		render(
			<CategorySelector
				selectedCategory="overview"
				onSelectCategory={vi.fn()}
			/>,
		);
		const selected = screen.getByTestId("category-chip-overview");
		const unselected = screen.getByTestId("category-chip-flow-health");
		expect(selected.className).not.toBe(unselected.className);
	});
});
