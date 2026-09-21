import { createTheme } from "@mui/material";
import { describe, expect, it } from "vitest";
import { appColors } from "../../../../../../utils/theme/colors";
import {
	markerColors,
	markerLabels,
	STATUS_CAP_COLORS,
} from "./timelineMarkers";

describe("what hovering a marked column says", () => {
	it("names the day the column is drawn on, not the day the instant lands on", () => {
		// The target is stored late in the UTC day. These tests run in a zone ahead of UTC, so
		// reading it raw names tomorrow - and the label then sat on a column drawn for today,
		// under a heading that also said today. Two of the three agreed and the one a reader
		// hovers did not.
		const storedLateInTheUtcDay = new Date(Date.UTC(2026, 9, 20, 22, 30));

		const asTheHeadingPrintsIt = storedLateInTheUtcDay.toLocaleDateString(
			undefined,
			{ timeZone: "UTC" },
		);

		expect(markerLabels(storedLateInTheUtcDay, new Date()).target).toContain(
			asTheHeadingPrintsIt,
		);
		// The fixture proving itself: without the reduction the label carries the other day, so
		// this pair cannot both hold against the mistake.
		expect(storedLateInTheUtcDay.toLocaleDateString()).not.toBe(
			asTheHeadingPrintsIt,
		);
	});

	it("says the target is not set, rather than saying a date it does not have", () => {
		// A Delivery with no date still draws a today column, and the label builder is asked for
		// both. Anything invented here would be printed on a column marked for a date nobody set.
		expect(markerLabels(undefined, new Date()).target).toBe("Target date · ");
	});

	it("names today in the reader's own day, which is the only day it has", () => {
		const thisAfternoon = new Date(2026, 9, 20, 15, 0);

		expect(markerLabels(undefined, thisAfternoon).today).toContain(
			thisAfternoon.toLocaleDateString(),
		);
	});

	it("says what each mark is, not only when it is", () => {
		// The words are the whole of what a hovered column tells a reader who has not been told
		// what a tinted column means.
		const labels = markerLabels(new Date(Date.UTC(2026, 9, 20)), new Date());

		expect(labels.target).toContain("Target date");
		expect(labels.today).toContain("Today");
	});
});

describe("the colours a cap can be", () => {
	it("gives each of the three a colour of its own", () => {
		// Asserted as three mutual differences rather than against values, so it cannot be
		// satisfied by reading the constant back out of the thing under test.
		const { finished, startsAfterTarget, endsAfterTarget } = STATUS_CAP_COLORS;

		expect(finished).not.toBe(startsAfterTarget);
		expect(finished).not.toBe(endsAfterTarget);
		expect(startsAfterTarget).not.toBe(endsAfterTarget);
	});

	it("takes them from the palette this product already reads forecasts in", () => {
		// Pinned to the literal values rather than to `appColors.forecast.*`, because comparing
		// these against the constants they are built from is the same reduction on both sides and
		// could not fail. A re-theme that changed what a colour means on this chart would be
		// invisible without this.
		expect(STATUS_CAP_COLORS.finished).toBe("#388e3c");
		expect(STATUS_CAP_COLORS.startsAfterTarget).toBe("#f44336");
		expect(STATUS_CAP_COLORS.endsAfterTarget).toBe("#ff9800");
	});

	it("shares the target column's colour, on purpose", () => {
		// This is a decision, not an oversight, and it is written as a test so that whoever
		// notices the clash and goes to fix one of the two is stopped by something rather than by
		// nobody. The bar's late cap and the column it crosses are the same amber; they are meant
		// to be told apart by shape and position, and by the key that names the cap.
		//
		// If looking at a real chart says they cannot be told apart, the answer is to move the
		// *column* to a colour nothing else uses and leave the caps alone - not to invent a second
		// amber here, which would leave the chart with two colours that both mean late.
		expect(STATUS_CAP_COLORS.endsAfterTarget).toBe(
			markerColors(
				createTheme({
					palette: { warning: { main: appColors.status.warning } },
				}),
			).target,
		);
	});
});
