import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type {
	IReportTemplates,
	ReportMetricKey,
} from "../../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import MetricsAndNameStep from "./MetricsAndNameStep";
import { newThenAndNowDraft, type ThenAndNowDraft } from "./thenAndNowDraft";

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			key === TERMINOLOGY_KEYS.CYCLE_TIME ? "Lead Time" : key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const NO_METRIC = "Choose at least one metric";

const offering = (...keys: string[]): IReportTemplates => ({
	today: "2026-10-02",
	earliestStart: null,
	minimumLengthDays: 14,
	templates: [
		{
			templateKey: "then-and-now",
			metrics: keys.map((key) => ({ key: key as ReportMetricKey })),
		},
	],
});

const NOT_CAPTURED = "Not captured for this report";

const renderStep = (
	templates: IReportTemplates,
	shownMetricKeys: ReportMetricKey[] | null,
	capturedMetricKeys?: ReportMetricKey[],
) => {
	const onChange = vi.fn<(draft: ThenAndNowDraft) => void>();
	const draft = {
		...newThenAndNowDraft(),
		shownMetricKeys,
		capturedMetricKeys,
	};
	render(
		<MetricsAndNameStep
			owner={{ kind: "team", id: 7 }}
			templates={templates}
			draft={draft}
			onChange={onChange}
			disabled={false}
		/>,
	);
	return onChange;
};

describe("the Metrics & name step", () => {
	it("ticks every offered metric, named in the instance's words, until one is unticked", () => {
		renderStep(offering("cycle-time"), null);

		expect(screen.getByRole("checkbox", { name: "Lead Time" })).toBeChecked();
		expect(screen.queryByText(NO_METRIC)).not.toBeInTheDocument();
	});

	it("does not offer a metric this version has no panel for", () => {
		renderStep(offering("cycle-time", "a-later-metric"), null);

		expect(screen.getAllByRole("checkbox")).toHaveLength(1);
		expect(
			screen.queryByRole("checkbox", { name: "a-later-metric" }),
		).toBeNull();
	});

	it("drops an unticked metric and keeps the others", async () => {
		const onChange = renderStep(offering("cycle-time", "throughput"), null);

		await userEvent.click(screen.getByRole("checkbox", { name: "Lead Time" }));

		expect(onChange).toHaveBeenCalledWith(
			expect.objectContaining({ shownMetricKeys: ["throughput"] }),
		);
	});

	it("asks for a metric while none is ticked, and adds the one ticked", async () => {
		const onChange = renderStep(offering("cycle-time"), []);

		expect(
			screen.getByRole("checkbox", { name: "Lead Time" }),
		).not.toBeChecked();
		expect(screen.getByText(NO_METRIC)).toBeInTheDocument();

		await userEvent.click(screen.getByRole("checkbox", { name: "Lead Time" }));

		expect(onChange).toHaveBeenCalledWith(
			expect.objectContaining({ shownMetricKeys: ["cycle-time"] }),
		);
	});

	it("says on the box itself why a metric the report never captured cannot be ticked", () => {
		renderStep(
			offering("cycle-time", "throughput"),
			["throughput"],
			["throughput"],
		);

		const neverCaptured = screen.getByRole("checkbox", { name: "Lead Time" });
		expect(neverCaptured).toBeDisabled();
		expect(neverCaptured).toHaveAccessibleDescription(NOT_CAPTURED);
		expect(
			screen.getByRole("checkbox", { name: "throughput" }),
		).not.toHaveAccessibleDescription(NOT_CAPTURED);
	});

	it("lets a metric the report shows but never captured be unticked", async () => {
		const onChange = renderStep(
			offering("cycle-time", "throughput"),
			["cycle-time", "throughput"],
			["throughput"],
		);

		await userEvent.click(screen.getByRole("checkbox", { name: "Lead Time" }));

		expect(onChange).toHaveBeenCalledWith(
			expect.objectContaining({ shownMetricKeys: ["throughput"] }),
		);
	});
});
