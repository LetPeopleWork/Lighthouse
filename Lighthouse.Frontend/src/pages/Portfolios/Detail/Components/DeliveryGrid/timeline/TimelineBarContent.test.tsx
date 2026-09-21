import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
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
	props: Partial<{ bar: TimelineBar; onSelect: () => void }>,
) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineBarContent {...props} />
		</ThemeProvider>,
	);

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
});
