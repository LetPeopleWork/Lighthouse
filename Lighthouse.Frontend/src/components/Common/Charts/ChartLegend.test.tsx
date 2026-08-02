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
			hidden={new Set<string>()}
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

	it("shows a hidden entry as switched off rather than dropping it", () => {
		// A filtered-out epic must stay listed — it is how the forecaster switches it back on, and for
		// the size chart it is also how a departed epic stays visible in the window (D7).
		renderLegend({ hidden: new Set(["EPIC-B"]) });

		expand();

		expect(screen.getByRole("button", { name: "Search" })).toHaveAttribute(
			"aria-pressed",
			"false",
		);
		expect(screen.getByRole("button", { name: "Checkout" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
	});

	it("clears the whole selection in one action (AC-4.4)", () => {
		const onShowAll = vi.fn();
		renderLegend({ hidden: new Set(["EPIC-B", "EPIC-C"]), onShowAll });

		expand();
		fireEvent.click(screen.getByRole("button", { name: /show all/i }));

		expect(onShowAll).toHaveBeenCalled();
	});

	it("offers no reset while nothing is filtered", () => {
		renderLegend();

		expand();

		expect(screen.queryByRole("button", { name: /show all/i })).toBeNull();
	});
});
