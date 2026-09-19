import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "../../../utils/charts/paceBands";
import SleRiskWidget from "./SleRiskWidget";

const count = () => screen.getByTestId("sle-risk-count");

describe("SleRiskWidget", () => {
	it("shows how much of the board is at risk", () => {
		render(<SleRiskWidget atRisk={{ count: 3, color: "#c62828" }} />);

		expect(count()).toHaveTextContent("3");
	});

	it("shows a zero when nothing is at risk", () => {
		// Distinct from having no target: zero here is a measurement, and a good one.
		render(<SleRiskWidget atRisk={{ count: 0 }} />);

		expect(count()).toHaveTextContent("0");
	});

	it("shows a dash rather than a number for a team that published no target", () => {
		// A zero would read as good news. There is nothing to measure until a target exists, and
		// the widget's status is what explains why.
		//
		// The dash is asserted rather than just the absence of a zero: an empty cell would also
		// satisfy "not a zero", and an empty cell is a widget that looks broken.
		render(<SleRiskWidget />);

		expect(count()).toHaveTextContent("—");
	});

	it("takes the colour of the worst item it counted", () => {
		const worstBand =
			PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1];

		render(<SleRiskWidget atRisk={{ count: 2, color: worstBand }} />);

		expect(count()).toHaveStyle(`color: ${worstBand}`);
	});

	it("names itself, so nothing has to find it by position", () => {
		render(<SleRiskWidget atRisk={{ count: 1, color: "#c62828" }} />);

		expect(screen.getByText("At Risk")).toBeInTheDocument();
	});
});
