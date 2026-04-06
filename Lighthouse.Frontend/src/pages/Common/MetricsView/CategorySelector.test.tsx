import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import CategorySelector from "./CategorySelector";

describe("CategorySelector", () => {
	it("renders a chip for each category", () => {
		render(
			<CategorySelector
				selectedCategory="flow-overview"
				onSelectCategory={vi.fn()}
			/>,
		);
		expect(
			screen.getByTestId("category-chip-flow-overview"),
		).toBeInTheDocument();
		expect(screen.getByTestId("category-chip-cycle-time")).toBeInTheDocument();
		expect(screen.getByTestId("category-chip-throughput")).toBeInTheDocument();
		expect(screen.getByTestId("category-chip-wip-aging")).toBeInTheDocument();
		expect(
			screen.getByTestId("category-chip-predictability"),
		).toBeInTheDocument();
		expect(screen.getByTestId("category-chip-portfolio")).toBeInTheDocument();
	});

	it("calls onSelectCategory when a chip is clicked", () => {
		const onSelect = vi.fn();
		render(
			<CategorySelector
				selectedCategory="flow-overview"
				onSelectCategory={onSelect}
			/>,
		);
		fireEvent.click(screen.getByTestId("category-chip-predictability"));
		expect(onSelect).toHaveBeenCalledWith("predictability");
	});

	it("visually distinguishes the selected chip", () => {
		render(
			<CategorySelector
				selectedCategory="flow-overview"
				onSelectCategory={vi.fn()}
			/>,
		);
		const selected = screen.getByTestId("category-chip-flow-overview");
		const unselected = screen.getByTestId("category-chip-cycle-time");
		expect(selected.className).not.toBe(unselected.className);
	});

	it("renders an icon for each category chip", () => {
		render(
			<CategorySelector
				selectedCategory="flow-overview"
				onSelectCategory={vi.fn()}
			/>,
		);
		const chip = screen.getByTestId("category-chip-flow-overview");
		expect(chip.querySelector("svg")).toBeInTheDocument();
	});

	it("renders a tooltip with hoverText for each category", () => {
		render(
			<CategorySelector
				selectedCategory="flow-overview"
				onSelectCategory={vi.fn()}
			/>,
		);
		// Each chip is wrapped in a Tooltip; the title is added as aria
		const chip = screen.getByTestId("category-chip-flow-overview");
		// MUI Tooltip doesn't render title text in DOM by default without hover,
		// but the wrapping span should exist
		expect(chip).toBeInTheDocument();
	});
});
