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

const CHART_POPOVERS = [
	{
		title: "Delivery Burnup",
		anchor: "burnup",
		purpose: "Total scope against completed Tickets over time.",
		whatYouSee: [
			"Top line: everything in this Release; rises when scope is added",
			"Filled area: the Tickets done",
			"Dashed line: scope still estimated, from Epics not yet broken down",
		],
		howToRead: [
			"The gap between line and area is the remaining work",
			"Gap narrowing: closing in",
			"Gap open or widening: losing ground to added scope",
		],
	},
	{
		title: "Delivery Predictability",
		anchor: "predictability",
		purpose: "How the forecast for this Release changed day by day.",
		whatYouSee: [
			"How Likely?: the chance of finishing by the target date, one point per day",
			"Line colour: Risky up to 50%, Realistic up to 70%, Confident up to 85%, Certain above",
			"When?: the forecasted finish dates (50th–95th percentile) against the target date",
		],
		howToRead: [
			"How Likely? rising: the odds of making the target are improving",
			"When? lines rising: the finish is slipping later; falling: pulling in",
			"A step in the target line: the target was moved, so read jumps around it with that in mind",
		],
	},
	{
		title: "Delivery Progress",
		anchor: "delivery-progress-fever-chart",
		purpose:
			"Where each Epic stands: how much is done, against how likely it is to be late.",
		whatYouSee: [
			"One dot per Epic: further right = more done, higher = more likely to be late",
			"Green, amber and red zones",
			"Run replays how each Epic moved",
		],
		howToRead: [
			"Green is on track, amber needs watching, red needs action",
			"Drifting up into red: the Epic is using up its buffer faster than it is making progress",
		],
	},
	{
		title: "Epics over Time",
		anchor: "features-over-time",
		purpose: "Which Epic grew, and when.",
		whatYouSee: [
			"One bar per day, split into a band per Epic and sized by its Tickets",
			"Hatched band: the size is still an estimate, not counted Tickets",
			"The line (right-hand axis): how many Epics the Release held",
		],
		howToRead: [
			"A band that suddenly grows: scope discovered inside that Epic",
			"A band going from hatched to solid: the Epic was broken down",
			"A band that ends: the Epic left the Release",
		],
	},
];

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

const listItemTexts = (list: HTMLElement): (string | null)[] =>
	within(list)
		.getAllByRole("listitem")
		.map((item) => item.textContent);

describe("DeliveryMetricsTab chart info buttons", () => {
	it.each(CHART_POPOVERS)(
		"$title explains itself in a popover that links to its docs section",
		({ title, anchor, purpose, whatYouSee, howToRead }) => {
			render(<DeliveryMetricsTab isLoading={false} history={history} />);

			fireEvent.click(screen.getByRole("button", { name: `About ${title}` }));

			const dialog = screen.getByRole("dialog", { name: `About ${title}` });
			expect(within(dialog).getByText(purpose)).toBeInTheDocument();
			expect(within(dialog).getByText("What you see")).toBeInTheDocument();
			expect(within(dialog).getByText("How to read it")).toBeInTheDocument();

			const lists = within(dialog).getAllByRole("list");
			expect(lists).toHaveLength(2);
			expect(listItemTexts(lists[0])).toEqual(whatYouSee);
			expect(listItemTexts(lists[1])).toEqual(howToRead);

			expect(
				within(dialog).getByRole("link", { name: "Learn More" }),
			).toHaveAttribute("href", `${DETAIL_PAGE}#${anchor}`);
		},
	);
});
