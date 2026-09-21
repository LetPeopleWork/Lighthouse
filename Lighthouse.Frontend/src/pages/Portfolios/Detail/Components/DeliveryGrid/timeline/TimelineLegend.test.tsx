import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import TimelineLegend, { type LegendEntry } from "./TimelineLegend";

// The key, on its own. It is the only place a colour can be read back to what it stands for at full
// width: a lane is only as wide as its Team's span and the names written along the lanes are
// routinely cut to a few characters, and a cap at the end of a bar carries no name at all.

const aTeam = (overrides: Partial<LegendEntry> = {}): LegendEntry => ({
	id: "team:5",
	label: "Zenith",
	color: "#4DA98C",
	...overrides,
});

const GRAVITY = aTeam({ id: "team:6", label: "Gravity", color: "#6BA3F5" });

const renderLegend = (entries: LegendEntry[], testId?: string) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineLegend entries={entries} testId={testId} />
		</ThemeProvider>,
	);

const swatchesOf = (legend: HTMLElement) =>
	Array.from(legend.querySelectorAll("[aria-hidden='true']"));

describe("the key to the chart's colours", () => {
	it("names every entry it is given, in the order it is given them", () => {
		renderLegend([GRAVITY, aTeam()]);

		// The order is the chart's own reading order, decided upstream, so the key and the chart
		// can be read against each other rather than each in its own sequence.
		expect(screen.getByTestId("timeline-legend")).toHaveTextContent(
			/Gravity.*Zenith/,
		);
	});

	it("gives each entry a patch of its own colour", () => {
		const legend = renderLegend([aTeam(), GRAVITY]).container;

		const swatches = swatchesOf(legend);

		expect(swatches).toHaveLength(2);
		// Asserted as a difference between the two rather than against a colour value: this
		// environment does not resolve the styles it injects, so a computed fill answers
		// transparent for a painted patch and an unpainted one alike. Two patches given different
		// colours are styled differently and get different classes; two given none share one.
		expect(new Set(swatches.map((patch) => patch.className)).size).toBe(2);
	});

	it("hides the patches from anything reading the page aloud", () => {
		// The label beside each one carries the whole meaning. Read out, the patches would be two
		// more things between a reader and the names.
		const legend = renderLegend([aTeam(), GRAVITY]).container;

		expect(swatchesOf(legend)).toHaveLength(2);
	});

	it("lists both entries when two of them carry the same words", () => {
		// Every Team a Portfolio cannot name is given the same phrase, so two labels here need not
		// be distinct. Keyed on the label rather than on the entry, one of the two disappears - and
		// the key then shows fewer Teams than the chart has colours for.
		const outsider = "A Team from outside this Portfolio";

		renderLegend([
			aTeam({ id: "team:404", label: outsider }),
			aTeam({ id: "team:405", label: outsider, color: "#6BA3F5" }),
		]);

		expect(
			within(screen.getByTestId("timeline-legend")).getAllByText(outsider),
		).toHaveLength(2);
	});

	it("draws nothing at all when there is nothing to explain", () => {
		renderLegend([]);

		expect(screen.getByTestId("timeline-legend")).toBeEmptyDOMElement();
	});

	it("answers to the name the caller gives it", () => {
		// Two keys can be on screen at once, and a reader looking for one of them has to be able
		// to say which.
		renderLegend([aTeam()], "timeline-team-legend");

		expect(screen.getByTestId("timeline-team-legend")).toBeInTheDocument();
	});
});
