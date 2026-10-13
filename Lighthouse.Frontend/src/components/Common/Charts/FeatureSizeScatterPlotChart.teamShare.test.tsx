import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
import { testTheme } from "../../../tests/testTheme";
import FeatureSizeScatterPlotChart from "./FeatureSizeScatterPlotChart";

/**
 * Story #6251: on a Team, the scatter sizes each Feature by this Team's part of it, so its size
 * axis and its bubbles say whose Work Items they count. On a Portfolio, they read as before. With
 * no Features to plot, both owners show the card's title and one short line instead of nothing.
 *
 * The copy is word for word what the maintainer approved, so a change to it is a change to agree
 * with them first.
 */

const terms = vi.hoisted(() => ({
	current: {} as Record<string, string>,
}));

const seededTerms: Record<string, string> = {
	feature: "Feature",
	features: "Features",
	team: "Team",
	workItems: "Work Items",
	cycleTime: "Cycle Time",
};

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
	}),
}));

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(() => null),
}));

vi.mock("../../../utils/featureName", () => ({
	getWorkItemName: (name: string) => name,
}));

vi.mock("@mui/x-charts", () => {
	type ChartDatum = Record<string, unknown>;
	type Series = { data?: ChartDatum[] };
	type MarkerSlot = (props: Record<string, unknown>) => React.ReactNode;

	return {
		ChartsContainer: ({
			children,
			xAxis,
			yAxis,
			series,
		}: {
			children: React.ReactNode;
			xAxis?: Array<{ label?: string }>;
			yAxis?: Array<{ label?: string }>;
			series?: Series[];
		}) => (
			<div
				data-testid="chart-container"
				data-x-axis-label={xAxis?.[0]?.label}
				data-y-axis-label={yAxis?.[0]?.label}
				data-series={JSON.stringify(series ?? [])}
			>
				{children}
			</div>
		),
		ScatterPlot: ({ slots }: { slots?: { marker?: MarkerSlot } }) => {
			const container = document.querySelector(
				'[data-testid="chart-container"]',
			);
			const series = JSON.parse(
				container?.getAttribute("data-series") ?? "[]",
			) as Series[];
			return (
				<div data-testid="scatter-plot">
					{series.flatMap((s, seriesIndex) =>
						(s.data ?? []).map((datum, dataIndex) => (
							<div key={String(datum.groupKey)}>
								{slots?.marker?.({
									x: 10,
									y: 10,
									dataIndex,
									seriesId: String(seriesIndex),
									data: datum,
									color: "#1976d2",
									isHighlighted: false,
								})}
							</div>
						)),
					)}
				</div>
			);
		},
		ChartsXAxis: () => null,
		ChartsYAxis: () => null,
		ChartsTooltip: () => null,
		ChartsReferenceLine: () => null,
	};
});

function aFeature(id: number, size: number, cycleTime: number): IFeature {
	const feature = new Feature();
	feature.id = id;
	feature.referenceId = `OE-00${id}`;
	feature.name = `Feature ${id}`;
	feature.size = size;
	feature.cycleTime = cycleTime;
	feature.state = "Done";
	feature.stateCategory = "Done";
	feature.type = "Epic";
	feature.closedDate = new Date("2026-09-10");
	return feature;
}

// Two Features of the same size and Cycle Time share one bubble; the third has one of its own.
const gravitysFeatures = [
	aFeature(1, 5, 8),
	aFeature(2, 5, 8),
	aFeature(3, 3, 12),
];

const sizeAxis = () =>
	screen.getByTestId("chart-container").getAttribute("data-x-axis-label");
const verticalAxis = () =>
	screen.getByTestId("chart-container").getAttribute("data-y-axis-label");

// A bubble's tooltip is the title of its circle.
const bubbleTitles = () =>
	Array.from(document.querySelectorAll("title")).map(
		(title) => title.textContent,
	);

function switchToClosedDate(): void {
	fireEvent.click(screen.getByRole("button", { name: "Closed Date" }));
}

beforeEach(() => {
	terms.current = { ...seededTerms };
});

describe("the Feature Size scatter on a Team counts that Team's Work Items", () => {
	// @us-06 @contract-shape:bounded-change
	it.skip("names the size axis as this Team's Work Items", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(sizeAxis()).toBe("Size (this Team's Work Items)");
	});

	// @us-06 @contract-shape:bounded-change
	it.skip("names the size axis as this Team's Work Items in the Closed Date mode too", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		switchToClosedDate();

		expect(verticalAxis()).toBe("Size (this Team's Work Items)");
	});

	// @us-06 @contract-shape:bounded-change
	it.skip("a bubble of two Features says each holds that many of this Team's Work Items", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(bubbleTitles()).toContain(
			"2 Features with 5 of this Team's Work Items",
		);
	});

	// @us-06 @contract-shape:bounded-change
	it.skip("a bubble of one Feature names it in the singular", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(bubbleTitles()).toContain(
			"1 Feature with 3 of this Team's Work Items",
		);
	});

	// @us-06 @contract-shape:bounded-change
	it.skip("a bubble's button names the same share for a screen reader", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(
			screen.getByRole("button", {
				name: /^View 2 Features with 5 of this Team's Work Items$/,
			}),
		).toBeInTheDocument();
	});

	// @us-06 @contract-shape:bounded-change
	it.skip("says it in the words this instance uses", () => {
		terms.current = {
			...seededTerms,
			feature: "Epic",
			features: "Epics",
			team: "Squad",
			workItems: "Tickets",
		};

		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(sizeAxis()).toBe("Size (this Squad's Tickets)");
		expect(bubbleTitles()).toContain("2 Epics with 5 of this Squad's Tickets");
	});

	// @us-06 @contract-shape:unbounded-preservation
	it("on a Team, keeps the title Features Size with no line of text added to say whose Work Items it counts", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(
			screen.getByRole("heading", { name: /^Features Size$/ }),
		).toBeInTheDocument();
		// Deliberately unanchored: any visible text that mentions the Team at all would be a
		// subtitle the maintainer ruled out. The bubbles' own tooltips are the one place it belongs.
		const mentionsOfTheTeam = screen
			.queryAllByText(/this Team's/)
			.filter((element) => element.tagName.toLowerCase() !== "title");
		expect(mentionsOfTheTeam).toEqual([]);
	});

	// @us-06 @contract-shape:unbounded-preservation
	it("offers only the Cycle Time and Closed Date modes when it is handed no estimate", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(
			screen.getByRole("button", { name: /^Cycle Time$/ }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Closed Date" }),
		).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: /^Estimation$/ })).toBeNull();
	});
});

// The state chips work the same on both owners. On a Team they decide which of the Team's own
// Features are plotted: finished ones by default, open ones once the reader turns them on.
describe("the Feature Size scatter on a Team filters by state like the Portfolio's", () => {
	function anOpenFeature(
		id: number,
		size: number,
		stateCategory: "ToDo" | "Doing",
	): IFeature {
		const feature = aFeature(id, size, 0);
		feature.state = stateCategory === "ToDo" ? "To Do" : "In Progress";
		feature.stateCategory = stateCategory;
		feature.closedDate = null as unknown as Date;
		feature.workItemAge = 6;
		return feature;
	}

	// OE-011 is finished; OE-002 and OE-003 are open, each at a size no other Feature has, so each
	// is a bubble of its own.
	const gravitysFinishedAndOpenFeatures = [
		aFeature(11, 4, 9),
		anOpenFeature(2, 7, "ToDo"),
		anOpenFeature(3, 10, "Doing"),
	];

	const featuresPlotted = () =>
		(
			JSON.parse(
				screen.getByTestId("chart-container").getAttribute("data-series") ??
					"[]",
			) as { data?: unknown[] }[]
		).flatMap((series) => series.data ?? []).length;

	const chip = (name: string) =>
		screen.getByRole("button", { name: `${name} visibility toggle` });

	// @us-06 @contract-shape:unbounded-preservation
	it("starts with only the Team's finished Features plotted: Done on, To Do and In Progress off", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFinishedAndOpenFeatures}
			/>,
		);

		expect(featuresPlotted()).toBe(1);
		expect(chip("Done")).toHaveAttribute("aria-pressed", "true");
		expect(chip("To Do")).toHaveAttribute("aria-pressed", "false");
		expect(chip("In Progress")).toHaveAttribute("aria-pressed", "false");
	});

	// @us-06 @contract-shape:unbounded-preservation
	it("adds the Team's open Features once To Do and In Progress are turned on", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="team"
				sizeDataPoints={gravitysFinishedAndOpenFeatures}
			/>,
		);

		fireEvent.click(chip("To Do"));
		fireEvent.click(chip("In Progress"));

		expect(featuresPlotted()).toBe(3);
	});
});

describe("the Feature Size scatter on a Portfolio reads as before", () => {
	// @us-06 @contract-shape:unbounded-preservation
	it("names the size axis as child items", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="portfolio"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(sizeAxis()).toBe("Size (Child Items)");
	});

	// @us-06 @contract-shape:unbounded-preservation
	it("a bubble says its size in child items", () => {
		render(
			<FeatureSizeScatterPlotChart
				ownerKind="portfolio"
				sizeDataPoints={gravitysFeatures}
			/>,
		);

		expect(bubbleTitles()).toContain("2 items with size 5 child items");
	});
});

describe.each(["team", "portfolio"] as const)(
	"the Feature Size scatter on a %s with no Features to plot",
	(ownerKind) => {
		// @us-03 @error @contract-shape:bounded-change
		it.skip("shows its title and says there are no Features to show", () => {
			render(
				<FeatureSizeScatterPlotChart
					ownerKind={ownerKind}
					sizeDataPoints={[]}
				/>,
			);

			expect(
				screen.getByRole("heading", { name: /^Features Size$/ }),
			).toBeInTheDocument();
			expect(screen.getByText(/^No Features to show\.$/)).toBeInTheDocument();
			expect(screen.queryByTestId("chart-container")).toBeNull();
		});

		// @us-03 @error @contract-shape:bounded-change
		it.skip("says so in the words this instance uses", () => {
			terms.current = { ...seededTerms, feature: "Epic", features: "Epics" };

			render(
				<FeatureSizeScatterPlotChart
					ownerKind={ownerKind}
					sizeDataPoints={[]}
				/>,
			);

			expect(screen.getByText(/^No Epics to show\.$/)).toBeInTheDocument();
		});
	},
);
