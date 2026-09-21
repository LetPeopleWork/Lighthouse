import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { TeamColour } from "./deliveryTeamLanes";
import TimelineTeamLegend from "./TimelineTeamLegend";

// The key to the colours, on its own. It is the only place a colour can be read back to a Team at
// full width, because a lane is only as wide as its Team's span and the names written along the
// lanes are routinely cut to a few characters.

const ZENITH: TeamColour = {
	teamId: 5,
	teamName: "Zenith",
	color: "#4DA98C",
};
const GRAVITY: TeamColour = {
	teamId: 6,
	teamName: "Gravity",
	color: "#6BA3F5",
};

const renderLegend = (teams: TeamColour[]) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineTeamLegend teams={teams} />
		</ThemeProvider>,
	);

const swatchesOf = (legend: HTMLElement) =>
	Array.from(legend.querySelectorAll("[aria-hidden='true']"));

describe("the key to the Teams' colours", () => {
	it("names every Team it is given, in the order it is given them", () => {
		renderLegend([GRAVITY, ZENITH]);

		// The order is the lanes' own reading order, decided upstream, so the key and the chart
		// can be read against each other rather than each in its own sequence.
		expect(screen.getByTestId("timeline-team-legend")).toHaveTextContent(
			/Gravity.*Zenith/,
		);
	});

	it("gives each Team a patch of its own colour", () => {
		const legend = renderLegend([ZENITH, GRAVITY]).container;

		const swatches = swatchesOf(legend);

		expect(swatches).toHaveLength(2);
		// Asserted as a difference between the two rather than against a colour value: this
		// environment does not resolve the styles it injects, so a computed fill answers
		// transparent for a painted patch and an unpainted one alike. Two patches given different
		// colours are styled differently and get different classes; two given none share one.
		expect(new Set(swatches.map((patch) => patch.className)).size).toBe(2);
	});

	it("hides the patches from anything reading the page aloud", () => {
		// The Team's name is beside each one and carries the whole meaning. Read out, the patches
		// would be two more things between a reader and the names.
		const legend = renderLegend([ZENITH, GRAVITY]).container;

		expect(swatchesOf(legend)).toHaveLength(2);
	});

	it("lists both Teams when this Portfolio can name neither of them", () => {
		// Every Team a Portfolio cannot name is given the same phrase, so the names here are not
		// distinct. Keyed on the name rather than the Team, one of the two disappears - and the
		// key then shows fewer Teams than the chart has colours for.
		const outsider = "A Team from outside this Portfolio";

		renderLegend([
			{ teamId: 404, teamName: outsider, color: "#4DA98C" },
			{ teamId: 405, teamName: outsider, color: "#6BA3F5" },
		]);

		expect(
			within(screen.getByTestId("timeline-team-legend")).getAllByText(outsider),
		).toHaveLength(2);
	});

	it("draws nothing at all when there is no colour to explain", () => {
		renderLegend([]);

		expect(screen.getByTestId("timeline-team-legend")).toBeEmptyDOMElement();
	});
});
