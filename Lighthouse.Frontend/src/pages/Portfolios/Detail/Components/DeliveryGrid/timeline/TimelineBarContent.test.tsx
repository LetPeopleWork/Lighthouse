import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { BarMark } from "./deliveryBarMarks";
import type { TeamColour, TeamLane } from "./deliveryTeamLanes";
import type { TimelineBar } from "./deliveryTimelineModel";
import TimelineBarContent, {
	rowTextColour,
	TimelineBarMarks,
} from "./TimelineBarContent";

const bar: TimelineBar = {
	featureId: 7,
	name: "Sonar Refit",
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
	endIsObserved: false,
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
		team: TeamColour;
		statusColor: string;
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

describe("what a row writes over its fill", () => {
	// Two colours a Team can actually be given. The whole Team palette is light - the colours are
	// chosen to be told apart against a dark background - so the chart's white over any of them is
	// what made a Team's name unreadable.
	//
	// What cannot be checked here is whether the result is comfortable to read: this environment
	// applies no styles and has nothing to measure a contrast ratio with, so legibility is a
	// question for someone looking at the chart. What can be checked is that no fill a Team wears
	// is given the white the bars use, which is the part that was wrong.
	const PALE_LIME = "#C5E06E";
	const DEEP_CYAN = "#2AA0B5";
	const THE_CHARTS_WHITE = "#ffffff";

	it("never writes the chart's white over a colour a Team can wear", () => {
		expect(rowTextColour(PALE_LIME)).not.toBe(THE_CHARTS_WHITE);
		expect(rowTextColour(DEEP_CYAN)).not.toBe(THE_CHARTS_WHITE);
	});

	it("still writes light over a fill dark enough to need it", () => {
		// Paired with the row above, so neither can pass against a function that answers one
		// colour whatever it is handed.
		expect(rowTextColour("#000000")).toBe(THE_CHARTS_WHITE);
	});

	it("leaves a bar with no fill of its own to the colour the chart sets", () => {
		// A Feature's own bar is painted by the chart in the product's colour, with a text colour
		// chosen once against it. Only the rows that wear a Team's colour have to work it out.
		expect(rowTextColour(undefined)).toBe("inherit");
	});
});

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
					text: "No forecast for Meridian, so it is not shown separately.",
					isWarning: false,
				},
			],
			namesOnTheBar: [{ teamId: 7, name: "Meridian" }],
		});

		// Matched exactly, and not as a substring of the bar's whole text: the symbol already
		// carries the sentence in an accessible title nobody can see, so a containment check passes
		// against a name that is only ever read out.
		expect(
			within(screen.getByTestId("timeline-bar-content")).getByText("Meridian"),
		).toBeInTheDocument();
	});

	it("writes both un-laned Teams on the bar when neither of them can be named", async () => {
		// Two Teams, one phrase between them.
		//
		// **React does not drop the second of two children sharing a key** - it renders both and
		// complains. So counting the entries proves the list was built from the Teams rather than
		// collapsed by name, and it says nothing about the key: this test passed unchanged with
		// the names used as keys. The key is pinned by the complaint instead, below, because that
		// is the only thing a duplicate actually changes here.
		const complaints = vi.spyOn(console, "error").mockImplementation(() => {});

		const outsider = "A Team from outside this Portfolio";
		const sentence = `No forecast for ${outsider}.`;

		renderMarkedBar({
			notes: [
				{ text: sentence, isWarning: false, subject: "team:404" },
				{ text: sentence, isWarning: false, subject: "team:405" },
			],
			namesOnTheBar: [
				{ teamId: 404, name: outsider },
				{ teamId: 405, name: outsider },
			],
		});

		expect(
			within(screen.getByTestId("timeline-bar-content")).getAllByText(outsider),
		).toHaveLength(2);

		// And the same again in the hover text, which is its own list with its own keys.
		await userEvent.hover(screen.getByTestId("timeline-bar-content"));

		const tooltip = await screen.findByRole("tooltip");

		expect(within(tooltip).getAllByText(sentence)).toHaveLength(2);

		// Nothing was keyed on the words. Two entries sharing a key reconcile as one on the next
		// update, which is invisible on a first render and shows up later as a name that will not
		// change when the Team behind it does.
		// Read across every argument of every call rather than matched against one shape: React
		// splits this message over a format string and its substitutions, and how many there are
		// is theirs to change.
		expect(complaints.mock.calls.flat().join(" ")).not.toContain("same key");

		complaints.mockRestore();
	});

	it("names the one Team a Feature has to itself along its own bar", () => {
		// Such a Feature never gets a row of its own, so if its bar does not say which Team it is,
		// nothing on the chart does - which is half the chart answering the switch and half not.
		renderBar({
			bar,
			team: { teamId: 42, teamName: "Meridian", color: "#4DA98C" },
		});

		const content = screen.getByTestId("timeline-bar-content");

		// The Feature's own name stays: the Team is added to the bar, it does not replace it. The
		// Team is matched exactly rather than as a substring, so a name only ever read out to a
		// screen reader would not satisfy it.
		expect(content).toHaveTextContent("Sonar Refit");
		expect(within(content).getByText("Meridian")).toBeInTheDocument();
	});

	it("paints a Feature with one Team differently from one with several", () => {
		// A bar wearing its Team's colour and a bar keeping the default is how a reader tells "this
		// one Team" from "several Teams" at a glance. Asserted as a difference rather than against
		// a colour, for the same reason as the rows: this environment resolves neither.
		render(
			<ThemeProvider theme={createTheme()}>
				<div data-testid="one-team">
					<TimelineBarContent
						bar={bar}
						team={{ teamId: 42, teamName: "Meridian", color: "#4DA98C" }}
					/>
				</div>
				<div data-testid="several-teams">
					<TimelineBarContent bar={quietBar} />
				</div>
			</ThemeProvider>,
		);

		const paintingOf = (testId: string) =>
			within(screen.getByTestId(testId)).getByTestId("timeline-bar-content")
				.className;

		expect(
			new Set([paintingOf("one-team"), paintingOf("several-teams")]).size,
		).toBe(2);
	});
});

describe("what a bar says about the target date", () => {
	const AMBER = "#ff9800";
	const MERIDIAN: TeamColour = {
		teamId: 42,
		teamName: "Meridian",
		color: "#4DA98C",
	};

	const paintingOf = (testId: string) =>
		within(screen.getByTestId(testId)).getByTestId("timeline-bar-content")
			.className;

	it("wears the colour it is given, and nothing when it is given none", () => {
		// Read as a difference in painting rather than as a computed background: the row declares
		// `background: none` before its colour and this environment resolves that pair to
		// transparent, so a computed fill answers the same for a painted bar and an unpainted one.
		// Two bars painted differently are styled differently and get different classes.
		render(
			<ThemeProvider theme={createTheme()}>
				<div data-testid="late">
					<TimelineBarContent bar={bar} statusColor={AMBER} />
				</div>
				<div data-testid="on-track">
					<TimelineBarContent bar={quietBar} />
				</div>
			</ThemeProvider>,
		);

		expect(paintingOf("late")).not.toBe(paintingOf("on-track"));
	});

	it("never colours a Team's own row for the Feature's status", () => {
		// The lane and the bar share a renderer, so threading the status into both is one line and
		// the natural mistake. Asserted against a bar that does wear it and one that wears nothing,
		// so it cannot pass against a component that colours nothing at all.
		render(
			<ThemeProvider theme={createTheme()}>
				<div data-testid="the-feature">
					<TimelineBarContent bar={bar} statusColor={AMBER} />
				</div>
				<div data-testid="one-of-its-teams">
					<TimelineBarContent lane={lane()} statusColor={AMBER} />
				</div>
				<div data-testid="a-plain-bar">
					<TimelineBarContent bar={quietBar} />
				</div>
			</ThemeProvider>,
		);

		expect(paintingOf("the-feature")).not.toBe(paintingOf("a-plain-bar"));
		expect(paintingOf("one-of-its-teams")).not.toBe(paintingOf("the-feature"));
	});

	it("lets the Team's colour win, because the reader asked for one thing at a time", () => {
		// The two never arrive together from the tab - the reader picks one view - but the
		// component is handed both here so the rule is written down somewhere rather than resting
		// on every caller being careful.
		//
		// **Asserted on the painting, not on the Team's name.** The name is rendered by a branch
		// that never looks at the fill, so a test reading it is blind to the precedence it is named
		// for: inverting the rule to `statusColor ?? team?.color` left all 272 tests green.
		render(
			<ThemeProvider theme={createTheme()}>
				<div data-testid="both">
					<TimelineBarContent bar={bar} team={MERIDIAN} statusColor={AMBER} />
				</div>
				<div data-testid="the-Team-alone">
					<TimelineBarContent bar={quietBar} team={MERIDIAN} />
				</div>
				<div data-testid="the-status-alone">
					<TimelineBarContent bar={quietBar} statusColor={AMBER} />
				</div>
			</ThemeProvider>,
		);

		expect(paintingOf("both")).toBe(paintingOf("the-Team-alone"));
		expect(paintingOf("both")).not.toBe(paintingOf("the-status-alone"));
	});

	it("promises a Team's row nothing it cannot do", async () => {
		// A lane opens the same Feature its bar opens, and says so on hover - but only where there
		// is something listening. A row that offers the click and does not take it teaches the
		// reader it is broken.
		const onSelect = vi.fn();

		renderBar({ lane: lane(), onSelect });

		await userEvent.click(screen.getByRole("button"));

		expect(onSelect).toHaveBeenCalledExactlyOnceWith(7);
	});

	it("offers a Team's row no click when nothing is listening", () => {
		// Paired with the row above, so neither passes against a component that treats every lane
		// the same way.
		renderBar({ lane: lane() });

		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it.each([
		{ what: "a Feature's bar", props: { bar } },
		{ what: "a Team's row", props: { lane: lane() } },
	])(
		"promises a click on hover only where there is one, on $what",
		async ({ props }) => {
			// The hover offers "click for more details", and both rows work that out separately from
			// whether they are actually clickable. A row that says it and does nothing teaches the
			// reader the chart is broken; one that stays silent hides the dialog entirely.
			const { unmount } = renderBar({ ...props, onSelect: vi.fn() });

			await userEvent.hover(screen.getByTestId("timeline-bar-content"));
			expect(await screen.findByRole("tooltip")).toHaveTextContent(
				/click for more details/i,
			);

			unmount();

			renderBar(props);

			await userEvent.hover(screen.getByTestId("timeline-bar-content"));
			expect(await screen.findByRole("tooltip")).not.toHaveTextContent(
				/click for more details/i,
			);
		},
	);

	it("stays clickable once it is marked", async () => {
		// A cap drawn as an element over the button would swallow the click, and the reader would
		// lose the dialog with nothing anywhere to say why.
		const onSelect = vi.fn();

		renderBar({ bar, statusColor: "#f44336", onSelect });

		await userEvent.click(screen.getByRole("button"));

		expect(onSelect).toHaveBeenCalledExactlyOnceWith(7);
	});
});
