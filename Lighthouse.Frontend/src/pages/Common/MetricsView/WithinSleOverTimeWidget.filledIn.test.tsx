import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type {
	IWithinSleDay,
	IWithinSleOverTime,
	WithinSleDayOrigin,
} from "../../../models/Metrics/WithinSleOverTime";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import type { ITerminologyService } from "../../../services/Api/TerminologyService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import WithinSleOverTimeWidget from "./WithinSleOverTimeWidget";

vi.mock("../../../services/UsageData/usageDataReporter", () => ({
	useUsageDataReporter: () => vi.fn(),
}));

// The chart library draws to SVG, which the test cannot read; how a marker looks is pinned on the line builder.
vi.mock("@mui/x-charts", () => ({
	LineChart: () => <div data-testid="drawn-chart" />,
}));

const day = (
	recordedAt: string,
	origin: WithinSleDayOrigin,
	sharePercent: number | null,
): IWithinSleDay => ({
	recordedAt,
	horizon: 30,
	closedCount: sharePercent === null ? 3 : 5,
	withinCount: 3,
	slePercentile: 70,
	sleDays: 10,
	origin,
	verdict: sharePercent === null ? "TooFew" : "Measured",
	sharePercent,
});

const answerWith = (days: IWithinSleDay[]): IWithinSleOverTime => ({
	currentSlePercentile: 70,
	currentSleDays: 10,
	days,
});

// The instance calls its SLE a "Delivery Promise", so the note must use that word rather than "SLE".
const renamedSle: ITerminologyService = {
	getAllTerminology: vi.fn().mockResolvedValue([
		{
			id: 1,
			key: "sle",
			defaultValue: "SLE",
			description: "Service Level Expectation, short",
			value: "Delivery Promise",
		},
	]),
	updateTerminology: vi.fn(),
};

function drawTheChart(answer: IWithinSleOverTime): void {
	const metricsService = {
		getWithinSleOverTime: vi.fn().mockResolvedValue(answer),
	} as unknown as IMetricsService<IWorkItem | IFeature>;

	render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ terminologyService: renamedSle })}
			>
				<TerminologyProvider>
					<WithinSleOverTimeWidget
						owner={{ ownerId: 2, ownerUpdatedAt: 0 }}
						metricsService={metricsService}
						startDate={new Date(2026, 10, 1)}
						endDate={new Date(2026, 10, 10)}
					/>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
}

const FILLED_IN_NOTE =
	/filled in from history \(measured against today's Delivery Promise\)/;

describe("the note under the Within SLE Over Time chart about filled-in days", () => {
	it("says that filled-in days are measured against today's SLE, in the instance's own word, when a drawn point is filled in", async () => {
		drawTheChart(
			answerWith([
				day("2026-11-03", "FilledIn", 80),
				day("2026-11-04", "Recorded", 60),
			]),
		);

		expect(
			await screen.findByText(FILLED_IN_NOTE),
			"A drawn point was filled in from history and measured against the SLE set now; the note under the chart must say so, using the configured word for SLE.",
		).toBeInTheDocument();
	});

	it("shows no such note when no drawn point was filled in", async () => {
		drawTheChart(
			answerWith([
				day("2026-11-03", "Recorded", 80),
				day("2026-11-04", "FilledIn", null),
			]),
		);

		await screen.findByTestId("drawn-chart");
		await screen.findByText(/Within Delivery Promise Over Time/);
		expect(
			screen.queryByText(/filled in from history/),
			"Every drawn point was recorded at the time; a filled-in day too few to judge draws no point, so there is nothing for the note to explain.",
		).not.toBeInTheDocument();
	});
});
