import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import WidgetShell from "./WidgetShell";

const inProgressItem = (overrides?: Partial<IWorkItem>): IWorkItem => ({
	id: 1,
	name: "Work item in progress",
	state: "In Progress",
	stateCategory: "Doing" as StateCategory,
	type: "Story",
	referenceId: "WIP-1",
	url: "https://example.com/work/1",
	startedDate: new Date("2026-05-20"),
	closedDate: new Date("2026-05-25"),
	cycleTime: 0,
	workItemAge: 5,
	parentWorkItemReference: "",
	isBlocked: false,
	currentStateEnteredAt: new Date("2026-05-20T00:00:00Z"),
	...overrides,
});

describe("WidgetShell WIP-overview view-data wiring", () => {
	it("renders the Time in State column in the dialog when viewData carries timeInStateColumn", async () => {
		const user = userEvent.setup();
		render(
			<WidgetShell
				widgetKey="wipOverview"
				viewData={{
					title: "Team in Progress",
					items: [inProgressItem()],
					highlightColumn: {
						title: "Work Item Age",
						description: "days",
						valueGetter: (item) => item.workItemAge,
					},
					timeInStateColumn: {},
				}}
			>
				<div>Content</div>
			</WidgetShell>,
		);

		await user.click(screen.getByTestId("widget-view-data-wipOverview"));

		expect(
			screen.getByRole("columnheader", { name: /Time in State/ }),
		).toBeInTheDocument();
	});
});
