import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import TimelineControls, { type TimelineViewOption } from "./TimelineControls";
import type { TimelineView } from "./timelineView";

const EVERY_VIEW: TimelineViewOption[] = [
	{ view: "none", label: "Nothing", offered: true },
	{ view: "teams", label: "Teams", offered: true },
	{ view: "status", label: "Status", offered: true },
	{ view: "warnings", label: "Warnings", offered: true },
];

const renderControls = (
	views: TimelineViewOption[] = EVERY_VIEW,
	view: TimelineView = "status",
) => {
	const onViewChosen = vi.fn();
	const onPercentileChosen = vi.fn();

	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineControls
				percentile={70}
				onPercentileChosen={onPercentileChosen}
				view={view}
				onViewChosen={onViewChosen}
				views={views}
			/>
		</ThemeProvider>,
	);

	return { onViewChosen, onPercentileChosen };
};

describe("the row above the chart", () => {
	it("offers only the views this Delivery can answer", () => {
		// Both halves. A row that drew every view it was handed passes the first; one that drew
		// none passes the second.
		renderControls([
			{ view: "none", label: "Nothing", offered: true },
			{ view: "status", label: "Status", offered: true },
			{ view: "teams", label: "Teams", offered: false },
		]);

		expect(screen.getByRole("button", { name: "Status" })).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Teams" }),
		).not.toBeInTheDocument();
	});

	it("shows which view is in force", () => {
		renderControls(EVERY_VIEW, "teams");

		expect(screen.getByRole("button", { name: "Teams" })).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		// Paired, or it passes against a row that presses everything.
		expect(screen.getByRole("button", { name: "Status" })).toHaveAttribute(
			"aria-pressed",
			"false",
		);
	});

	it("reports the view the reader chose", async () => {
		const { onViewChosen } = renderControls();

		await userEvent.click(screen.getByRole("button", { name: "Teams" }));

		expect(onViewChosen).toHaveBeenCalledExactlyOnceWith("teams");
	});

	it("keeps the view when its own button is clicked again", async () => {
		// The group reports null when the active choice is clicked. Nothing is already a button of
		// its own, so emptying the group by accident would leave it with no answer selected while
		// the chart still showed one.
		const { onViewChosen } = renderControls(EVERY_VIEW, "status");

		await userEvent.click(screen.getByRole("button", { name: "Status" }));

		expect(onViewChosen).not.toHaveBeenCalled();
	});

	it("withholds the whole group where there is no choice to make", () => {
		// With only "Nothing" on offer the group is one button saying nothing happens - which is
		// not a control, it is a statement dressed as one. Paired with the row above, so this
		// cannot pass against a component that never draws the group.
		renderControls([
			{ view: "none", label: "Nothing", offered: true },
			{ view: "teams", label: "Teams", offered: false },
			{ view: "status", label: "Status", offered: false },
			{ view: "warnings", label: "Warnings", offered: false },
		]);

		expect(
			screen.queryByRole("button", { name: "Nothing" }),
		).not.toBeInTheDocument();
		// The probability group is untouched by any of this.
		expect(screen.getByRole("button", { name: "70%" })).toBeInTheDocument();
	});

	it("reports the probability the reader chose", async () => {
		const { onPercentileChosen } = renderControls();

		await userEvent.click(screen.getByRole("button", { name: "95%" }));

		expect(onPercentileChosen).toHaveBeenCalledExactlyOnceWith(95);
	});

	it("keeps a probability selected when its own button is clicked again", async () => {
		// A timeline with no probability selected has nothing to draw, so the choice stands and
		// nothing is reported.
		const { onPercentileChosen } = renderControls();

		await userEvent.click(screen.getByRole("button", { name: "70%" }));

		expect(onPercentileChosen).not.toHaveBeenCalled();
	});
});
