import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import DeliveryGanttChart from "./DeliveryGanttChart";
import type { TimelineBar } from "./deliveryTimelineModel";

// The chart paints its background grid onto a canvas, and this test environment has no 2D canvas —
// `getContext` answers null and the library dereferences it. Nothing in our own wiring is involved,
// so the drawing surface is stubbed rather than the test abandoned: everything up to the paint is
// then genuinely exercised. The painted grid itself is only ever seen in a browser.
beforeAll(() => {
	HTMLCanvasElement.prototype.getContext = vi.fn(
		() =>
			({
				translate: vi.fn(),
				beginPath: vi.fn(),
				moveTo: vi.fn(),
				lineTo: vi.fn(),
				stroke: vi.fn(),
				clearRect: vi.fn(),
				setTransform: vi.fn(),
				scale: vi.fn(),
			}) as unknown as CanvasRenderingContext2D,
	) as unknown as HTMLCanvasElement["getContext"];

	// The grid is then handed to CSS as a data URL. Left alone this prints twelve lines of
	// "not implemented" per run, which is the kind of noise a real warning hides in later.
	HTMLCanvasElement.prototype.toDataURL = vi.fn(() => "data:,");
});

/**
 * The only tests that render the real library rather than standing it in — which is why this file
 * holds nothing else. Every decision the adapter makes is a pure function tested next door in
 * `ganttShapes.test.ts`, without a canvas stub and without loading the library at all.
 *
 * What is left here is the one thing those cannot cover: whether our wiring mounts. That is a real
 * failure mode with this library — one of the themes it ships crashes its free build outright, and
 * importing the wrong stylesheet leaves it reporting success while rendering nothing. So these
 * assert that it mounted and which theme was chosen, and stop short of their DOM.
 */
const bar = (id: number, name: string): TimelineBar => ({
	featureId: id,
	name,
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
	endIsObserved: false,
});

const BRAND_GREEN = "#30574e";

const renderInTheme = (mode: "light" | "dark") =>
	render(
		<ThemeProvider
			theme={createTheme({
				palette: { mode, primary: { main: BRAND_GREEN } },
			})}
		>
			<DeliveryGanttChart
				bars={[bar(1, "Deep Sea Mapping"), bar(2, "Sonar Refit")]}
				targetDate={new Date(2026, 9, 25)}
				today={new Date(2026, 9, 12)}
			/>
		</ThemeProvider>,
	);

describe("DeliveryGanttChart", () => {
	it.each(["light", "dark"] as const)("mounts in %s mode", (mode) => {
		renderInTheme(mode);

		expect(screen.getByTestId("delivery-gantt")).toHaveAttribute(
			"data-theme-mode",
			mode,
		);
	});

	it("mounts with nothing to draw", () => {
		render(
			<ThemeProvider theme={createTheme()}>
				<DeliveryGanttChart bars={[]} today={new Date(2026, 9, 12)} />
			</ThemeProvider>,
		);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
	});
});
