import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { outlierWording } from "../../../utils/charts/outlierWording";
import OutlierChip from "./OutlierChip";
import OutlierPopoverContent from "./OutlierPopoverContent";
import { buildOutlierRows } from "./outlierPlot";

const wording = outlierWording({
	workItems: "Work Items",
	sle: "SLE",
	cycleTime: "Cycle Time",
});

const row = {
	key: "1-A-1",
	referenceId: "A-1",
	name: "Long one",
	closedDate: "2 Mar 2026",
	days: 40,
	timesShort: "6.7x",
	timesSpoken: "6.7 times the median",
};

const chip = (over: Partial<Parameters<typeof OutlierChip>[0]> = {}) => (
	<OutlierChip
		wording={wording}
		count={2}
		cut={30}
		median={6}
		rows={[row]}
		moreCount={0}
		aboveTheAxis={null}
		disabled={false}
		onOpened={() => {}}
		onOpenInList={() => {}}
		chipRef={null}
		{...over}
	/>
);

const phone = (matches: boolean) =>
	vi.stubGlobal("matchMedia", (query: string) => ({
		matches,
		media: query,
		addEventListener: () => {},
		removeEventListener: () => {},
		addListener: () => {},
		removeListener: () => {},
		onchange: null,
		dispatchEvent: () => false,
	}));

describe("OutlierChip", () => {
	it("shows only the count on a phone, but the words while disabled", () => {
		phone(true);
		const { rerender } = render(chip());
		expect(screen.getByTestId("outlier-chip").textContent).toContain("2");
		expect(screen.getByTestId("outlier-chip").textContent).not.toContain(
			"outliers",
		);
		rerender(chip({ disabled: true }));
		expect(screen.getByTestId("outlier-chip").textContent).toBe("Outliers");
		vi.unstubAllGlobals();
	});

	it("shows the words on a wide screen", () => {
		phone(false);
		render(chip());
		expect(screen.getByTestId("outlier-chip").textContent).toBe("2 outliers");
		vi.unstubAllGlobals();
	});

	it("reports an opening once and closes the popover when it becomes disabled", () => {
		phone(false);
		const onOpened = vi.fn();
		const { rerender } = render(chip({ onOpened }));
		fireEvent.click(screen.getByTestId("outlier-chip"));
		expect(onOpened).toHaveBeenCalledTimes(1);
		expect(screen.getByText("2 outliers", { selector: "h3" })).toBeTruthy();

		fireEvent.click(screen.getByTestId("outlier-chip"));
		expect(onOpened).toHaveBeenCalledTimes(1);

		rerender(chip({ onOpened, disabled: true }));
		expect(screen.queryByText("2 outliers", { selector: "h3" })).toBeNull();
		rerender(chip({ onOpened, disabled: false }));
		expect(screen.queryByText("2 outliers", { selector: "h3" })).toBeNull();
		vi.unstubAllGlobals();
	});

	it("gives the loading chip no tooltip and the ready chip its accessible name", async () => {
		phone(false);
		const { rerender } = render(chip({ disabled: true }));
		fireEvent.mouseOver(screen.getByTestId("outlier-chip"));
		expect(screen.queryByRole("tooltip")).toBeNull();
		rerender(chip());
		fireEvent.mouseOver(screen.getByTestId("outlier-chip"));
		expect((await screen.findByRole("tooltip")).textContent).toBe(
			"2 outliers not drawn on the chart. Show details.",
		);
		vi.unstubAllGlobals();
	});
});

describe("OutlierPopoverContent", () => {
	const content = (
		over: Partial<Parameters<typeof OutlierPopoverContent>[0]> = {},
	) => (
		<OutlierPopoverContent
			wording={wording}
			count={2}
			cut={30}
			median={6}
			rows={[row]}
			moreCount={0}
			aboveTheAxis={null}
			onOpenInList={() => {}}
			{...over}
		/>
	);

	it("names the lines above the axis only when there are some", () => {
		const { rerender } = render(content());
		expect(screen.queryByText(/Above the axis/)).toBeNull();
		rerender(content({ aboveTheAxis: "Above the axis: 95% at 33 days." }));
		expect(screen.getByText("Above the axis: 95% at 33 days.")).toBeTruthy();
	});

	it("mentions further rows only when more than none are left out", () => {
		const { rerender } = render(content());
		expect(screen.queryByText(/more$/)).toBeNull();
		rerender(content({ moreCount: 1 }));
		expect(screen.getByText("and 1 more")).toBeTruthy();
	});

	it("hides the spoken form visually without removing it", () => {
		render(content());
		const spoken = getComputedStyle(screen.getByText("6.7 times the median"));
		expect(spoken.position).toBe("absolute");
		expect(spoken.overflow).toBe("hidden");
		expect(spoken.whiteSpace).toBe("nowrap");
		expect(spoken.clip).toContain("rect(0");
		expect(spoken.margin).toBe("-8px");
		expect(buildOutlierRows([], 1)).toEqual([]);
	});
});
