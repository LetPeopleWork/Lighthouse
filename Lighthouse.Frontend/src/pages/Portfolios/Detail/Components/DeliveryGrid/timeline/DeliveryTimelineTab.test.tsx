import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../../../../models/EntityReference";
import type { IFeature, IFeatureStart } from "../../../../../../models/Feature";
import type {
	IFeatureDependency,
	NotHonouredReason,
} from "../../../../../../models/FeatureDependency";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import { TERMINOLOGY_KEYS } from "../../../../../../models/TerminologyKeys";
import DeliveryTimelineTab from "./DeliveryTimelineTab";
import type { DrawnDependency } from "./deliveryDependencyOverlay";
import type { TeamColour, TeamLane } from "./deliveryTeamLanes";
import type { TimelineBar } from "./deliveryTimelineModel";
import TimelineBarContent from "./TimelineBarContent";
import { timelineViewStore } from "./timelineView";

const licence = vi.hoisted(() => ({
	isPremium: true,
	/** Null until the licence has been fetched, which is a state the tab really meets. */
	isKnown: true,
}));

vi.mock("../../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: licence.isKnown
			? { canUsePremiumFeatures: licence.isPremium }
			: null,
	}),
}));

/**
 * The chart itself is stood in for, on purpose and as a rule rather than a convenience.
 *
 * What is worth asserting at this seam is the contract we hand the third party — which spans, with
 * which target, with or without its name pane. Its markup is theirs; a test reaching into it would
 * go red on their release rather than on our defect, which is the whole reason the adapter exists.
 */
type GanttProps = {
	bars: TimelineBar[];
	lanes?: TeamLane[];
	barTeams?: ReadonlyMap<number, TeamColour>;
	barStatusColors?: ReadonlyMap<number, string>;
	links?: DrawnDependency[];
	targetDate?: Date;
	today?: Date;
	onBarSelected?: (featureId: number) => void;
};

const ganttProps = vi.hoisted(() => ({ current: null as GanttProps | null }));

// The bars are drawn here rather than left to the stub, because what a bar shows is decided by the
// tab and reaches the bar only through a context - a stub that renders nothing would let the two
// halves of that disagree unnoticed.
vi.mock("./DeliveryGanttChart", () => ({
	default: (props: GanttProps) => {
		ganttProps.current = props;
		return (
			<div data-testid="delivery-gantt">
				{props.bars.map((bar) => (
					// Only the bar, and deliberately not the Team its bar wears. Resolving that here
					// would be this stub re-doing the adapter's own work, and every assertion about
					// it would then be reading back what the stub decided rather than what the
					// adapter does. What the tab hands over is asserted on the props directly; what
					// the adapter makes of them is asserted where the adapter runs.
					<div
						key={bar.featureId}
						data-testid={`timeline-bar-${bar.featureId}`}
					>
						<TimelineBarContent bar={bar} />
					</div>
				))}
			</div>
		);
	},
}));

const terminology = vi.hoisted(() => ({
	overrides: {} as Record<string, string>,
}));

vi.mock("../../../../../../services/TerminologyContext", async (original) => {
	const actual =
		await original<
			typeof import("../../../../../../services/TerminologyContext")
		>();

	return {
		...actual,
		useTerminology: () => {
			const real = actual.useTerminology();

			return {
				...real,
				getTerm: (key: string) =>
					terminology.overrides[key] ?? real.getTerm(key),
			};
		},
	};
});

const october = (day: number) => new Date(2026, 9, day);

const aPercentile = (probability: number, day: number) =>
	WhenForecast.new(probability, october(day));

const spreadFrom = (day: number) => [
	aPercentile(50, day),
	aPercentile(70, day + 1),
	aPercentile(85, day + 2),
	aPercentile(95, day + 4),
];

const startingAround = (day: number): IFeatureStart => ({
	source: "Forecast",
	percentiles: spreadFrom(day),
});

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Deep Sea Mapping Initiative",
		startForecast: startingAround(10),
		forecasts: spreadFrom(20),
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

const waitingOn = (
	referenceId: string,
	name: string,
	notHonouredReason: NotHonouredReason | null = null,
): IFeatureDependency => ({
	referenceId,
	name,
	url: null,
	source: "TrackerLink",
	notHonouredReason,
	blockerPositionedBelow: false,
	isWithheld: false,
});

const renderTab = (
	features: IFeature[],
	targetDate?: Date,
	teams: IEntityReference[] = [],
) =>
	render(
		<DeliveryTimelineTab
			features={features}
			targetDate={targetDate}
			featuresTerm="Features"
			teams={teams}
		/>,
	);

const forTeam = (teamId: number, startDay: number, endDay: number) => ({
	teamId,
	startPercentiles: [aPercentile(70, startDay), aPercentile(95, startDay + 2)],
	completionPercentiles: [aPercentile(70, endDay), aPercentile(95, endDay + 2)],
});

const ZENITH: IEntityReference = { id: 5, name: "Zenith" };
const GRAVITY: IEntityReference = { id: 6, name: "Gravity" };
const MERIDIAN: IEntityReference = { id: 7, name: "Meridian" };

const VIEW_KEY = "lighthouse:deliveryTimeline:view";

const teamsButton = () => screen.getByRole("button", { name: "Teams" });

/** Asks the chart for the Teams the way a reader does, through the control. */
const askForTheTeams = async () => {
	await userEvent.click(teamsButton());
};

/**
 * Breaks one of storage's own methods, and puts it back afterwards.
 *
 * **Spied on the object, not on `Storage.prototype`.** In this environment `localStorage` does not
 * inherit from it, so a prototype spy installs cleanly and intercepts nothing - which is how the
 * blocked-storage cases below passed without ever reaching the guards they name.
 *
 * Undone by hand, because `vi.restoreAllMocks()` does not put these back and a broken `setItem`
 * left standing makes the next test throw while arranging its own fixture.
 */
const brokenStorage: { mockRestore: () => void }[] = [];

const breakStorage = (method: "getItem" | "setItem") => {
	const spy = vi.spyOn(localStorage, method).mockImplementation(() => {
		throw new Error("site data is blocked");
	});

	brokenStorage.push(spy);

	return spy;
};

const repairStorage = () => {
	for (const spy of brokenStorage.splice(0)) {
		spy.mockRestore();
	}
};

afterEach(repairStorage);

const markOn = (featureId: number) =>
	within(screen.getByTestId(`timeline-bar-${featureId}`)).getByTestId(
		"timeline-bar-mark",
	);

// Written out rather than asked of the sentence builder the tab uses: the same call on both sides of
// an expectation agrees with itself whatever it returns.
const DEFAULT_SIZE_WARNING =
	"No child Work Items were found for this Feature. The remaining Work Items displayed are based on the default Feature size specified in the advanced project settings.";

beforeEach(() => {
	licence.isPremium = true;
	licence.isKnown = true;
	ganttProps.current = null;
	terminology.overrides = {};
	localStorage.clear();
	// Each switch's state is shared across every Delivery on the page, which means it is held
	// outside React and outlives a test. Clearing storage alone would leave the previous test's
	// choice standing.
	timelineViewStore.forget();
	vi.restoreAllMocks();
});

describe("DeliveryTimelineTab", () => {
	it("draws the Features that can be placed", () => {
		renderTab([feature()]);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
		expect(ganttProps.current?.bars).toHaveLength(1);
	});

	it("names what the buttons are choosing", () => {
		renderTab([feature()]);

		// Three bare percentages with no label leave the reader guessing what they are a
		// percentage of. "Probability" is the word Settings already uses for this number.
		expect(screen.getByText("Probability")).toBeInTheDocument();
		expect(screen.getByRole("group")).toHaveAccessibleName("Probability");
	});

	it("starts at 70% and offers 85 and 95", () => {
		renderTab([feature()]);

		expect(screen.getByRole("button", { name: "70%" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);

		for (const other of ["85%", "95%"]) {
			expect(screen.getByRole("button", { name: other })).toHaveAttribute(
				"aria-pressed",
				"false",
			);
		}
	});

	it("moves the bars when another confidence level is chosen", async () => {
		renderTab([feature()]);

		const atSeventy = ganttProps.current?.bars[0].start;

		await userEvent.click(screen.getByRole("button", { name: "95%" }));

		expect(ganttProps.current?.bars[0].start).not.toEqual(atSeventy);
	});

	it("lists a Feature it cannot place, with the reason, rather than dropping it", () => {
		renderTab([
			feature({ id: 1, name: "Placed" }),
			feature({ id: 2, name: "Nothing To Go On", forecasts: [] }),
		]);

		const listed = screen.getByTestId("timeline-unplaceable");

		expect(listed).toHaveTextContent("Nothing To Go On");
		expect(listed).toHaveTextContent(/finishes/);
		// The placeable one is on the chart, not in the list of refusals.
		expect(listed).not.toHaveTextContent("Placed");
		expect(ganttProps.current?.bars).toHaveLength(1);
	});

	it("says so when nothing at all can be placed, rather than drawing an empty axis", () => {
		renderTab([feature({ forecasts: [] })]);

		expect(screen.queryByTestId("delivery-gantt")).not.toBeInTheDocument();
		expect(
			screen.getByText(/None of these Features can be placed/i),
		).toBeInTheDocument();
		expect(screen.getByTestId("timeline-unplaceable")).toBeInTheDocument();
	});

	it("shows the premium notice and no chart without a licence", () => {
		licence.isPremium = false;

		renderTab([feature()]);

		// An empty notice is a silent failure: the reader sees a blank panel where the chart was
		// and is told nothing, so the copy is pinned rather than just the element.
		expect(screen.getByTestId("premium-feature-notice")).toHaveTextContent(
			/premium feature/i,
		);
		expect(screen.queryByTestId("delivery-gantt")).not.toBeInTheDocument();
	});

	it("offers the premium notice in the word this instance uses for a Delivery", () => {
		licence.isPremium = false;
		terminology.overrides = { [TERMINOLOGY_KEYS.DELIVERY]: "Launch" };

		renderTab([feature()]);

		expect(screen.getByTestId("premium-feature-notice")).toHaveTextContent(
			"The Launch timeline is a premium feature",
		);
	});

	it("withholds the chart while the licence is still unknown", () => {
		// The hook answers null until the licence has been fetched. Reading through it without a
		// guard throws and takes the whole tab down; treating unknown as licensed would show a
		// premium chart to an unlicensed instance for as long as the request is in flight.
		licence.isKnown = false;

		renderTab([feature()]);

		expect(screen.getByTestId("premium-feature-notice")).toBeInTheDocument();
		expect(screen.queryByTestId("delivery-gantt")).not.toBeInTheDocument();
	});

	it("keeps a confidence level selected when its button is clicked again", async () => {
		renderTab([feature()]);

		// A toggle group reports null when the active button is pressed a second time. Taking that
		// at face value would leave the timeline with no percentile and nothing to draw.
		await userEvent.click(screen.getByRole("button", { name: "70%" }));

		expect(screen.getByRole("button", { name: "70%" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		expect(ganttProps.current?.bars).toHaveLength(1);
	});

	it("says how many Features are not on the timeline", () => {
		renderTab([
			feature({ id: 1, name: "One", forecasts: [] }),
			feature({ id: 2, name: "Two", startForecast: undefined }),
		]);

		expect(screen.getByTestId("timeline-unplaceable")).toHaveTextContent(
			"Not on the timeline (2)",
		);
	});

	it("shows no list at all when every Feature is on the timeline", () => {
		renderTab([feature()]);

		expect(
			screen.queryByTestId("timeline-unplaceable"),
		).not.toBeInTheDocument();
	});

	it("opens the details for the Feature whose bar was chosen", async () => {
		renderTab([
			feature({ id: 1, name: "Deep Sea Mapping" }),
			feature({ id: 2, name: "Sonar Refit" }),
		]);

		// The chart is stood in for, so the click is driven through the handler it was handed —
		// which is the contract that matters here. Whether a bar is clickable in a browser is the
		// screenshot test's business, not this one's.
		act(() => {
			ganttProps.current?.onBarSelected?.(2);
		});

		const dialog = await screen.findByRole("dialog");

		expect(dialog).toHaveTextContent("Sonar Refit");
		expect(dialog).not.toHaveTextContent("Deep Sea Mapping");
	});

	it("shows no details until a bar is chosen", () => {
		renderTab([feature()]);

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("closes the details again, and can reopen them", async () => {
		renderTab([feature({ id: 2, name: "Sonar Refit" })]);

		act(() => ganttProps.current?.onBarSelected?.(2));
		await screen.findByRole("dialog");

		await userEvent.click(screen.getByRole("button", { name: /close/i }));

		// A dialog that cannot be dismissed leaves the timeline behind it unreachable, and one
		// that forgets to clear its selection cannot be opened on the same bar twice.
		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);

		act(() => ganttProps.current?.onBarSelected?.(2));

		expect(await screen.findByRole("dialog")).toHaveTextContent("Sonar Refit");
	});

	it("gives the chart one reading of today, and keeps it across a re-render", async () => {
		renderTab([feature()]);

		// A fresh Date per render is a fresh identity, which would invalidate everything the
		// chart memoises against it — including the axis range — on every keystroke elsewhere.
		const chartsDay = ganttProps.current?.today;
		expect(chartsDay).toBeInstanceOf(Date);

		await userEvent.click(screen.getByRole("button", { name: "95%" }));

		expect(ganttProps.current?.today).toBe(chartsDay);
	});

	it("hands the Delivery's target date through to the chart", () => {
		const target = october(31);

		renderTab([feature()], target);

		expect(ganttProps.current?.targetDate).toEqual(target);
	});

	it("hands the chart the waits it should draw", () => {
		renderTab([
			feature({ id: 1, name: "Hull Fabrication", referenceId: "OE-001" }),
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [waitingOn("OE-001", "Hull Fabrication")],
			}),
		]);

		expect(ganttProps.current?.links).toEqual([
			{ blockerFeatureId: 1, waitingFeatureId: 2 },
		]);
		expect(screen.queryByTestId("timeline-chart-note")).not.toBeInTheDocument();
	});

	it("lists the chosen Feature's warnings beside it", async () => {
		renderTab([
			feature({ id: 1, name: "Hull Fabrication", referenceId: "OE-001" }),
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [waitingOn("OE-001", "Hull Fabrication", "InALoop")],
			}),
		]);

		act(() => ganttProps.current?.onBarSelected?.(2));

		const dialog = await screen.findByRole("dialog");

		expect(
			within(dialog).getByRole("columnheader", { name: /Warnings/ }),
		).toBeInTheDocument();
		// Asserted against the fixture's own facts - the blocker it names and the loop it is in -
		// rather than against what the sentence builder returns, which would be the same reduction
		// on both sides of the expectation.
		expect(dialog).toHaveTextContent("Hull Fabrication");
		expect(dialog).toHaveTextContent(/waiting on each other/i);
	});

	it("keeps what only the chart knows out of the Feature's warnings", async () => {
		renderTab([
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [waitingOn("OE-404", "Mineral Survey")],
			}),
		]);

		act(() => ganttProps.current?.onBarSelected?.(2));

		const dialog = await screen.findByRole("dialog");

		// The blocker is simply somewhere else. Nothing is wrong with the dependency, and saying
		// so under a header that reads "Warnings" would label a sound wait as a problem - on this
		// screen and on the fifteen others the column was written for.
		expect(
			within(dialog).getByRole("columnheader", { name: /Warnings/ }),
		).toBeInTheDocument();
		expect(
			within(dialog).getByTestId("warningsColumnContent").textContent,
		).toBe("");
		expect(dialog).not.toHaveTextContent(/not on this timeline/i);
		expect(dialog).not.toHaveTextContent("Mineral Survey");
	});

	it("says once, above the chart, that this Portfolio has set its dependencies aside", () => {
		renderTab([
			feature({ id: 1, name: "Hull Fabrication", referenceId: "OE-001" }),
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [
					waitingOn("OE-001", "Hull Fabrication", "IgnoredByPortfolio"),
				],
			}),
		]);

		// Said on every dependent bar it would be the same words repeated; said nowhere, the
		// reader is left with a chart whose lines silently vanished.
		const notes = screen.getAllByTestId("timeline-chart-note");

		expect(notes).toHaveLength(1);
		expect(notes[0]).toHaveTextContent(
			"Portfolio is set to ignore dependencies.",
		);
		expect(ganttProps.current?.links).toEqual([]);
	});

	it("warns on the bar of a Feature the table warns about, dependency or not", () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({ id: 1, name: "Sonar Refit", isUsingDefaultFeatureSize: true }),
		]);

		// Nothing here is about dependencies, and the reader is looking at the same Feature the
		// table marks. A bar that stays blank is the two screens disagreeing about it.
		expect(markOn(1)).toHaveAccessibleName(`Warning. ${DEFAULT_SIZE_WARNING}`);
	});

	it("warns on the bar of a Feature marked done with work still left", () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({
				id: 1,
				name: "Sonar Refit",
				stateCategory: "Done",
				getRemainingWorkForFeature: () => 3,
			}),
		]);

		expect(markOn(1)).toHaveAccessibleName(
			/^Warning\. This Feature is marked as done/,
		);
	});

	it("warns on the bar in the words of the reason the forecast gives", () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({ id: 1, name: "Hull Fabrication", referenceId: "OE-001" }),
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [waitingOn("OE-001", "Hull Fabrication", "InALoop")],
			}),
		]);

		// The refusal moved the dates, and the bar is the picture of those dates. A bar that says
		// nothing about it leaves the reader looking for a line that was deliberately not drawn.
		expect(markOn(2)).toHaveAccessibleName(
			"Warning. This Feature and Hull Fabrication are waiting on each other. That dependency is not included in the forecast.",
		);
		expect(ganttProps.current?.links).toEqual([]);
	});

	it("warns on the bar of a Feature whose blocker sits below it, and still draws the line", () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({ id: 1, name: "Hull Fabrication", referenceId: "OE-001" }),
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [
					{
						...waitingOn("OE-001", "Hull Fabrication"),
						blockerPositionedBelow: true,
					},
				],
			}),
		]);

		// The forecast did wait, so the line is the truth about the dates. What the order says about
		// it is a warning, and it is said once, in the words the table uses.
		expect(markOn(2)).toHaveAccessibleName(
			"Warning. This Feature depends on Hull Fabrication, which sits below it in the order.",
		);
		expect(ganttProps.current?.links).toEqual([
			{ blockerFeatureId: 1, waitingFeatureId: 2 },
		]);
	});

	it("marks, without alarm, a bar whose only note is where its blocker went", () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				dependsOn: [waitingOn("OE-404", "Mineral Survey")],
			}),
		]);

		// Nothing is wrong with this wait; its blocker is simply somewhere else. Raising a warning
		// about it would teach the reader that the warning symbol means nothing in particular.
		expect(markOn(2)).toHaveAccessibleName(
			"Note. Waiting on Mineral Survey, which is not on this timeline.",
		);
	});

	it("warns on a bar that has both a warning and a note, and says both", () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({
				id: 2,
				name: "Sonar Refit",
				referenceId: "OE-002",
				isUsingDefaultFeatureSize: true,
				dependsOn: [waitingOn("OE-404", "Mineral Survey")],
			}),
		]);

		expect(markOn(2)).toHaveAccessibleName(
			`Warning. ${DEFAULT_SIZE_WARNING} Waiting on Mineral Survey, which is not on this timeline.`,
		);
	});

	it("leaves a bar with nothing against it unmarked, and offers no all-clear", () => {
		timelineViewStore.set("warnings");

		renderTab([feature({ id: 1, name: "Sonar Refit" })]);

		const barElement = screen.getByTestId("timeline-bar-1");

		expect(
			within(barElement).queryByTestId("timeline-bar-mark"),
		).not.toBeInTheDocument();
		// A green check on every bar that is fine would sit on most of them, competing with the
		// name for the only space a bar a few pixels tall has. Asked of the markup rather than by
		// role: a decorative icon is hidden from the accessibility tree and would pass a role query.
		expect(barElement.querySelector("svg")).toBeNull();
	});

	it("explains a warning that has nothing to do with dependencies on hover", async () => {
		timelineViewStore.set("warnings");

		renderTab([
			feature({ id: 1, name: "Sonar Refit", isUsingDefaultFeatureSize: true }),
		]);

		// A symbol the hover does not account for is an alarm with no cause attached to it.
		await userEvent.hover(
			within(screen.getByTestId("timeline-bar-1")).getByTestId(
				"timeline-bar-content",
			),
		);

		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			DEFAULT_SIZE_WARNING,
		);
	});

	it("warns in the words this instance uses for the things it names", () => {
		timelineViewStore.set("warnings");

		terminology.overrides = { [TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets" };

		renderTab([
			feature({ id: 1, name: "Sonar Refit", isUsingDefaultFeatureSize: true }),
		]);

		expect(markOn(1)).toHaveAccessibleName(
			"Warning. No child Tickets were found for this Feature. The remaining Tickets displayed are based on the default Feature size specified in the advanced project settings.",
		);
	});
});

describe("showing the Teams behind a Feature's bar", () => {
	const splittingFeature = (overrides: Partial<IFeature> = {}) =>
		feature({
			id: 1,
			name: "Coral Reef Restoration",
			teamForecasts: [forTeam(5, 12, 15), forTeam(6, 17, 24)],
			...overrides,
		});

	it("shows which Team drives which end once the reader asks for it", async () => {
		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

		const beforeTheClick = ganttProps.current?.bars;

		expect(ganttProps.current?.lanes ?? []).toEqual([]);

		await askForTheTeams();

		// The lanes reach the chart, each written with its own Team's name and its own dates, and
		// the Feature's own bar arrives exactly as it arrived before the click.
		expect(
			ganttProps.current?.lanes?.map((lane) => [
				lane.teamName,
				lane.start,
				lane.end,
			]),
		).toEqual([
			["Gravity", october(17), october(24)],
			["Zenith", october(12), october(15)],
		]);
		expect(ganttProps.current?.bars).toEqual(beforeTheClick);
	});

	it("keeps the lanes off until they are asked for, and off again afterwards", async () => {
		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

		const untouched = { ...ganttProps.current };

		expect(teamsButton()).toHaveAttribute("aria-pressed", "false");

		await askForTheTeams();
		expect(ganttProps.current?.lanes).toHaveLength(2);

		// Asking for nothing is how the reader gets back, now that the views are one choice rather
		// than three switches: clicking the Teams again would report an empty group, which the
		// control ignores so that the chart can never end up showing something no button claims.
		await userEvent.click(screen.getByRole("button", { name: "Nothing" }));

		// A round trip that does not return leaves the reader with a chart they cannot put back.
		expect(ganttProps.current?.lanes ?? []).toEqual(untouched.lanes ?? []);
		expect(ganttProps.current?.bars).toEqual(untouched.bars);
	});

	it("withholds the control entirely where no Team on the chart can be named", () => {
		// Asserted with the chart rendered, so "no control" cannot pass on a blank tab. This
		// Delivery holds no Team names at all, so there is nothing the switch could show.
		renderTab([feature({ id: 1, teamForecasts: [forTeam(5, 12, 15)] })]);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Teams" }),
		).not.toBeInTheDocument();
	});

	it("offers the control for a Delivery whose Features each have one Team", () => {
		// Nothing here splits, and the switch still does real work: it answers which Team is on
		// each bar, which the bar never says. Gating this on a Feature having two or more Teams
		// hides a control that would have been useful on every row.
		renderTab(
			[
				feature({
					id: 1,
					name: "Coral Reef",
					teamForecasts: [forTeam(5, 12, 15)],
				}),
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(6, 12, 15)],
				}),
			],
			undefined,
			[ZENITH, GRAVITY],
		);

		expect(screen.getByRole("button", { name: "Teams" })).toBeInTheDocument();
		expect(ganttProps.current?.lanes ?? []).toEqual([]);
	});

	it("withholds the control for a three-Team Feature that has no bar", () => {
		// Counted over the Features actually placed. A Team with no throughput takes the whole
		// Feature off the chart, so nothing about its three Teams can be shown. The Feature that
		// does have a bar carries no Team at all, so it offers no reason of its own.
		renderTab(
			[
				feature({
					id: 1,
					name: "Deep Sea Mapping",
					teamsWithoutForecast: ["Meridian"],
					teamForecasts: [
						forTeam(5, 12, 15),
						forTeam(6, 17, 24),
						forTeam(7, 11, 13),
					],
				}),
				feature({ id: 2, name: "Kelp Forest", teamForecasts: [] }),
			],
			undefined,
			[ZENITH, GRAVITY, MERIDIAN],
		);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Teams" }),
		).not.toBeInTheDocument();
	});

	it("offers the control where a placed Feature's second Team has no dates", () => {
		// Counted over the `teamForecasts` rows rather than over the lanes actually drawn: this
		// Feature has two contributing Teams and will grow exactly one lane.
		renderTab(
			[
				splittingFeature({
					teamForecasts: [
						forTeam(5, 12, 15),
						{ teamId: 7, startPercentiles: [], completionPercentiles: [] },
					],
				}),
			],
			undefined,
			[ZENITH, MERIDIAN],
		);

		expect(screen.getByRole("button", { name: "Teams" })).toBeInTheDocument();
	});

	it("names the control in this instance's own word for a Team", () => {
		const seeded = renderTab([splittingFeature()], undefined, [
			ZENITH,
			GRAVITY,
		]);

		expect(
			within(seeded.container).getByRole("button", { name: "Teams" }),
		).toHaveAccessibleName("Teams");

		terminology.overrides = { [TERMINOLOGY_KEYS.TEAMS]: "Squads" };

		const renamed = renderTab([splittingFeature()], undefined, [
			ZENITH,
			GRAVITY,
		]);

		// Pinned against both literals. The label is JSX text, which a mutation run never
		// challenges, and a hard-coded string passes the first clause and fails only the second.
		expect(
			within(renamed.container).getByRole("button", { name: "Squads" }),
		).toHaveAccessibleName("Squads");
	});

	it("says nothing about a missing lane on a chart that has no lanes", async () => {
		// With the switch off there is no split, so there is nothing for a Team to be missing
		// from. A note about a lane on a chart without lanes names something the reader cannot
		// see, and breaks the one promise the switch makes: off is the chart exactly as it was.
		const oneTeamHasNoDates = splittingFeature({
			teamForecasts: [
				forTeam(5, 12, 15),
				{ teamId: 7, startPercentiles: [], completionPercentiles: [] },
			],
		});

		renderTab([oneTeamHasNoDates], undefined, [ZENITH, MERIDIAN]);

		const bar = screen.getByTestId("timeline-bar-1");

		expect(
			within(bar).queryByTestId("timeline-bar-mark"),
		).not.toBeInTheDocument();
		expect(bar).not.toHaveTextContent("Meridian");

		// Paired with the same bar once the Teams are shown, so this cannot pass against a tab
		// that never says anything about an un-laned Team at all.
		await askForTheTeams();

		expect(markOn(1)).toHaveAccessibleName(/Meridian/);
	});

	it("names a Team with no lane on its Feature's bar, as a note rather than an alarm", async () => {
		renderTab(
			[
				splittingFeature({
					teamForecasts: [
						forTeam(5, 12, 15),
						{ teamId: 7, startPercentiles: [], completionPercentiles: [] },
					],
				}),
			],
			undefined,
			[ZENITH, MERIDIAN],
		);

		await askForTheTeams();

		// Asserted against the fixture's own Team name and against the mark's accessible name,
		// whose leading word is what tells a note from a warning. Never against what the sentence
		// helper returns, which would be the same reduction on both sides.
		expect(markOn(1)).toHaveAccessibleName(
			"Note. No forecast for Meridian, so it is not shown separately.",
		);
		expect(
			within(screen.getByTestId("timeline-bar-1")).getByTestId(
				"timeline-bar-content",
			),
		).toHaveTextContent("Meridian");
	});

	it("finds the lanes still on for a reader who turns them on and comes back later", async () => {
		const { unmount } = renderTab([splittingFeature()], undefined, [
			ZENITH,
			GRAVITY,
		]);

		await askForTheTeams();

		// The key is pinned exactly once, here. A round trip passes happily against any key at
		// all, and a renamed key silently forgets every reader's choice.
		expect(localStorage.getItem(VIEW_KEY)).toBe("teams");

		unmount();
		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

		expect(teamsButton()).toHaveAttribute("aria-pressed", "true");
		expect(ganttProps.current?.lanes).toHaveLength(2);
	});

	it("shows no lanes to a reader who has never touched the control", () => {
		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

		// The house precedent for this defaults to ON and therefore compares against "false".
		// Lifted without flipping it, an absent key reads as on and every reader who has never
		// heard of the feature meets a chart of twenty-one rows.
		expect(localStorage.getItem(VIEW_KEY)).toBeNull();
		expect(ganttProps.current?.lanes ?? []).toEqual([]);
	});

	it("keeps the tab working, and the lanes off, when storage is blocked or corrupt", async () => {
		// The Teams are stored **before** the read is broken. Against an absent key this asserts
		// nothing: no lanes is what an unset choice produces anyway, so a guard that never ran and
		// a reader who never chose are indistinguishable. With the choice stored, a read that got
		// through would put two lanes on the chart.
		localStorage.setItem(VIEW_KEY, "teams");
		timelineViewStore.forget();
		breakStorage("getItem");

		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
		expect(ganttProps.current?.lanes ?? []).toEqual([]);

		repairStorage();
		localStorage.clear();
		timelineViewStore.forget();

		// A word this version does not offer, and a word from a version that did. Handed back as
		// it stands, either selects a button the group does not have - and the reader is left
		// looking at a control with nothing pressed on a chart that is showing something.
		for (const stored of ["showTeams", "sub-lanes"]) {
			localStorage.setItem(VIEW_KEY, stored);
			timelineViewStore.forget();
			renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

			expect(ganttProps.current?.lanes ?? []).toEqual([]);
		}

		localStorage.clear();
		timelineViewStore.forget();
		breakStorage("setItem");

		const blocked = renderTab([splittingFeature()], undefined, [
			ZENITH,
			GRAVITY,
		]);

		// Storage that will not take the choice costs this reader the memory of it, not the view.
		await userEvent.click(
			within(blocked.container).getByRole("button", { name: "Teams" }),
		);

		expect(ganttProps.current?.lanes).toHaveLength(2);
		expect(localStorage.getItem(VIEW_KEY)).toBeNull();
	});

	it("moves every control on the page together, not just the one clicked", async () => {
		// A Portfolio opens several Deliveries at once, each with a control of its own. Held in
		// component state they each get a truth of their own: move one and the others sit there
		// contradicting it, and storage agrees with none of them.
		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);
		renderTab(
			[
				splittingFeature({
					id: 42,
					name: "Whale Migration Study",
				}),
			],
			undefined,
			[ZENITH, GRAVITY],
		);

		const [first, second] = screen.getAllByRole("button", { name: "Teams" });

		expect(second).toHaveAttribute("aria-pressed", "false");

		await userEvent.click(first);

		expect(first).toHaveAttribute("aria-pressed", "true");
		expect(second).toHaveAttribute("aria-pressed", "true");
		expect(localStorage.getItem(VIEW_KEY)).toBe("teams");
	});

	it("carries the reader's choice into the next Delivery they open", async () => {
		const { unmount } = renderTab([splittingFeature()], undefined, [
			ZENITH,
			GRAVITY,
		]);

		await askForTheTeams();
		unmount();

		// A key composed with the Delivery's id is what the nearest precedent does, and it would
		// leave this reader turning the same control on once per Delivery. "Show me Teams" is a
		// property of the reader.
		renderTab(
			[
				splittingFeature({
					id: 42,
					name: "Whale Migration Study",
					teamForecasts: [forTeam(5, 12, 15), forTeam(7, 17, 24)],
				}),
			],
			undefined,
			[ZENITH, MERIDIAN],
		);

		expect(ganttProps.current?.lanes?.map((lane) => lane.teamName)).toEqual([
			"Meridian",
			"Zenith",
		]);
	});

	it("names the one Team on a Feature only one Team works on, without adding a row", async () => {
		renderTab(
			[
				splittingFeature(),
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(7, 12, 15)],
				}),
			],
			undefined,
			[ZENITH, GRAVITY, MERIDIAN],
		);

		await askForTheTeams();

		// No extra row for it - with one Team the earliest and the latest are that Team, so a row
		// would be a second bar drawn where the first one is. What it gets instead is the Team.
		expect(
			ganttProps.current?.lanes?.filter((lane) => lane.featureId === 2),
		).toEqual([]);
		expect(ganttProps.current?.barTeams?.get(2)?.teamName).toBe("Meridian");
		// And the Feature two Teams work on keeps the default colour, so dark reads as "several"
		// and coloured reads as "this one".
		expect(ganttProps.current?.barTeams?.has(1)).toBe(false);
	});

	it("leaves a single-Team Feature's dates exactly where they were when the Teams appear", async () => {
		// Its bar now changes appearance with the switch, which is new. Its span must not, and
		// that is the guarantee worth a test of its own rather than one inherited from the split.
		renderTab(
			[
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(7, 12, 15)],
				}),
			],
			undefined,
			[MERIDIAN],
		);

		const before = ganttProps.current?.bars;

		await askForTheTeams();

		// Both halves. The Team has to have arrived, or this passes against a switch that does
		// nothing at all; and the bar has to be the bar it was, which is the whole guarantee.
		expect(ganttProps.current?.barTeams?.get(2)?.teamName).toBe("Meridian");
		expect(ganttProps.current?.bars).toEqual(before);
	});

	it("shows a key to the colours only once the Teams are being shown", async () => {
		renderTab([splittingFeature()], undefined, [ZENITH, GRAVITY]);

		expect(
			screen.queryByTestId("timeline-team-legend"),
		).not.toBeInTheDocument();

		await askForTheTeams();

		// Every Team carrying a colour, named in full. A row is only as wide as its Team's span,
		// so the names written along the rows are routinely cut to a few characters and this is
		// the only place the colour can be read back to a Team.
		const legend = screen.getByTestId("timeline-team-legend");

		expect(legend).toHaveTextContent("Gravity");
		expect(legend).toHaveTextContent("Zenith");
	});

	it("shows the key for a Delivery whose Features each have one Team", async () => {
		// Nothing splits here, so nothing gets a lane - and the colours still need explaining,
		// because every bar is now wearing one.
		renderTab(
			[
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(7, 12, 15)],
				}),
			],
			undefined,
			[MERIDIAN],
		);

		await askForTheTeams();

		expect(ganttProps.current?.lanes ?? []).toEqual([]);
		expect(screen.getByTestId("timeline-team-legend")).toHaveTextContent(
			"Meridian",
		);
	});
});

describe("a Team the split cannot give a row to", () => {
	const aFeatureWithAnUnnameableTeam = () =>
		feature({
			id: 1,
			teamForecasts: [
				forTeam(5, 12, 15),
				{ teamId: 7, startPercentiles: [], completionPercentiles: [] },
			],
		});

	it("is named on the bar for a reader who has asked for the Teams and nothing else", async () => {
		// The promise the split is built on: it never shows fewer Teams than the Feature has. That
		// naming once travelled with the bar's warnings, which meant a reader looking at the Teams
		// was told about one of them and left to guess at the other.
		renderTab([aFeatureWithAnUnnameableTeam()], undefined, [ZENITH, MERIDIAN]);

		await askForTheTeams();

		// Both halves. The lane is what proves the split ran at all; the name is what the split
		// promised. Either alone passes against a chart that does one and not the other.
		expect(ganttProps.current?.lanes).toHaveLength(1);
		expect(screen.getByTestId("timeline-bar-1")).toHaveTextContent("Meridian");
	});

	it("says nothing about it when the reader is looking at something else", async () => {
		// A sentence about a missing lane, on a chart that has no lanes, names something the reader
		// cannot see. Paired with the test above, so neither passes against a chart that never
		// names the Team at all.
		renderTab([aFeatureWithAnUnnameableTeam()], undefined, [ZENITH, MERIDIAN]);

		await askForTheTeams();
		expect(screen.getByTestId("timeline-bar-1")).toHaveTextContent("Meridian");

		await userEvent.click(screen.getByRole("button", { name: "Nothing" }));

		expect(screen.getByTestId("timeline-bar-1")).not.toHaveTextContent(
			"Meridian",
		);
	});
});

describe("saying which Features are finished and which are late", () => {
	// This Feature's bar ends on the 21st at 70 and on the 24th at 95.
	//
	// Two dates, because the two questions here need different ones. Due on the 20th it is late
	// whichever probability is selected, which is what a test about the colouring wants. Due on the
	// 22nd it is in time at 70 and late at 95 - the only shape in which "the control does not come
	// and go" can fail, and therefore the only one in which it is worth asserting.
	const DUE_ON_THE_TWENTIETH = new Date(Date.UTC(2026, 9, 20, 22, 30));
	const DUE_ON_THE_TWENTY_SECOND = new Date(Date.UTC(2026, 9, 22, 22, 30));

	const statusButton = () => screen.queryByRole("button", { name: "Status" });

	const AMBER = "#ff9800";

	it("colours the late bars for a reader who has chosen nothing", async () => {
		// The status is what a first-time reader is shown, so this needs no arranging - which is
		// the whole claim. Paired with a reader who asks for nothing, or it passes against a chart
		// that colours regardless.
		renderTab([feature()], DUE_ON_THE_TWENTIETH);

		expect(ganttProps.current?.barStatusColors?.get(1)).toBe(AMBER);

		await userEvent.click(screen.getByRole("button", { name: "Nothing" }));

		expect(ganttProps.current?.barStatusColors).toBeUndefined();
	});

	it("keeps the control still while the reader works the probability buttons", async () => {
		// Which bars cross the date is exactly what these buttons change, so a control offered on
		// the strength of the colouring currently drawn would appear at 95 and be gone again at 70
		// - under the reader's hand, while they are comparing the two.
		renderTab([feature()], DUE_ON_THE_TWENTY_SECOND);

		expect(statusButton()).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "95%" }));
		expect(statusButton()).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "70%" }));
		expect(statusButton()).toBeInTheDocument();
	});

	it("withholds the view from a Delivery with nothing it could ever say", () => {
		// No date to be late against and nothing finished. A button here would do nothing when
		// used, and a reader who gets nothing back concludes the chart is broken rather than that
		// the question does not apply.
		renderTab([feature()], undefined);

		expect(statusButton()).not.toBeInTheDocument();
	});

	it("withholds the view when the only finished Feature has no beginning to draw from", () => {
		// Finished work is the one thing that makes this view worth offering on a Delivery with no
		// date. A finished Feature with nothing saying when it began gets no bar at all, so
		// nothing on the chart would ever be coloured.
		renderTab(
			[
				feature({ id: 1 }),
				feature({ id: 2, closedDate: october(9), startForecast: undefined }),
			],
			undefined,
		);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
		expect(statusButton()).not.toBeInTheDocument();
	});

	it("counts a Feature whose beginning is a fact rather than a forecast", () => {
		// A started Feature carries an observed date and an empty list of percentiles, so a filter
		// asking only for percentiles would call it undrawable and withhold the view for it - and
		// it is the one kind of Feature that certainly has a bar.
		renderTab(
			[
				feature({
					id: 1,
					startForecast: {
						source: "Observed",
						observedDate: october(3),
						percentiles: [],
					},
					closedDate: october(9),
				}),
			],
			undefined,
		);

		expect(ganttProps.current?.bars).toHaveLength(1);
		expect(statusButton()).toBeInTheDocument();
	});

	it("counts a finished Feature whose completion forecast is gone", () => {
		// With nothing left to simulate the forecast list can be empty, and the day it closed is
		// still an end. A filter asking only for forecasts would withhold the view for exactly the
		// Features the view exists to colour.
		renderTab(
			[feature({ id: 1, closedDate: october(9), forecasts: [] })],
			undefined,
		);

		expect(ganttProps.current?.bars).toHaveLength(1);
		expect(statusButton()).toBeInTheDocument();
	});

	it("offers the view for a Delivery with no date where only some of the work is finished", () => {
		// Paired with the test above, which is what stops that one passing against a tab that
		// never offers the view at all. Finished is not a verdict about a date, so it survives the
		// date being absent.
		//
		// One of the two is still running, deliberately: a Delivery is worth colouring the moment
		// *any* of it is done, and asking whether *all* of it is would withhold the view from every
		// Delivery anyone is still working on - which is all of them.
		renderTab(
			[feature({ id: 1, closedDate: october(9) }), feature({ id: 2 })],
			undefined,
		);

		expect(statusButton()).toBeInTheDocument();
	});

	it("falls back to nothing on a Delivery that cannot answer what the reader asked", () => {
		// The choice is one value for the whole page. A reader who asked for the Teams and then
		// opens a Delivery whose Features have one Team each is shown a plain chart rather than
		// something they did not ask for - and their choice survives for the Delivery that can
		// honour it.
		timelineViewStore.set("teams");

		renderTab([feature()], DUE_ON_THE_TWENTIETH);

		expect(ganttProps.current?.lanes).toBeUndefined();
		expect(ganttProps.current?.barStatusColors).toBeUndefined();
		expect(timelineViewStore.read()).toBe("teams");

		// And the control says so rather than standing there with nothing pressed, which is what a
		// fallback to anything but a real option would leave behind.
		expect(screen.getByRole("button", { name: "Nothing" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
	});

	it("keeps the Teams' key and the Teams' colours off a chart showing the status", async () => {
		// Both belong to the Teams and are drawn from the same choice, so a comparison written the
		// wrong way round puts a key for Teams above a chart coloured by status - two colour
		// schemes on screen at once, which is the whole thing the single choice exists to prevent.
		renderTab(
			[
				feature({
					id: 1,
					teamForecasts: [forTeam(5, 12, 15), forTeam(6, 17, 24)],
				}),
			],
			DUE_ON_THE_TWENTIETH,
			[ZENITH, GRAVITY],
		);

		expect(
			screen.queryByTestId("timeline-team-legend"),
		).not.toBeInTheDocument();
		expect(ganttProps.current?.barTeams).toBeUndefined();

		// Paired, so neither assertion passes against a tab that never draws them at all.
		await askForTheTeams();

		expect(screen.getByTestId("timeline-team-legend")).toBeInTheDocument();
		expect(ganttProps.current?.barTeams).toBeDefined();
	});

	it("names each colour beside the chart while the status is being shown", async () => {
		// A colour on a bar says nothing by itself to a reader who has not been told what it
		// means, and with a finished bar and an on-track bar both green it is this that tells them
		// apart. Pinned against the words themselves: mutation testing does not change copy, so a
		// looser match would never be challenged.
		renderTab([feature()], DUE_ON_THE_TWENTIETH);

		const key = screen.getByTestId("timeline-status-legend");

		expect(key).toHaveTextContent("Finished");
		expect(key).toHaveTextContent("Finishes after the target date");
		expect(key).toHaveTextContent("Not even started by the target date");

		await userEvent.click(screen.getByRole("button", { name: "Nothing" }));

		expect(
			screen.queryByTestId("timeline-status-legend"),
		).not.toBeInTheDocument();
	});
});

describe("quieting what each bar has to say", () => {
	const warningsButton = () =>
		screen.queryByRole("button", { name: "Warnings" });

	const markedFeature = () =>
		feature({ id: 1, isUsingDefaultFeatureSize: true });

	it("says nothing on a bar until the reader asks for the warnings", async () => {
		// Both halves. The symbol is unconditional in the version this replaces, so the first is a
		// deliberate change to what an existing reader sees rather than an accident.
		renderTab([markedFeature()]);

		expect(
			within(screen.getByTestId("timeline-bar-1")).queryByTestId(
				"timeline-bar-mark",
			),
		).not.toBeInTheDocument();

		await userEvent.click(warningsButton() as HTMLElement);

		expect(markOn(1)).toBeInTheDocument();
	});

	it("leaves the Feature's own warnings alone in the list opened from a bar", async () => {
		// The column is shown by fifteen other screens that have no timeline, so gating it on a
		// control on this one would quieten it everywhere.
		renderTab([markedFeature()]);

		act(() => ganttProps.current?.onBarSelected?.(1));

		expect(
			within(await screen.findByRole("dialog")).getByText(DEFAULT_SIZE_WARNING),
		).toBeInTheDocument();
	});

	it("withholds the view from a Delivery with nothing to say about anything", () => {
		renderTab([feature()]);

		expect(warningsButton()).not.toBeInTheDocument();
	});

	// A bar is refused for three different reasons, and a Feature refused for any of them can
	// carry no mark at all. Each is listed so that screening one and calling it "placeable" is
	// caught here rather than by a reader clicking a control that does nothing.
	const cannotBeDrawn = [
		{
			why: "a Team of its own has no forecast",
			shape: { teamsWithoutForecast: ["Meridian"] },
		},
		{ why: "nothing says when it begins", shape: { startForecast: undefined } },
		{ why: "nothing says when it ends", shape: { forecasts: [] } },
	];

	it.each(cannotBeDrawn)(
		"withholds the view when the only Feature with anything to say cannot be drawn because $why",
		({ shape }) => {
			renderTab([
				feature({ id: 1 }),
				feature({ id: 2, isUsingDefaultFeatureSize: true, ...shape }),
			]);

			// The chart is rendered, so "no control" cannot pass on a blank tab.
			expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
			expect(warningsButton()).not.toBeInTheDocument();
		},
	);

	it("offers the view for a Feature carrying a dependency there is nothing wrong with", async () => {
		// A sound dependency raises no warning and still puts a note on the bar, so the view has
		// to be offered for it. Paired with the tests above, or those pass against a tab that
		// never offers it.
		renderTab([
			feature({ dependsOn: [waitingOn("OE-9", "Hull Fabrication")] }),
		]);

		expect(warningsButton()).toBeInTheDocument();

		// And it stays put across the probability buttons, for the reason the status view does.
		await userEvent.click(screen.getByRole("button", { name: "95%" }));

		expect(warningsButton()).toBeInTheDocument();
	});
});
