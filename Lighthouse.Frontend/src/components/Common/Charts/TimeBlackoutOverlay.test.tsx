import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import { testTheme } from "../../../tests/testTheme";
import TimeBlackoutOverlay from "./TimeBlackoutOverlay";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Domain: Mar 15 00:00 UTC .. Mar 22 00:00 UTC
const domainMin = new Date("2026-03-15T00:00:00Z").getTime();
const domainMax = new Date("2026-03-22T00:00:00Z").getTime();
const domainRange = domainMax - domainMin;
const rangeStart = 50;
const rangeEnd = 750;
const pixelRange = rangeEnd - rangeStart;

const mockTimeScale = Object.assign(
	(ts: number) => {
		if (ts < domainMin || ts > domainMax) return undefined;
		return rangeStart + ((ts - domainMin) / domainRange) * pixelRange;
	},
	{
		domain: () => [domainMin, domainMax] as [number, number],
		range: () => [rangeStart, rangeEnd] as [number, number],
	},
);

vi.mock("@mui/x-charts/hooks", () => ({
	useDrawingArea: () => ({ top: 20, height: 400 }),
	useXScale: () => mockTimeScale,
}));

const createBlackoutPeriod = (
	overrides: Partial<IBlackoutPeriod> = {},
): IBlackoutPeriod => ({
	id: 1,
	start: "2026-03-17",
	end: "2026-03-18",
	description: "St. Patrick's Day",
	...overrides,
});

describe("TimeBlackoutOverlay", () => {
	it("should render nothing when blackoutPeriods is empty", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={[]} />
			</svg>,
		);

		expect(
			container.querySelector('[data-testid="blackout-overlay"]'),
		).toBeNull();
	});

	it("should render overlay when blackout periods are within the domain", () => {
		render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={[createBlackoutPeriod()]} />
			</svg>,
		);

		expect(screen.getByTestId("blackout-overlay")).toBeInTheDocument();
	});

	it("should render one band per day in the blackout period", () => {
		// Period: Mar 17 - Mar 18 = 2 days
		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={[createBlackoutPeriod()]} />
			</svg>,
		);

		const overlay = container.querySelector('[data-testid="blackout-overlay"]');
		if (!overlay) {
			throw new Error("Overlay should not be null");
		}
		// 2 days × 2 rects each (background + hatch) = 4
		const rects = overlay.querySelectorAll("rect");
		expect(rects.length).toBe(4);
	});

	it("should position bands based on time scale", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={[createBlackoutPeriod()]} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);

		// Mar 17 00:00 UTC → pixel position
		const mar17Start = new Date("2026-03-17T00:00:00Z").getTime();
		const mar17End = mar17Start + 24 * 60 * 60 * 1000;
		const expectedX1 =
			rangeStart + ((mar17Start - domainMin) / domainRange) * pixelRange;
		const expectedX2 =
			rangeStart + ((mar17End - domainMin) / domainRange) * pixelRange;

		const xAttribute = rects[0].getAttribute("x");
		const widthAttribute = rects[0].getAttribute("width");

		if (xAttribute === null || widthAttribute === null) {
			throw new Error("Rect attributes should not be null");
		}

		const x = Number.parseFloat(xAttribute);
		const width = Number.parseFloat(widthAttribute);
		expect(x).toBeCloseTo(expectedX1, 0);
		expect(width).toBeCloseTo(expectedX2 - expectedX1, 0);
		expect(rects[0].getAttribute("y")).toBe("20");
		expect(rects[0].getAttribute("height")).toBe("400");
	});

	it("should render the hatch pattern definition", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={[createBlackoutPeriod()]} />
			</svg>,
		);

		const pattern = container.querySelector("#time-blackout-hatch");
		expect(pattern).toBeInTheDocument();
		expect(pattern?.getAttribute("patternTransform")).toBe("rotate(45)");
	});

	it("should skip periods entirely outside the domain", () => {
		const outsidePeriod = createBlackoutPeriod({
			start: "2025-01-01",
			end: "2025-01-02",
		});

		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={[outsidePeriod]} />
			</svg>,
		);

		expect(
			container.querySelector('[data-testid="blackout-overlay"]'),
		).toBeNull();
	});

	it("should handle multiple separate blackout periods", () => {
		const periods: IBlackoutPeriod[] = [
			createBlackoutPeriod({
				id: 1,
				start: "2026-03-16",
				end: "2026-03-16",
			}),
			createBlackoutPeriod({
				id: 2,
				start: "2026-03-19",
				end: "2026-03-19",
			}),
		];

		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay blackoutPeriods={periods} />
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		// 2 single-day periods × 2 rects each = 4
		expect(rects.length).toBe(4);
	});

	it("should apply background and hatch fill to rect pairs", () => {
		const { container } = render(
			<svg>
				<title>Test</title>
				<TimeBlackoutOverlay
					blackoutPeriods={[
						createBlackoutPeriod({
							start: "2026-03-17",
							end: "2026-03-17",
						}),
					]}
				/>
			</svg>,
		);

		const rects = container.querySelectorAll(
			'[data-testid="blackout-overlay"] rect',
		);
		expect(rects[0].getAttribute("fill-opacity")).toBe("0.08");
		expect(rects[1].getAttribute("fill")).toBe("url(#time-blackout-hatch)");
	});
});
