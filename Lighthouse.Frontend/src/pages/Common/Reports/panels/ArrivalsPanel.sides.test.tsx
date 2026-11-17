import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IReportMetric } from "../../../../models/Reports/Report";
import { aDash, shown } from "../../../../tests/ReportsTestKit";
import ArrivalsPanel from "./ArrivalsPanel";
import ThenNowPanelFrame from "./ThenNowPanelFrame";

const segment = (name: "then" | "now" | "change") =>
	document.querySelector(`[data-segment="${name}"]`) as HTMLElement;

const reading = (element: HTMLElement) =>
	(element.textContent ?? "").replaceAll(/\s+/g, " ").trim();

const NOT_REPORTED = { value: null, sampleSize: null, absentReason: null };

const renderPanel = (metric: IReportMetric) =>
	render(
		<ArrivalsPanel
			title="Arrivals"
			metric={metric}
			workItem="Work Item"
			workItems="Work Items"
			directionOfGood="lower"
		/>,
	);

describe("the sides of the Arrivals panel when values are missing", () => {
	it("reads a dash for a missing total and for a missing gap, never the word null or a sentence for a gap of zero, and explains the dashes with the reason the server gave", () => {
		renderPanel({
			atThen: {
				total: NOT_REPORTED,
				perDay: shown(1.1, 90),
				throughputPerDay: shown(0.8, 90),
				gapPerDay: aDash("no-finished-items"),
			},
			atNow: {
				total: shown(81, 90),
				perDay: shown(0.9, 90),
				throughputPerDay: shown(1, 90),
				gapPerDay: shown(-0.1, 90),
			},
		});

		const then = reading(segment("then"));

		expect(then).toMatch(/^Then Total started — Arrivals 1\.10 per day/);
		expect(then).toMatch(/Throughput 0\.80 per day —/);
		expect(then).not.toMatch(/null|as many started/);
		expect(then).not.toContain("Not captured for this report");
		expect(then).toMatch(/finished/);
	});

	it("has three empty rows above the change so the mark sits level with the gap sentence", () => {
		renderPanel({
			atThen: {
				total: shown(99, 90),
				perDay: shown(1.1, 90),
				throughputPerDay: shown(0.8, 90),
				gapPerDay: shown(0.3, 90),
			},
			atNow: {
				total: shown(81, 90),
				perDay: shown(0.9, 90),
				throughputPerDay: shown(1, 90),
				gapPerDay: shown(-0.1, 90),
			},
		});

		const rowsInChange = segment("change").querySelectorAll(
			":scope > .MuiBox-root",
		);
		const rowsInThen = segment("then").querySelectorAll(
			":scope > .MuiBox-root",
		);

		expect(rowsInChange.length).toBe(4);
		expect(rowsInThen.length).toBe(4);
		expect(reading(rowsInChange[0] as HTMLElement)).toBe("");
	});

	it("names its info button About Arrivals", () => {
		renderPanel({
			atThen: { gapPerDay: shown(0.3, 90) },
			atNow: { gapPerDay: shown(0.1, 90) },
		});

		expect(
			screen.getByRole("button", { name: "About Arrivals" }),
		).toBeInTheDocument();
	});

	it("writes the gap sentence in bold", () => {
		renderPanel({
			atThen: { gapPerDay: shown(0.3, 90) },
			atNow: { gapPerDay: shown(0.1, 90) },
		});

		expect(
			getComputedStyle(screen.getAllByText(/more started than finished/)[0])
				.fontWeight,
		).toBe("700");
	});
});

describe("the frame of a Then & Now panel", () => {
	it("keeps the title's gap below it when there is no info control, and hands that gap to the row when there is one", () => {
		const { rerender } = render(
			<ThenNowPanelFrame title="Plain" then="a" now="b" change="c" />,
		);
		const plain = screen.getByRole("heading", { name: "Plain" });
		const plainMargin = getComputedStyle(plain).marginBottom;
		expect(plain.parentElement?.tagName).toBe("SECTION");

		rerender(
			<ThenNowPanelFrame
				title="With info"
				then="a"
				now="b"
				change="c"
				info={<button type="button">About</button>}
			/>,
		);
		const withInfo = screen.getByRole("heading", { name: "With info" });
		const row = withInfo.parentElement as HTMLElement;

		expect(plainMargin).toBe("7px");
		expect(getComputedStyle(withInfo).marginBottom).toBe("0px");
		expect(getComputedStyle(row).display).toBe("flex");
		expect(getComputedStyle(row).alignItems).toBe("center");
		expect(getComputedStyle(row).marginBottom).toBe("5.6px");
		expect(getComputedStyle(row).gap).toBe("4px");
	});
});
