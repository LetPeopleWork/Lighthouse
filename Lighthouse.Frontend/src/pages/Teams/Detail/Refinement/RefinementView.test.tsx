import { render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { IRefinementService } from "../../../../services/Api/RefinementService";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import RefinementView from "./RefinementView";

/**
 * What the coach reads on the Refinement tab: a heading counting the Work Items in refinement, and one
 * row per Work Item in the order the server answered - which is backlog order, worked out there, so the
 * tab must not reorder it. Each row links to the tracker and says the state and its category; a Doing
 * row says how old the Work Item is, a To Do row does not. Every renameable word comes from Terminology.
 */

const { terms } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
}));

vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

// Spelled out: the keys join the shared list together with the words the server seeds for them.
const REFINEMENT_KEY = "refinement";
const REFINEMENTS_KEY = "refinements";

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEM]: "Work Item",
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.WORK_ITEM_AGE]: "Work Item Age",
	[REFINEMENT_KEY]: "Refinement",
	[REFINEMENTS_KEY]: "Refinements",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
};

const aRow = (
	referenceId: string,
	name: string,
	state: string,
	workItemAge: number | null,
): IRefinementRow => ({
	referenceId,
	name,
	url: `https://tracker.example/browse/${referenceId}`,
	state,
	stateCategory: workItemAge === null ? "ToDo" : "Doing",
	workItemAge,
});

const gravitysRefinement: IRefinementView = {
	refinementConfigured: true,
	workItems: [
		aRow("GR-058", "User activity tracking", "Next", 2),
		aRow("GR-059", "Advanced search filters", "Next", 3),
		aRow("GR-051", "Advanced reporting module", "Analysing", 4),
		aRow("GR-073", "Configuration management", "Backlog", null),
	],
};

const teamGravity = () => {
	const team = new Team();
	team.id = 7;
	team.name = "Team Gravity";
	return team;
};

const renderTheRefinementTab = (answer: IRefinementView) => {
	const refinementService: IRefinementService = {
		getRefinement: vi.fn().mockResolvedValue(answer),
	};

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ refinementService })}
		>
			<RefinementView team={teamGravity()} />
		</ApiServiceContext.Provider>,
	);

	return { refinementService };
};

const theRowOf = async (referenceId: string) => {
	const link = await screen.findByRole("link", { name: referenceId });
	const row = link.closest('[role="row"], tr');
	if (row === null) {
		throw new Error(`${referenceId} is not shown inside a row`);
	}
	return row as HTMLElement;
};

const theListedReferenceIds = () =>
	screen
		.getAllByRole("link")
		.map((link) => link.textContent?.trim())
		.filter((text) => text?.startsWith("GR-"));

describe("The Refinement tab lists the Work Items in refinement", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
	});

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it("asks for this Team's refinement and counts what it holds in the heading", async () => {
		const { refinementService } = renderTheRefinementTab(gravitysRefinement);

		expect(
			await screen.findByRole("heading", {
				name: "4 Work Items in Refinement",
			}),
		).toBeVisible();
		expect(refinementService.getRefinement).toHaveBeenCalledWith(7);
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it("shows the Work Items in the order they came, which is backlog order", async () => {
		renderTheRefinementTab(gravitysRefinement);

		await screen.findByRole("link", { name: "GR-058" });

		expect(theListedReferenceIds()).toEqual([
			"GR-058",
			"GR-059",
			"GR-051",
			"GR-073",
		]);
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it("links each Work Item to the tracker and names it, its state and its category", async () => {
		renderTheRefinementTab(gravitysRefinement);

		const analysing = await theRowOf("GR-051");
		const backlog = await theRowOf("GR-073");

		expect(
			within(analysing).getByRole("link", { name: "GR-051" }),
		).toHaveAttribute("href", "https://tracker.example/browse/GR-051");
		expect(analysing).toHaveTextContent("Advanced reporting module");
		expect(analysing).toHaveTextContent("Analysing");
		expect(analysing).toHaveTextContent("Doing");
		expect(backlog).toHaveTextContent("Configuration management");
		expect(backlog).toHaveTextContent("Backlog");
		expect(backlog).toHaveTextContent("To Do");
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it("says how old a Doing Work Item is and gives a To Do Work Item no age", async () => {
		renderTheRefinementTab(gravitysRefinement);

		const analysing = await theRowOf("GR-051");
		const backlog = await theRowOf("GR-073");

		expect(analysing).toHaveTextContent("4 days");
		expect(backlog).not.toHaveTextContent(/days?/);
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it("counts a single Work Item in the singular", async () => {
		renderTheRefinementTab({
			refinementConfigured: true,
			workItems: [aRow("GR-073", "Configuration management", "Backlog", null)],
		});

		expect(
			await screen.findByRole("heading", {
				name: "1 Work Item in Refinement",
			}),
		).toBeVisible();
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it("states that nothing is in refinement right now instead of showing an empty list or an error", async () => {
		renderTheRefinementTab({ refinementConfigured: true, workItems: [] });

		expect(
			await screen.findByText("No Work Items in Refinement states right now"),
		).toBeVisible();
		expect(screen.queryByRole("alert")).toBeNull();
		expect(screen.queryByRole("link")).toBeNull();
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it("says the Team's own words in the heading and in the empty state", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			[TERMINOLOGY_KEYS.WORK_ITEM_AGE]: "Ticket Age",
			[REFINEMENT_KEY]: "Replenishment",
			[REFINEMENTS_KEY]: "Replenishments",
		};
		renderTheRefinementTab(gravitysRefinement);

		expect(
			await screen.findByRole("heading", {
				name: "4 Tickets in Replenishment",
			}),
		).toBeVisible();
		expect(screen.queryByText(/refinement/i)).toBeNull();
		expect(screen.queryByText(/work items?/i)).toBeNull();
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it("says the Team's own words when nothing is in refinement", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			[REFINEMENT_KEY]: "Replenishment",
		};
		renderTheRefinementTab({ refinementConfigured: true, workItems: [] });

		expect(
			await screen.findByText("No Tickets in Replenishment states right now"),
		).toBeVisible();
	});

	// @us-02 @slice-02 @boundary @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function
	// Three hundred is the size of a whole backlog sitting in one To Do state; the heading has to count all
	// of them and the list has to start where the backlog starts, whatever the list does to stay fast.
	it("handles three hundred Work Items, counting all of them and starting at the top of the backlog", async () => {
		renderTheRefinementTab({
			refinementConfigured: true,
			workItems: Array.from({ length: 300 }, (_, index) =>
				aRow(
					`GR-${1000 + index}`,
					`Refinement candidate ${index}`,
					"Backlog",
					null,
				),
			),
		});

		expect(
			await screen.findByRole("heading", {
				name: "300 Work Items in Refinement",
			}),
		).toBeVisible();
		expect(theListedReferenceIds()[0]).toBe("GR-1000");
	});
});
