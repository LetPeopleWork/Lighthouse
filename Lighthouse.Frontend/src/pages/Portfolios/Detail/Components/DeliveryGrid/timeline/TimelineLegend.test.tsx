import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import TimelineLegend from "./TimelineLegend";

const renderLegend = (targetDate?: Date) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineLegend targetDate={targetDate} today={new Date(2026, 9, 12)} />
		</ThemeProvider>,
	);

describe("TimelineLegend", () => {
	it("names both marked days and says which dates they are", () => {
		// The shaded columns are the problem this solves: a reader can see that a day is special
		// and has no way to learn which special thing it is. Naming them is the whole job, and
		// carrying the dates means a target scrolled out of view is still readable.
		renderLegend(new Date(2026, 9, 25));

		const legend = screen.getByTestId("timeline-legend");

		expect(legend).toHaveTextContent("Today");
		expect(legend).toHaveTextContent("10/12/2026");
		expect(legend).toHaveTextContent("Target date");
		expect(legend).toHaveTextContent("10/25/2026");
	});

	it("draws its two swatches differently, the way the chart draws its two columns", () => {
		renderLegend(new Date(2026, 9, 25));

		const ruled = screen.getByTestId("legend-swatch-ruled");
		const filled = screen.getByTestId("legend-swatch-filled");

		// A legend whose two entries look alike explains nothing — the reader still cannot tell
		// which column on the chart is which. Asserted as a difference rather than as two literal
		// colours, because the rule is that they must not match; pinning the values would also
		// break on any palette change without anything actually being wrong.
		const styleOf = (element: HTMLElement) => getComputedStyle(element);

		expect(styleOf(filled).backgroundColor).not.toBe(
			styleOf(ruled).backgroundColor,
		);
		expect(styleOf(ruled).borderLeftWidth).not.toBe(
			styleOf(filled).borderLeftWidth,
		);
	});

	it("says nothing about a target the Delivery does not have", () => {
		renderLegend(undefined);

		const legend = screen.getByTestId("timeline-legend");

		expect(legend).toHaveTextContent("Today");
		expect(legend).not.toHaveTextContent("Target date");
	});
});
