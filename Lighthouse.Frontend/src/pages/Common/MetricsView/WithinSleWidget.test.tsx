import { render, screen } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWithinSleInfo } from "../../../models/Metrics/WithinSleInfo";

const theWordsOnThePage = vi.hoisted(() => ({
	terms: {} as Record<string, string>,
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => theWordsOnThePage.terms[key] ?? key,
	}),
}));

const productDefaults = {
	workItem: "Work Item",
	workItems: "Work Items",
	sle: "SLE",
	cycleTime: "Cycle Time",
};

const elevenClosedEightWithinTenDays: IWithinSleInfo = {
	verdict: "Measured",
	closedCount: 11,
	withinCount: 8,
	slePercentile: 70,
	sleDays: 10,
	sharePercent: 72.7,
	comparison: { direction: "none", metricLabel: "Within SLE", detailRows: [] },
};

// The widget is imported while the test runs, so a widget that is not built yet fails the
// test on its own words, not the whole file on a missing module.
async function renderTheWidget(info: IWithinSleInfo) {
	const widgetFiles = import.meta.glob("./WithinSleWidget.tsx");
	const load = widgetFiles["./WithinSleWidget.tsx"];
	expect(
		load,
		"The Within SLE widget is missing: the Flow Overview needs a WithinSleWidget next to the other widgets that takes the server's answer as `info`.",
	).toBeDefined();
	const module = (await load()) as {
		default: React.ComponentType<{ readonly info: IWithinSleInfo }>;
	};
	const WithinSleWidget = module.default;
	return render(<WithinSleWidget info={info} />);
}

describe("WithinSleWidget", () => {
	beforeEach(() => {
		theWordsOnThePage.terms = { ...productDefaults };
	});

	it("shows the share within the SLE with its counts, the sentence and the target, with no licence needed", async () => {
		await renderTheWidget(elevenClosedEightWithinTenDays);

		expect(screen.getByText("72.7 % (8 / 11)")).toBeInTheDocument();
		expect(
			screen.getByText("of Work Items closed within 10 days or less"),
		).toBeInTheDocument();
		expect(screen.getByText("Target 70 %")).toBeInTheDocument();
	});

	it("names the Work Items in the words the reader configured", async () => {
		theWordsOnThePage.terms = { ...productDefaults, workItems: "Tickets" };

		await renderTheWidget(elevenClosedEightWithinTenDays);

		expect(
			screen.getByText("of Tickets closed within 10 days or less"),
		).toBeInTheDocument();
		expect(screen.queryByText(/Work Items/)).not.toBeInTheDocument();
	});

	it("shows the share the server answered and never counts again", async () => {
		await renderTheWidget({
			...elevenClosedEightWithinTenDays,
			sharePercent: 66.7,
			withinCount: 5,
			closedCount: 7,
			slePercentile: 85,
			sleDays: 6,
		});

		expect(screen.getByText("66.7 % (5 / 7)")).toBeInTheDocument();
		expect(
			screen.getByText("of Work Items closed within 6 days or less"),
		).toBeInTheDocument();
		expect(screen.getByText("Target 85 %")).toBeInTheDocument();
	});
});
