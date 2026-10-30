import { fireEvent, render, screen, within } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { parseDeliveryMetricsHistory } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../../../tests/testTheme";
import DeliveryMetricsTab from "./DeliveryMetricsTab";

const TERMS: Record<string, string> = {
	workItems: "Tickets",
	features: "Epics",
	feature: "Epic",
	delivery: "Release",
};

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => TERMS[key] ?? key,
	}),
}));

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// The charts are rendered for real so each card's own header and info button are what the test
// reaches. The plotting libraries are stubbed: their output is not under test here.
vi.mock("@mui/x-charts/LineChart", () => ({
	LineChart: ({ children }: { children?: ReactNode }) => (
		<svg>
			<title>Test</title>
			{children}
		</svg>
	),
}));

vi.mock("@mui/x-charts/ScatterChart", () => ({
	ScatterChart: ({ children }: { children?: ReactNode }) => (
		<svg>
			<title>Test</title>
			{children}
		</svg>
	),
}));

vi.mock("@mui/x-charts/hooks", () => ({
	useXScale: () => (value: number) => value,
	useYScale: () => (value: number) => value,
}));

vi.mock("@mui/x-charts", () => ({
	ChartsContainer: ({ children }: { children?: ReactNode }) => (
		<svg>
			<title>Test</title>
			{children}
		</svg>
	),
	BarPlot: () => null,
	ChartsReferenceLine: () => null,
	ChartsTooltip: () => null,
	ChartsXAxis: () => null,
	ChartsYAxis: () => null,
	LinePlot: () => null,
	MarkPlot: () => null,
}));

const DETAIL_PAGE =
	"https://docs.lighthouse.letpeople.work/portfolios/detail.html";

const BURNUP_COPY =
	"Total scope against completed Tickets over time. The top line is everything in this Release and rises when scope is added; the filled area is what's done. The gap between them is the remaining work: a gap that narrows is closing in, a gap that stays open or widens is losing ground to added scope. A dashed line shows how much of the scope is still estimated, from Epics not yet broken down.";

const PREDICTABILITY_COPY =
	"How the forecast for this Release changed day by day. How Likely? shows the chance of finishing by the target date. The line's colour shows the level: Risky up to 50%, Realistic up to 70%, Confident up to 85%, Certain above that. When? shows the forecasted finish dates (50th to 95th percentile) against the target. Lines that rise mean the finish is slipping later; lines that fall mean it's pulling in.";

const PROGRESS_COPY =
	"Where each Epic stands: how much of it is done, against how likely it is to be late. Green is on track, amber needs watching, red needs action. Press Run to replay how each Epic moved. One drifting up into red is using up its buffer faster than it's making progress.";

const FEATURES_COPY =
	"Which Epic grew, and when. Each day is one bar, split into one band per Epic and sized by its Tickets. A band that suddenly grows is scope discovered inside that Epic. A hatched band is still an estimate rather than counted Tickets. The line, on the right-hand axis, counts how many Epics the Release held.";

const history = parseDeliveryMetricsHistory({
	deliveryDate: "2026-06-21T00:00:00Z",
	firstSnapshotDate: "2026-06-01T00:00:00Z",
	points: [
		{
			date: "2026-06-01T00:00:00Z",
			totalWork: 20,
			doneWork: 4,
			remainingWork: 16,
			estimatedItemCount: null,
			forecastHowMany: null,
			likelihoodPercentage: 70,
			whenDistribution: null,
			featureBreakdown: [
				{
					referenceId: "F-1",
					name: "Checkout",
					completion: 20,
					likelihood: 60,
				},
			],
		},
		{
			date: "2026-06-08T00:00:00Z",
			totalWork: 20,
			doneWork: 10,
			remainingWork: 10,
			estimatedItemCount: null,
			forecastHowMany: null,
			likelihoodPercentage: 80,
			whenDistribution: null,
			featureBreakdown: [
				{
					referenceId: "F-1",
					name: "Checkout",
					completion: 50,
					likelihood: 85,
				},
			],
		},
	],
});

describe("DeliveryMetricsTab chart info buttons", () => {
	it.each([
		["Delivery Burnup", "burnup", BURNUP_COPY],
		["Delivery Predictability", "predictability", PREDICTABILITY_COPY],
		["Delivery Progress", "delivery-progress-fever-chart", PROGRESS_COPY],
		["Epics over Time", "features-over-time", FEATURES_COPY],
	])(
		"%s explains itself in a popover that links to its docs section",
		(title, anchor, copy) => {
			render(<DeliveryMetricsTab isLoading={false} history={history} />);

			fireEvent.click(screen.getByRole("button", { name: `About ${title}` }));

			const dialog = screen.getByRole("dialog", { name: `About ${title}` });
			expect(within(dialog).getByText(copy)).toBeInTheDocument();
			expect(
				within(dialog).getByRole("link", { name: "Learn More" }),
			).toHaveAttribute("href", `${DETAIL_PAGE}#${anchor}`);
		},
	);
});
