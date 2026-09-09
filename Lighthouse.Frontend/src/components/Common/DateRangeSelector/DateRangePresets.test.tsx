import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { DateWindowPreset } from "../../../pages/Common/MetricsView/dateWindow";
import DateRangePresets from "./DateRangePresets";

const teamPresets: readonly DateWindowPreset[] = [
	{ label: "Last 7 days", days: 7 },
	{ label: "Last 14 days", days: 14 },
	{ label: "Last 30 days", days: 30 },
	{ label: "Last 90 days", days: 90 },
];

const portfolioPresets: readonly DateWindowPreset[] = [
	{ label: "Last 30 days", days: 30 },
	{ label: "Last 90 days", days: 90 },
	{ label: "Last 180 days", days: 180 },
];

describe("DateRangePresets", () => {
	it("shows nothing at all when there are no windows to offer", () => {
		const { container } = render(
			<DateRangePresets
				presets={[]}
				selectedDays={null}
				onSelectPreset={vi.fn()}
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});

	// aria-pressed carries the choice to a screen reader; these carry it to everyone else. Without
	// them the chosen window could render exactly like the three it was chosen over.
	it("fills and colours the chosen window, and leaves the rest outlined", () => {
		render(
			<DateRangePresets
				presets={teamPresets}
				selectedDays={30}
				onSelectPreset={vi.fn()}
			/>,
		);

		const chipFor = (label: string) =>
			screen.getByText(label).closest("[role='button']") as HTMLElement;

		expect(chipFor("Last 30 days").className).toContain("MuiChip-filled");
		expect(chipFor("Last 30 days").className).toContain("MuiChip-colorPrimary");
		expect(chipFor("Last 7 days").className).toContain("MuiChip-outlined");
		expect(chipFor("Last 7 days").className).not.toContain("colorPrimary");
	});

	it("offers a team every window it was given, in the order it was given them", () => {
		render(
			<DateRangePresets
				presets={teamPresets}
				selectedDays={30}
				onSelectPreset={vi.fn()}
			/>,
		);

		expect(screen.getAllByRole("button").map((b) => b.textContent)).toEqual([
			"Last 7 days",
			"Last 14 days",
			"Last 30 days",
			"Last 90 days",
		]);
	});

	it("offers a portfolio its own three windows", () => {
		render(
			<DateRangePresets
				presets={portfolioPresets}
				selectedDays={90}
				onSelectPreset={vi.fn()}
			/>,
		);

		expect(screen.getAllByRole("button")).toHaveLength(3);
		expect(screen.getByText("Last 180 days")).toBeInTheDocument();
	});

	it("reports the window the reader chose", () => {
		const onSelectPreset = vi.fn();
		render(
			<DateRangePresets
				presets={teamPresets}
				selectedDays={30}
				onSelectPreset={onSelectPreset}
			/>,
		);

		fireEvent.click(screen.getByText("Last 90 days"));

		expect(onSelectPreset).toHaveBeenCalledWith(90);
	});

	it("shows which window the reader is currently looking at", () => {
		render(
			<DateRangePresets
				presets={teamPresets}
				selectedDays={30}
				onSelectPreset={vi.fn()}
			/>,
		);

		expect(
			screen.getByText("Last 30 days").closest("[role='button']"),
		).toHaveAttribute("aria-pressed", "true");
		expect(
			screen.getByText("Last 7 days").closest("[role='button']"),
		).toHaveAttribute("aria-pressed", "false");
	});

	it("claims no window at all once the dates were picked by hand", () => {
		// A hand-picked window is nobody's "last N days". A chip left looking selected here would
		// tell the reader they are looking at something they are not.
		render(
			<DateRangePresets
				presets={teamPresets}
				selectedDays={null}
				onSelectPreset={vi.fn()}
			/>,
		);

		for (const chip of screen.getAllByRole("button")) {
			expect(chip).toHaveAttribute("aria-pressed", "false");
		}
	});
});
