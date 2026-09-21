import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { TeamLane } from "./deliveryTeamLanes";
import type { TimelineBar } from "./deliveryTimelineModel";
import TimelineBarContent, {
	type BarMark,
	TimelineBarMarks,
} from "./TimelineBarContent";

const bar: TimelineBar = {
	featureId: 7,
	name: "Sonar Refit",
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
};

const quietBar: TimelineBar = {
	...bar,
	featureId: 8,
	name: "Hull Fabrication",
};

const renderBar = (
	props: Partial<{
		bar: TimelineBar;
		lane: TeamLane;
		onSelect: (featureId: number) => void;
	}>,
) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineBarContent {...props} />
		</ThemeProvider>,
	);

const lane = (overrides: Partial<TeamLane> = {}): TeamLane => ({
	featureId: 7,
	teamId: 42,
	teamName: "Zenith",
	start: new Date(2026, 9, 12),
	end: new Date(2026, 9, 18),
	color: "#4DA98C",
	...overrides,
});

const renderMarkedBar = (mark: BarMark) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineBarMarks marks={new Map([[bar.featureId, mark]])}>
				<TimelineBarContent bar={bar} />
			</TimelineBarMarks>
		</ThemeProvider>,
	);

describe("TimelineBarContent", () => {
	it("writes the Feature's name along the bar", () => {
		renderBar({ bar });

		expect(screen.getByTestId("timeline-bar-content")).toHaveTextContent(
			"Sonar Refit",
		);
	});

	it("is a button, and reports which Feature was chosen", async () => {
		const onSelect = vi.fn();

		renderBar({ bar, onSelect });

		await userEvent.click(screen.getByRole("button"));

		expect(onSelect).toHaveBeenCalledExactlyOnceWith(7);
	});

	it("is inert, and offers no pointer, when nothing is listening", () => {
		renderBar({ bar });

		// A bar that looks clickable and is not is worse than one that looks inert: the reader
		// learns it does nothing by trying it.
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
		expect(
			getComputedStyle(screen.getByTestId("timeline-bar-content")).cursor,
		).not.toBe("pointer");
	});

	it("renders nothing for a bar the chart asked for and we do not have", () => {
		// An empty bar would be a nameless clickable box sitting on the timeline.
		renderBar({ bar: undefined, onSelect: vi.fn() });

		expect(
			screen.queryByTestId("timeline-bar-content"),
		).not.toBeInTheDocument();
	});

	it("shows the span on hover, and offers the detail only when there is some", async () => {
		const onSelect = vi.fn();

		renderBar({ bar, onSelect });
		await userEvent.hover(screen.getByRole("button"));

		const tooltip = await screen.findByRole("tooltip");

		expect(tooltip).toHaveTextContent("10/10/2026");
		expect(tooltip).toHaveTextContent("10/20/2026");
		expect(tooltip).toHaveTextContent(/click for more details/i);
	});

	it("raises the warning symbol for a bar carrying a warning", () => {
		renderMarkedBar({
			notes: [
				{
					text: "This Feature and Hull Fabrication are waiting on each other.",
					isWarning: true,
				},
			],
		});

		expect(screen.getByTestId("timeline-bar-mark")).toHaveAccessibleName(
			"Warning. This Feature and Hull Fabrication are waiting on each other.",
		);
	});

	it("marks a bar that only has something to report without calling it a warning", () => {
		renderMarkedBar({
			notes: [
				{
					text: "Waiting on Hull Fabrication, which is not on this timeline.",
					isWarning: false,
				},
			],
		});

		// One symbol serving both kinds would leave the reader unable to tell a dependency that
		// needs them from one that is simply drawn elsewhere.
		expect(screen.getByTestId("timeline-bar-mark")).toHaveAccessibleName(
			"Note. Waiting on Hull Fabrication, which is not on this timeline.",
		);
	});

	it("leaves a bar with nothing to say unmarked, and offers it no all-clear either", () => {
		render(
			<ThemeProvider theme={createTheme()}>
				<TimelineBarMarks
					marks={
						new Map([
							[
								bar.featureId,
								{
									notes: [{ text: "Waiting on something.", isWarning: false }],
								},
							],
						])
					}
				>
					<div data-testid="has-something-to-say">
						<TimelineBarContent bar={bar} />
					</div>
					<div data-testid="has-nothing-to-say">
						<TimelineBarContent bar={quietBar} />
					</div>
				</TimelineBarMarks>
			</ThemeProvider>,
		);

		expect(
			within(screen.getByTestId("has-something-to-say")).getByTestId(
				"timeline-bar-mark",
			),
		).toBeInTheDocument();

		const quietBarElement = screen.getByTestId("has-nothing-to-say");

		expect(
			within(quietBarElement).queryByTestId("timeline-bar-mark"),
		).not.toBeInTheDocument();
		// The Feature table answers "nothing wrong here" with a green check. In a bar a few pixels
		// tall that check competes with the name for the only space there is, and it is shown on
		// every bar that is fine, which is most of them. Asked of the markup rather than by role:
		// a decorative icon is hidden from the accessibility tree and would pass a role query.
		expect(quietBarElement.querySelector("svg")).toBeNull();
	});

	it("reads its notes out on hover, where the symbol alone cannot", async () => {
		renderMarkedBar({
			notes: [
				{
					text: "Waiting on Hull Fabrication, which is not on this timeline.",
					isWarning: false,
				},
			],
		});

		await userEvent.hover(screen.getByTestId("timeline-bar-content"));

		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			"Waiting on Hull Fabrication, which is not on this timeline.",
		);
	});

	it("writes the Team's name along a lane, not the Feature's", () => {
		// The Feature's name and the Team's share no substring here, so a component that keeps
		// drawing what it drew before cannot be rescued by a loose match.
		renderBar({ lane: lane({ teamName: "Zenith" }) });

		const content = screen.getByTestId("timeline-bar-content");

		expect(content).toHaveTextContent("Zenith");
		expect(content).not.toHaveTextContent("Sonar Refit");
	});

	it("opens the Feature a lane belongs to, not the Team", async () => {
		// Both halves. An inert lane beside a clickable bar reads as a defect — a lane is that
		// Feature, for one Team — and the two ids here are different numbers, so reporting the
		// Team's instead of the Feature's fails.
		const onSelect = vi.fn();

		renderBar({ lane: lane({ featureId: 7, teamId: 42 }), onSelect });

		await userEvent.click(screen.getByRole("button"));

		expect(onSelect).toHaveBeenCalledExactlyOnceWith(7);
	});

	it("keeps a Feature's mark on its own bar and off its lanes", () => {
		// The existing lookup is `marks.get(bar.featureId)`, so a lane arriving with its
		// Feature's id would wear the same symbol — a three-lane Feature carrying it four times.
		// Both halves, because the absence alone passes against a component that draws nothing.
		render(
			<ThemeProvider theme={createTheme()}>
				<TimelineBarMarks
					marks={
						new Map([
							[
								bar.featureId,
								{
									notes: [{ text: "Waiting on something.", isWarning: false }],
								},
							],
						])
					}
				>
					<div data-testid="the-feature">
						<TimelineBarContent bar={bar} />
					</div>
					<div data-testid="its-lane">
						<TimelineBarContent lane={lane({ featureId: bar.featureId })} />
					</div>
				</TimelineBarMarks>
			</ThemeProvider>,
		);

		expect(
			within(screen.getByTestId("the-feature")).getByTestId(
				"timeline-bar-mark",
			),
		).toBeInTheDocument();
		expect(
			within(screen.getByTestId("its-lane")).queryByTestId("timeline-bar-mark"),
		).not.toBeInTheDocument();
		// Paired with the lane actually drawing something. On its own, "the lane has no mark" is
		// satisfied by a component that renders no lane at all.
		expect(
			within(screen.getByTestId("its-lane")).getByTestId(
				"timeline-bar-content",
			),
		).toHaveTextContent("Zenith");
	});

	it("paints two Teams' lanes differently", () => {
		// Asserted as a difference between the two, never against a colour value: "all of them
		// identical" passes when every one of them is absent, and this project has paid for that
		// once already.
		//
		// And asserted on the class each row was given rather than on its computed fill, because
		// this environment does not resolve the styles it injects: `getComputedStyle` answers
		// transparent for both of these, which is the same answer it would give if neither were
		// painted at all. The generated class is what does differ, and it differs only because the
		// two rows were styled differently — two rows painted alike share one class. Whether the
		// colours are distinguishable to a reader is the live check's business.
		render(
			<ThemeProvider theme={createTheme()}>
				<div data-testid="first-lane">
					<TimelineBarContent lane={lane({ teamId: 1, color: "#4DA98C" })} />
				</div>
				<div data-testid="second-lane">
					<TimelineBarContent lane={lane({ teamId: 2, color: "#6BA3F5" })} />
				</div>
			</ThemeProvider>,
		);

		const paintingOf = (testId: string) =>
			within(screen.getByTestId(testId)).getByTestId("timeline-bar-content")
				.className;

		expect(
			new Set([paintingOf("first-lane"), paintingOf("second-lane")]).size,
		).toBe(2);
	});

	it("names a Team with no lane on the bar before anyone hovers or clicks", () => {
		// Naming that lives only in the tooltip fails this — its content is not rendered until a
		// hover — and so does a sentence reporting a count: "1 Team without a forecast" says
		// something while defeating the point, which is that a Team is never quietly missing.
		renderMarkedBar({
			notes: [
				{
					text: "No forecast for Meridian, so it has no lane of its own.",
					isWarning: false,
				},
			],
			namesOnTheBar: ["Meridian"],
		});

		// Matched exactly, and not as a substring of the bar's whole text: the symbol already
		// carries the sentence in an accessible title nobody can see, so a containment check passes
		// against a name that is only ever read out.
		expect(
			within(screen.getByTestId("timeline-bar-content")).getByText("Meridian"),
		).toBeInTheDocument();
	});
});
