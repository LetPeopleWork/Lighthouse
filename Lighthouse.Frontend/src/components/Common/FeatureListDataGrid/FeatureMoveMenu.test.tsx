import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type {
	FeatureMoveGate,
	FeatureMoveTarget,
} from "../../../models/FeatureOrdering";
import FeatureMoveMenu from "./FeatureMoveMenu";

/**
 * Epic 5375 slice 03 — US-03 AC-3.7 … AC-3.11.
 *
 * The first block is the epic's HARD GATE, and it is the reason this file exists at all. The expression
 * a reviewer would most naturally write for "may I move this Feature?" is
 * `projects.every(p => isPortfolioAdmin(p.id))`, and it fails open **twice**: `projects` is already
 * filtered to the Portfolios the caller may read, and `every` is vacuously true on the empty array an
 * orphan Feature produces. Both paths render the move actions *enabled* for somebody who may not move
 * anything at all (ADR-136 SA-10).
 *
 * So RBAC is mocked to say yes to everything below. A component that re-derives the verdict passes
 * every other test in this file and fails these — which is exactly the point.
 */
vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: true,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: (_id: number) => true,
		isPortfolioAdmin: (_id: number) => true,
		summary: {},
	}),
}));

const theSearchIndex = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 7,
		name: "Rebuild the search index",
		position: 4,
		projects: [{ id: 1, name: "Launch Alignment" }],
		forecasts: [],
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

const theServerSaysYes: FeatureMoveGate = { enabled: true };

const theServerSaysNo = (
	reason: "no-write" | "orphan" | "sorted",
	blockingPortfolios: string[] = [],
): FeatureMoveGate => ({ enabled: false, reason, blockingPortfolios });

const renderTheMenu = (
	gate: FeatureMoveGate,
	feature: IFeature = theSearchIndex(),
	onMove: (target: FeatureMoveTarget) => Promise<void> = vi
		.fn()
		.mockResolvedValue(undefined),
) => {
	render(
		<FeatureMoveMenu
			feature={feature}
			gate={gate}
			onMove={onMove}
			visibleNeighbours={{ firstId: 1, previousId: 3, nextId: 9 }}
		/>,
	);

	return { onMove };
};

const openTheMenu = async () => {
	await userEvent.click(screen.getByRole("button", { name: /move/i }));
};

// describe.skip = RED scaffold; DELIVER enables it one at a time (ADR-025). The "the move verdict is
// the server's alone" block is a HARD GATE: it must be un-skipped and green BEFORE the slice-03 code
// review, not merely before DELIVER completes — a skipped test for a fail-open authorization path is
// indistinguishable from no test.
describe.skip("FeatureMoveMenu", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// --- HARD GATE: the verdict is the server's, and this component never works it out ---
	describe("the move verdict is the server's alone", () => {
		// Fail-open shape 1: `every` over an empty array is true, so a Feature in no Portfolio the caller
		// can see would read as fully writable to any client-side conjunction.
		it("keeps the actions disabled for a Feature that belongs to no Portfolio the caller can see", async () => {
			renderTheMenu(
				theServerSaysNo("orphan"),
				theSearchIndex({ projects: [] }),
			);
			await openTheMenu();

			expect(
				screen.getByRole("menuitem", { name: "Move to Top" }),
			).toHaveAttribute("aria-disabled", "true");
		});

		// Fail-open shape 2: `projects` is read-filtered, so every Portfolio ON the row can be writable
		// while the Feature also sits in one the caller may not even see.
		it("keeps the actions disabled when every Portfolio shown on the row is writable", async () => {
			renderTheMenu(
				theServerSaysNo("no-write"),
				theSearchIndex({
					projects: [
						{ id: 1, name: "Launch Alignment" },
						{ id: 2, name: "New Product Initiative" },
					],
				}),
			);
			await openTheMenu();

			expect(
				screen.getByRole("menuitem", { name: "Move to Top" }),
			).toHaveAttribute("aria-disabled", "true");
		});

		it("enables the actions only because the server said so, never because the row looks writable", async () => {
			renderTheMenu(theServerSaysYes);
			await openTheMenu();

			expect(
				screen.getByRole("menuitem", { name: "Move to Top" }),
			).not.toHaveAttribute("aria-disabled", "true");
		});
	});

	// --- What a disabled menu tells the person looking at it ---
	describe("a refusal says why", () => {
		it("names the Portfolio standing in the way", async () => {
			renderTheMenu(theServerSaysNo("no-write", ["New Product Initiative"]));
			await openTheMenu();

			expect(
				await screen.findByText(/New Product Initiative/),
			).toBeInTheDocument();
		});

		// SA-9: a Portfolio the caller may not read is never named, so the refusal has to stand on its
		// own words. A blank tooltip would leave a dead button with no stated reason.
		it("still says something when there is no Portfolio it may name", async () => {
			renderTheMenu(theServerSaysNo("no-write"));
			await openTheMenu();

			const item = screen.getByRole("menuitem", { name: "Move to Top" });
			expect(item).toHaveAttribute("aria-disabled", "true");
			expect(item.closest("[title], [aria-describedby]")).not.toBeNull();
		});

		// AC-3.9 / D14: Move Up and Move to Top have no predictable meaning while the grid is sorted by
		// another column, so they grey out rather than doing something surprising.
		it("greys the relative moves out while the grid is sorted by a column", async () => {
			renderTheMenu(theServerSaysNo("sorted"));
			await openTheMenu();

			expect(screen.getByRole("menuitem", { name: "Move Up" })).toHaveAttribute(
				"aria-disabled",
				"true",
			);
		});
	});

	// --- AC-3.10: not disabled, absent ---
	describe("when this instance does not order its own Features", () => {
		it.each([["not-premium"], ["policy-off"]] as const)(
			"renders no move actions at all (%s)",
			async (reason) => {
				render(
					<FeatureMoveMenu
						feature={theSearchIndex()}
						gate={{ enabled: false, reason, blockingPortfolios: [] }}
						onMove={vi.fn()}
						visibleNeighbours={{ firstId: 1, previousId: 3, nextId: 9 }}
					/>,
				);

				expect(
					screen.queryByRole("button", { name: /move/i }),
				).not.toBeInTheDocument();
			},
		);
	});

	// --- The four gestures, all reduced to the one command shape (D18 / DDD-7) ---
	describe("the four gestures", () => {
		it("Move to Top places the row above the first row the user can see", async () => {
			const { onMove } = renderTheMenu(theServerSaysYes);
			await openTheMenu();

			await userEvent.click(
				screen.getByRole("menuitem", { name: "Move to Top" }),
			);

			expect(onMove).toHaveBeenCalledWith({ beforeFeatureId: 1 });
		});

		// AC-3.3: "previous" means the row above it ON SCREEN. Hidden Done Features and rows the grid
		// filtered out are jumped, not landed on.
		it("Move Up places the row above the previous VISIBLE row", async () => {
			const { onMove } = renderTheMenu(theServerSaysYes);
			await openTheMenu();

			await userEvent.click(screen.getByRole("menuitem", { name: "Move Up" }));

			expect(onMove).toHaveBeenCalledWith({ beforeFeatureId: 3 });
		});

		it("Move Down places the row below the next visible row", async () => {
			const { onMove } = renderTheMenu(theServerSaysYes);
			await openTheMenu();

			await userEvent.click(
				screen.getByRole("menuitem", { name: "Move Down" }),
			);

			expect(onMove).toHaveBeenCalledWith({ afterFeatureId: 9 });
		});

		it("Move to Bottom names no target at all, which is what the end of the order is", async () => {
			const { onMove } = renderTheMenu(theServerSaysYes);
			await openTheMenu();

			await userEvent.click(
				screen.getByRole("menuitem", { name: "Move to Bottom" }),
			);

			expect(onMove).toHaveBeenCalledWith({ beforeFeatureId: null });
		});

		it("asks for nothing when a disabled action is clicked", async () => {
			const { onMove } = renderTheMenu(theServerSaysNo("no-write"));
			await openTheMenu();

			await userEvent.click(
				screen.getByRole("menuitem", { name: "Move to Top" }),
			);

			expect(onMove).not.toHaveBeenCalled();
		});
	});

	// --- AC-3.11: buttons were chosen over drag BECAUSE of this, so it is asserted, not assumed ---
	describe("operable without a mouse", () => {
		it("opens and moves by keyboard alone", async () => {
			const { onMove } = renderTheMenu(theServerSaysYes);

			await userEvent.tab();
			expect(screen.getByRole("button", { name: /move/i })).toHaveFocus();

			await userEvent.keyboard("{Enter}");
			await userEvent.keyboard("{ArrowDown}{Enter}");

			expect(onMove).toHaveBeenCalled();
		});

		it("announces the outcome, because a list that silently re-sorts tells a screen reader nothing", async () => {
			renderTheMenu(theServerSaysYes);
			await openTheMenu();

			await userEvent.click(
				screen.getByRole("menuitem", { name: "Move to Top" }),
			);

			await waitFor(() => {
				expect(screen.getByRole("status")).toHaveTextContent(
					/Rebuild the search index/,
				);
			});
		});
	});
});
