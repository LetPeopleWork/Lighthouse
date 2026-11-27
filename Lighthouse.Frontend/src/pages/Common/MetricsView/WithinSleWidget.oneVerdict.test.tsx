import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IWithinSleInfo } from "../../../models/Metrics/WithinSleInfo";
import WithinSleWidget from "./WithinSleWidget";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
	}),
}));

describe("WithinSleWidget shows one verdict at a time", () => {
	it("shows no share when there is no SLE", () => {
		const noSle = {
			verdict: "NoSle",
			closedCount: 4,
			withinCount: 0,
		} as IWithinSleInfo;

		render(<WithinSleWidget info={noSle} />);

		expect(screen.queryByTestId("within-sle-share")).not.toBeInTheDocument();
	});

	it("says nothing about a missing SLE when the share is measured", () => {
		const measured = {
			verdict: "Measured",
			closedCount: 10,
			withinCount: 8,
			sharePercent: 80,
			slePercentile: 70,
			sleDays: 12,
		} as IWithinSleInfo;

		render(<WithinSleWidget info={measured} />);

		expect(screen.getByTestId("within-sle-share")).toBeInTheDocument();
		expect(
			screen.queryByText(/define one in settings/),
		).not.toBeInTheDocument();
	});
});
