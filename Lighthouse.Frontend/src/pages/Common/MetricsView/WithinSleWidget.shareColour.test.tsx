import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IWithinSleInfo } from "../../../models/Metrics/WithinSleInfo";
import WithinSleWidget from "./WithinSleWidget";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
	}),
}));

const measured = (sharePercent: number, slePercentile: number) =>
	({
		verdict: "Measured",
		closedCount: 10,
		withinCount: 8,
		sharePercent,
		slePercentile,
		sleDays: 12,
	}) as IWithinSleInfo;

describe("WithinSleWidget colours the share by its status", () => {
	it.each([
		["at the target (Sustain)", 70, 70, "green"],
		["above the target (Sustain)", 80, 70, "green"],
		["just below the target (Observe)", 69, 70, "amber"],
		["exactly 20 points below (Observe)", 50, 70, "amber"],
		["more than 20 points below (Act)", 49, 70, "red"],
	])("marks a share %s", (_name, share, target, expected) => {
		render(<WithinSleWidget info={measured(share, target)} />);

		expect(screen.getByTestId("within-sle-share")).toHaveAttribute(
			"data-rag",
			expected,
		);
	});

	it("gives a share that is too few to judge no status", () => {
		const tooFew = {
			verdict: "TooFew",
			closedCount: 2,
			withinCount: 1,
			slePercentile: 70,
			sleDays: 12,
		} as IWithinSleInfo;

		render(<WithinSleWidget info={tooFew} />);

		expect(screen.queryByTestId("within-sle-share")).not.toBeInTheDocument();
		expect(document.querySelector("[data-rag]")).toBeNull();
	});

	it("gives a missing SLE no status", () => {
		const noSle = {
			verdict: "NoSle",
			closedCount: 4,
			withinCount: 0,
		} as IWithinSleInfo;

		render(<WithinSleWidget info={noSle} />);

		expect(document.querySelector("[data-rag]")).toBeNull();
	});
});
