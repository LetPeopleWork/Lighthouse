import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ChartLegend from "./ChartLegend";

// Epic #5585 slice 04 (US-04). The delivery Metrics cards run tall because a per-epic legend wraps to
// eight lines on a real delivery, and filtering by legend is a special-case action — so the legend is
// collapsed by default and costs one click to open. Extracted from DeliveryFeverChart's private
// FeatureLegend so the fever chart and the size chart behave the same way on the same tab.

const items = [
	{ id: "EPIC-A", label: "Checkout", color: "#111111" },
	{ id: "EPIC-B", label: "Search", color: "#222222" },
	{ id: "EPIC-C", label: "Billing", color: "#333333" },
];

const renderLegend = (overrides: Record<string, unknown> = {}) =>
	render(
		<ChartLegend
			items={items}
			selected={new Set<string>()}
			onToggle={vi.fn()}
			onShowAll={vi.fn()}
			{...overrides}
		/>,
	);

const expand = () =>
	fireEvent.click(screen.getByRole("button", { name: /legend/i }));

describe("ChartLegend", () => {
	it("stays out of the way until asked for", () => {
		renderLegend();

		expect(screen.queryByRole("button", { name: "Checkout" })).toBeNull();
	});

	it("lists one entry per item once opened", () => {
		renderLegend();

		expand();

		for (const item of items) {
			expect(
				screen.getByRole("button", { name: item.label }),
			).toBeInTheDocument();
		}
	});

	it("says how many entries it is hiding, so it is worth opening", () => {
		renderLegend();

		expect(screen.getByRole("button", { name: /legend/i })).toHaveTextContent(
			"3",
		);
	});

	it("reports which entry the forecaster clicked", () => {
		const onToggle = vi.fn();
		renderLegend({ onToggle });

		expand();
		fireEvent.click(screen.getByRole("button", { name: "Search" }));

		expect(onToggle).toHaveBeenCalledWith("EPIC-B");
	});

	it("marks every entry as showing while nothing is picked", () => {
		// An empty selection means "show everything", not "show nothing" — the forecaster has not
		// filtered yet, so no entry should look switched off.
		renderLegend();

		expand();

		for (const item of items) {
			expect(screen.getByRole("button", { name: item.label })).toHaveAttribute(
				"aria-pressed",
				"true",
			);
		}
	});

	it("switches off the entries that were not picked (AC-4.2)", () => {
		// Picking one entry isolates it. Every entry stays listed — that is how the forecaster adds a
		// second one, and for the size chart it is how a departed epic stays reachable (D7).
		renderLegend({ selected: new Set(["EPIC-B"]) });

		expand();

		expect(screen.getByRole("button", { name: "Search" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		expect(screen.getByRole("button", { name: "Checkout" })).toHaveAttribute(
			"aria-pressed",
			"false",
		);
	});

	it("clears the whole selection in one action (AC-4.4)", () => {
		const onShowAll = vi.fn();
		renderLegend({ selected: new Set(["EPIC-B"]), onShowAll });

		expand();
		fireEvent.click(screen.getByRole("button", { name: /show all/i }));

		expect(onShowAll).toHaveBeenCalled();
	});

	it("offers no reset while nothing is filtered", () => {
		renderLegend();

		expand();

		expect(screen.queryByRole("button", { name: /show all/i })).toBeNull();
	});

	it("says which way clicking it will go", () => {
		// Review 2026-08-02: the toggle read as a label, not a control.
		renderLegend();

		const toggle = screen.getByRole("button", { name: /legend/i });
		expect(toggle).toHaveAttribute("aria-expanded", "false");

		fireEvent.click(toggle);

		expect(toggle).toHaveAttribute("aria-expanded", "true");
	});

	it("turns its arrow round when it opens", () => {
		const { container } = renderLegend();

		const closed = container.querySelector("svg")?.getAttribute("data-testid");
		expand();
		const open = container.querySelector("svg")?.getAttribute("data-testid");

		expect(closed).toBeTruthy();
		expect(open).not.toBe(closed);
	});
});
