import { render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IFeature } from "../../../../models/Feature";
import type {
	IRefinementRow,
	IRefinementView,
	IYardstick,
} from "../../../../models/Refinement/Refinement";
import { Team } from "../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { IRefinementService } from "../../../../services/Api/RefinementService";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../../tests/MockApiServiceProvider";
import RefinementView from "./RefinementView";

/**
 * What the coach reads on the Refinement tab: a heading counting the Work Items in refinement, and a
 * grid with one row per Work Item in the order the server answered - which is backlog order, worked
 * out there, so the tab must not reorder it. Each row names the Work Item as one link to the tracker,
 * shows its parent and its state, and nothing about its age or category. Every renameable word comes
 * from Terminology.
 */

const { terms, mockUseLicenseRestrictions } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
	mockUseLicenseRestrictions: vi.fn(),
}));

vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
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

// The server always says what votes are cast against. Without a number the vote column says no "days",
// which a case about the columns could otherwise mistake for an age.
const NOTHING_TO_GO_ON: IYardstick = {
	source: "Unavailable",
	days: null,
	probability: null,
};

const aRow = (
	referenceId: string,
	name: string,
	state: string,
	parentReferenceId = "",
): IRefinementRow => ({
	referenceId,
	name,
	url: `https://tracker.example/browse/${referenceId}`,
	state,
	parentReferenceId,
});

const gravitysRefinement: IRefinementView = {
	refinementConfigured: true,
	yardstick: NOTHING_TO_GO_ON,
	workItems: [
		aRow("GR-058", "User activity tracking", "Next", "GR-010"),
		aRow("GR-059", "Advanced search filters", "Next", "GR-010"),
		aRow("GR-051", "Advanced reporting module", "Analysing", "GR-010"),
		aRow("GR-073", "Configuration management", "Backlog"),
	],
};

const payments = {
	referenceId: "GR-010",
	name: "Payments",
	url: "https://tracker.example/browse/GR-010",
} as IFeature;

const teamGravity = () => {
	const team = new Team();
	team.id = 7;
	team.name = "Team Gravity";
	return team;
};

const renderTheRefinementTab = (answer: IRefinementView | Error) => {
	const refinementService: IRefinementService = {
		getRefinement:
			answer instanceof Error
				? vi.fn().mockRejectedValue(answer)
				: vi.fn().mockResolvedValue(answer),
	};
	const featureService = createMockFeatureService();
	featureService.getFeaturesByReferences = vi
		.fn()
		.mockResolvedValue([payments]);

	render(
		<SnackbarErrorHandler>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					refinementService,
					featureService,
				})}
			>
				<RefinementView team={teamGravity()} />
			</ApiServiceContext.Provider>
		</SnackbarErrorHandler>,
	);

	return { refinementService, featureService };
};

const theGridRows = () =>
	screen
		.getAllByRole("row")
		.filter((row) => within(row).queryAllByRole("gridcell").length > 0);

const theRowOf = async (referenceId: string) => {
	const name = await screen.findByText(new RegExp(`^${referenceId}: `));
	const row = name.closest('[role="row"]');
	if (row === null) {
		throw new Error(`${referenceId} is not shown inside a grid row`);
	}
	return row as HTMLElement;
};

const theListedReferenceIds = () =>
	theGridRows().map(
		(row) => within(row).getAllByRole("link")[0].textContent?.split(":")[0],
	);

describe("The Refinement tab lists the Work Items in refinement", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultTerms };
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it("asks for this Team's refinement and counts what it holds in the heading above a grid", async () => {
		const { refinementService } = renderTheRefinementTab(gravitysRefinement);

		expect(
			await screen.findByRole("heading", {
				name: "4 Work Items in Refinement",
			}),
		).toBeVisible();
		expect(refinementService.getRefinement).toHaveBeenCalledWith(7);
		expect(screen.getByRole("grid")).toBeInTheDocument();
		expect(
			screen.getByRole("columnheader", { name: "Work Item Name" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("columnheader", { name: "Parent" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("columnheader", { name: "State" }),
		).toBeInTheDocument();
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it("shows the Work Items in the order they came, which is backlog order", async () => {
		renderTheRefinementTab(gravitysRefinement);

		await theRowOf("GR-058");

		expect(theListedReferenceIds()).toEqual([
			"GR-058",
			"GR-059",
			"GR-051",
			"GR-073",
		]);
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it("names each Work Item as one link to the tracker and shows its state", async () => {
		renderTheRefinementTab(gravitysRefinement);

		const analysing = await theRowOf("GR-051");
		const backlog = await theRowOf("GR-073");

		expect(
			within(analysing).getByRole("link", {
				name: "GR-051: Advanced reporting module",
			}),
		).toHaveAttribute("href", "https://tracker.example/browse/GR-051");
		expect(analysing).toHaveTextContent("Analysing");
		expect(
			within(backlog).getByRole("link", {
				name: "GR-073: Configuration management",
			}),
		).toHaveAttribute("href", "https://tracker.example/browse/GR-073");
		expect(backlog).toHaveTextContent("Backlog");
	});

	// @us-02 @slice-02 @contract-shape:pure-function
	it("links the parent when the Work Item has one and says No Parent when it has none", async () => {
		const { featureService } = renderTheRefinementTab(gravitysRefinement);

		const analysing = await theRowOf("GR-051");
		const backlog = await theRowOf("GR-073");

		expect(
			await within(analysing).findByRole("link", { name: "GR-010: Payments" }),
		).toHaveAttribute("href", "https://tracker.example/browse/GR-010");
		expect(backlog).toHaveTextContent("No Parent");
		expect(within(backlog).getAllByRole("link")).toHaveLength(1);
		expect(featureService.getFeaturesByReferences).toHaveBeenCalledWith([
			"GR-010",
		]);
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it("shows no age and no category for any Work Item", async () => {
		renderTheRefinementTab(gravitysRefinement);

		await theRowOf("GR-051");

		expect(screen.queryByText("Work Item Age")).toBeNull();
		expect(screen.queryByText("Category")).toBeNull();
		expect(screen.queryByText(/\bdays?\b/)).toBeNull();
		expect(screen.queryByText(/^(To Do|Doing)$/)).toBeNull();
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it("counts a single Work Item in the singular", async () => {
		renderTheRefinementTab({
			refinementConfigured: true,
			yardstick: NOTHING_TO_GO_ON,
			workItems: [aRow("GR-073", "Configuration management", "Backlog")],
		});

		expect(
			await screen.findByRole("heading", {
				name: "1 Work Item in Refinement",
			}),
		).toBeVisible();
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	it("states that nothing is in refinement right now instead of showing an empty grid or an error", async () => {
		renderTheRefinementTab({
			refinementConfigured: true,
			yardstick: NOTHING_TO_GO_ON,
			workItems: [],
		});

		expect(
			await screen.findByText("No Work Items in Refinement states right now"),
		).toBeVisible();
		expect(screen.queryByRole("alert")).toBeNull();
		expect(screen.queryByRole("grid")).toBeNull();
		expect(screen.queryByRole("link")).toBeNull();
	});

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it("says the Team's own words in the heading and the name column", async () => {
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
		expect(
			screen.getByRole("columnheader", { name: "Ticket Name" }),
		).toBeInTheDocument();
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
		renderTheRefinementTab({
			refinementConfigured: true,
			yardstick: NOTHING_TO_GO_ON,
			workItems: [],
		});

		expect(
			await screen.findByText("No Tickets in Replenishment states right now"),
		).toBeVisible();
	});

	// @us-02 @slice-02 @boundary @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function
	// Three hundred is the size of a whole backlog sitting in one To Do state; the heading has to count all
	// of them and the grid has to start where the backlog starts. The grid only draws the rows in view,
	// so this reads the first row rather than counting rows on the page.
	it("handles three hundred Work Items, counting all of them and starting at the top of the backlog", async () => {
		renderTheRefinementTab({
			refinementConfigured: true,
			yardstick: NOTHING_TO_GO_ON,
			workItems: Array.from({ length: 300 }, (_, index) =>
				aRow(`GR-${1000 + index}`, `Refinement candidate ${index}`, "Backlog"),
			),
		});

		expect(
			await screen.findByRole("heading", {
				name: "300 Work Items in Refinement",
			}),
		).toBeVisible();
		await theRowOf("GR-1000");
		expect(theListedReferenceIds()[0]).toBe("GR-1000");
	});

	// @us-02 @slice-02 @error @contract-shape:pure-function
	// A read that fails is said the way the other Team views say it, never shown as an empty refinement.
	it("says the read failed instead of claiming nothing is in refinement", async () => {
		renderTheRefinementTab(new Error("The Refinement tab could not be read"));

		expect(
			await screen.findByText("The Refinement tab could not be read"),
		).toBeVisible();
		expect(screen.queryByText(/right now/)).toBeNull();
		expect(screen.queryByRole("grid")).toBeNull();
	});
});

describe("The Refinement tab follows the Team it is showing", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultTerms };
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	const teamOcean = () => {
		const team = new Team();
		team.id = 8;
		team.name = "Team Ocean";
		return team;
	};

	const oceansRefinement: IRefinementView = {
		refinementConfigured: true,
		yardstick: NOTHING_TO_GO_ON,
		workItems: [aRow("OE-001", "Sonar mapping", "Backlog")],
	};

	// Opens Gravity's tab with Gravity's read still outstanding, then moves to Ocean, whose read answers
	// at once. Returns the outstanding read so the case can settle it late.
	const moveFromGravityToOceanBeforeGravitysReadAnswers = () => {
		let settleGravity: {
			answer: (view: IRefinementView) => void;
			fail: (error: Error) => void;
		} = { answer: () => {}, fail: () => {} };
		const gravitysRead = new Promise<IRefinementView>((resolve, reject) => {
			settleGravity = { answer: resolve, fail: reject };
		});
		const refinementService: IRefinementService = {
			getRefinement: vi.fn((teamId: number) =>
				teamId === 7 ? gravitysRead : Promise.resolve(oceansRefinement),
			),
		};
		const featureService = createMockFeatureService();
		featureService.getFeaturesByReferences = vi.fn().mockResolvedValue([]);

		const tabFor = (team: Team) => (
			<SnackbarErrorHandler>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({
						refinementService,
						featureService,
					})}
				>
					<RefinementView team={team} />
				</ApiServiceContext.Provider>
			</SnackbarErrorHandler>
		);

		const { rerender } = render(tabFor(teamGravity()));
		rerender(tabFor(teamOcean()));

		return { refinementService, settleGravity };
	};

	it("shows the new Team's refinement, not the previous Team's answer that arrived late", async () => {
		const { refinementService, settleGravity } =
			moveFromGravityToOceanBeforeGravitysReadAnswers();

		expect(
			await screen.findByRole("heading", {
				name: "1 Work Item in Refinement",
			}),
		).toBeVisible();
		settleGravity.answer(gravitysRefinement);
		await new Promise((resolve) => setTimeout(resolve, 0));

		expect(refinementService.getRefinement).toHaveBeenCalledWith(8);
		expect(
			screen.getByRole("heading", { name: "1 Work Item in Refinement" }),
		).toBeVisible();
		expect(screen.queryByText(/^GR-058: /)).toBeNull();
	});

	it("says nothing about the previous Team's read failing once it shows another Team", async () => {
		const { settleGravity } = moveFromGravityToOceanBeforeGravitysReadAnswers();

		await screen.findByRole("heading", { name: "1 Work Item in Refinement" });
		settleGravity.fail(
			new Error("Team Gravity's refinement could not be read"),
		);
		await new Promise((resolve) => setTimeout(resolve, 0));

		expect(
			screen.queryByText("Team Gravity's refinement could not be read"),
		).toBeNull();
	});
});
