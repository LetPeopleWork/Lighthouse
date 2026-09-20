import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature, IFeatureStart } from "../../../../../../models/Feature";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import DeliveryTimelineTab from "./DeliveryTimelineTab";
import type { TimelineBar } from "./deliveryTimelineModel";

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
const ganttProps = vi.hoisted(() => ({
	current: null as { bars: TimelineBar[]; targetDate?: Date } | null,
}));

vi.mock("./DeliveryGanttChart", () => ({
	default: (props: { bars: TimelineBar[]; targetDate?: Date }) => {
		ganttProps.current = props;
		return <div data-testid="delivery-gantt" />;
	},
}));

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

const renderTab = (features: IFeature[], targetDate?: Date) =>
	render(
		<DeliveryTimelineTab
			features={features}
			targetDate={targetDate}
			featuresTerm="Features"
		/>,
	);

beforeEach(() => {
	licence.isPremium = true;
	licence.isKnown = true;
	ganttProps.current = null;
});

describe("DeliveryTimelineTab", () => {
	it("draws the Features that can be placed", () => {
		renderTab([feature()]);

		expect(screen.getByTestId("delivery-gantt")).toBeInTheDocument();
		expect(ganttProps.current?.bars).toHaveLength(1);
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

	it("hands the Delivery's target date through to the chart", () => {
		const target = october(31);

		renderTab([feature()], target);

		expect(ganttProps.current?.targetDate).toEqual(target);
	});
});
