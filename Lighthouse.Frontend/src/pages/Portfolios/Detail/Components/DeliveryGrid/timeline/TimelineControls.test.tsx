import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import TimelineControls, { type TimelineToggle } from "./TimelineControls";

const aToggle = (overrides: Partial<TimelineToggle> = {}): TimelineToggle => ({
	id: "teams",
	label: "Show Teams",
	offered: true,
	shown: false,
	toggle: vi.fn(),
	...overrides,
});

const renderControls = (toggles: TimelineToggle[], onChosen = vi.fn()) => {
	render(
		<ThemeProvider theme={createTheme()}>
			<TimelineControls
				percentile={70}
				onPercentileChosen={onChosen}
				toggles={toggles}
			/>
		</ThemeProvider>,
	);

	return onChosen;
};

describe("the row above the chart", () => {
	it("offers only the choices this Delivery can act on", () => {
		// Both halves. A row that drew every toggle it was handed passes the first; one that drew
		// none passes the second.
		renderControls([
			aToggle({ id: "teams", label: "Show Teams", offered: true }),
			aToggle({ id: "status", label: "Show status", offered: false }),
		]);

		expect(
			screen.getByRole("switch", { name: "Show Teams" }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("switch", { name: "Show status" }),
		).not.toBeInTheDocument();
	});

	it("draws no divider when there is nothing to set apart from the buttons", () => {
		// The divider separates two kinds of control. With no switch offered there is only one
		// kind, and a rule hanging off the end of the buttons is a line to nowhere.
		//
		// Asked for by role rather than by tag. A vertical MUI divider is not an `hr`, so a query
		// for one answers "none" whether the divider is there or not - which is the shape of
		// assertion that passes against anything, and it is why the row below is written at all.
		renderControls([aToggle({ offered: false })]);

		expect(screen.queryAllByRole("separator")).toHaveLength(0);
	});

	it("draws one divider once something is offered, not one per switch", () => {
		renderControls([
			aToggle({ id: "teams", label: "Show Teams" }),
			aToggle({ id: "status", label: "Show status" }),
		]);

		expect(screen.queryAllByRole("separator")).toHaveLength(1);
	});

	it("reports the probability the reader chose", async () => {
		const onChosen = renderControls([aToggle()]);

		await userEvent.click(screen.getByRole("button", { name: "95%" }));

		expect(onChosen).toHaveBeenCalledExactlyOnceWith(95);
	});

	it("keeps a probability selected when its own button is clicked again", async () => {
		// The group reports null when the active choice is clicked. A timeline with no probability
		// selected has nothing to draw, so the choice stands and nothing is reported.
		const onChosen = renderControls([aToggle()]);

		await userEvent.click(screen.getByRole("button", { name: "70%" }));

		expect(onChosen).not.toHaveBeenCalled();
	});

	it("moves the switch the reader clicked, and only that one", async () => {
		const teams = vi.fn();
		const status = vi.fn();

		renderControls([
			aToggle({ id: "teams", label: "Show Teams", toggle: teams }),
			aToggle({ id: "status", label: "Show status", toggle: status }),
		]);

		await userEvent.click(screen.getByRole("switch", { name: "Show status" }));

		expect(status).toHaveBeenCalledTimes(1);
		expect(teams).not.toHaveBeenCalled();
	});
});
