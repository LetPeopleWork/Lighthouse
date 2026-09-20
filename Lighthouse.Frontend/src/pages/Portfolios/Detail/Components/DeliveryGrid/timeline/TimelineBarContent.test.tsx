import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { TimelineBar } from "./deliveryTimelineModel";
import TimelineBarContent from "./TimelineBarContent";

const bar: TimelineBar = {
	featureId: 7,
	name: "Sonar Refit",
	start: new Date(2026, 9, 10),
	end: new Date(2026, 9, 20),
	startIsObserved: false,
};

const renderBar = (
	props: Partial<{ bar: TimelineBar; onSelect: () => void }>,
) =>
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineBarContent {...props} />
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
});
