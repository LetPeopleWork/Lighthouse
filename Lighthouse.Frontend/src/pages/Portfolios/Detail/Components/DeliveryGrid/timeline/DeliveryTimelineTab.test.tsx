import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature, IFeatureStart } from "../../../../../../models/Feature";
import type {
	IFeatureDependency,
	NotHonouredReason,
} from "../../../../../../models/FeatureDependency";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import { TERMINOLOGY_KEYS } from "../../../../../../models/TerminologyKeys";
import DeliveryTimelineTab from "./DeliveryTimelineTab";
import type { DrawnDependency } from "./deliveryDependencyOverlay";
import type { TimelineBar } from "./deliveryTimelineModel";
import TimelineBarContent from "./TimelineBarContent";

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

const renderTab = (features: IFeature[], targetDate?: Date) =>
	render(
		<DeliveryTimelineTab
			features={features}
			targetDate={targetDate}
			featuresTerm="Features"
		/>,
	);

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
		renderTab([
			feature({ id: 1, name: "Sonar Refit", isUsingDefaultFeatureSize: true }),
		]);

		// Nothing here is about dependencies, and the reader is looking at the same Feature the
		// table marks. A bar that stays blank is the two screens disagreeing about it.
		expect(markOn(1)).toHaveAccessibleName(`Warning. ${DEFAULT_SIZE_WARNING}`);
	});

	it("warns on the bar of a Feature marked done with work still left", () => {
		renderTab([
			feature({
				id: 1,
				name: "Sonar Refit",
				stateCategory: "Done",
				getRemainingWorkForFeature: () => 3,
			}),
		]);

		expect(markOn(1)).toHaveAccessibleName(
			/^Warning\. This feature is marked as done/,
		);
	});

	it("marks, without alarm, a bar whose only note is where its blocker went", () => {
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
		renderTab([feature({ id: 1, name: "Sonar Refit" })]);

		const bar = within(screen.getByTestId("timeline-bar-1"));

		expect(bar.queryByTestId("timeline-bar-mark")).not.toBeInTheDocument();
		// A green check on every bar that is fine would sit on most of them, competing with the
		// name for the only space a bar a few pixels tall has.
		expect(bar.queryByRole("img")).not.toBeInTheDocument();
	});

	it("explains a warning that has nothing to do with dependencies on hover", async () => {
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
		terminology.overrides = { [TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets" };

		renderTab([
			feature({ id: 1, name: "Sonar Refit", isUsingDefaultFeatureSize: true }),
		]);

		expect(markOn(1)).toHaveAccessibleName(
			"Warning. No child Tickets were found for this Feature. The remaining Tickets displayed are based on the default Feature size specified in the advanced project settings.",
		);
	});
});
