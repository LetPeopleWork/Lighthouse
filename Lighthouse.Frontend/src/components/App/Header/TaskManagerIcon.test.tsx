import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	IUpdateSubscriptionService,
	IUpdateTask,
} from "../../../services/UpdateSubscriptionService";
import {
	createMockApiServiceContext,
	createMockUpdateSubscriptionService,
} from "../../../tests/MockApiServiceProvider";
import TaskManagerIcon from "./TaskManagerIcon";

/**
 * DISTILL specifications (Epic #5511 — Task Manager), slice 02 / #5840, frontend half.
 * US-02: AC-02.3 (rows read as something), AC-02.4 (Terminology), AC-02.5 (updates itself),
 * AC-02.6 (System Administrator only) and AC-02.7 (nothing running says so in words).
 *
 * AC-02.8 — `OAuthHealthIcon` stays beside the new icon until slice 05 — is a promise about the
 * header's composition rather than about this component, so it is specified in `Header.test.tsx`.
 */

const mockIsSystemAdmin = vi.fn();

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: mockIsSystemAdmin(),
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: () => true,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: true,
			isSystemAdmin: mockIsSystemAdmin(),
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

const mockGetTerm = vi.fn((key: string) => key);

vi.mock("../../../services/TerminologyContext", async () => {
	const actual = await vi.importActual<
		typeof import("../../../services/TerminologyContext")
	>("../../../services/TerminologyContext");
	return {
		...actual,
		useTerminology: () => ({ getTerm: mockGetTerm }),
	};
});

const aRunningTeam: IUpdateTask = {
	updateType: "Team",
	id: 7,
	name: "Lagunitas",
	status: "InProgress",
};

const aQueuedPortfolio: IUpdateTask = {
	updateType: "Features",
	id: 3,
	name: "Q4 Platform",
	status: "Queued",
	waitingBehind: "Lagunitas",
};

const renderIcon = (
	tasks: IUpdateTask[],
	configure?: (service: IUpdateSubscriptionService) => void,
) => {
	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.getRunningTasks = vi.fn().mockResolvedValue(tasks);
	configure?.(updateSubscriptionService);

	render(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ updateSubscriptionService })}
			>
				<TaskManagerIcon />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

	return updateSubscriptionService;
};

const openThePopover = async () => {
	const user = userEvent.setup();
	await user.click(await screen.findByRole("button", { name: /activity/i }));
};

describe("TaskManagerIcon", () => {
	beforeEach(() => {
		mockIsSystemAdmin.mockReturnValue(true);
		mockGetTerm.mockImplementation((key: string) => key);
	});

	// AC-02.3 — an id is not an answer to "what is running"; the name is what an operator recognises.
	// Asserted per row: the name of whatever holds the lane legitimately appears twice, once as the row
	// that is running and once inside what the waiting row says it is waiting for.
	it("lists what is running and what is waiting, by name", async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();

		expect(
			await screen.findByTestId("task-manager-row-Team-7"),
		).toHaveTextContent(/Lagunitas/);
		expect(screen.getByTestId("task-manager-row-Features-3")).toHaveTextContent(
			/Q4 Platform/,
		);
	});

	// AC-02.3 — running and waiting are different answers, and the whole point of the glance.
	it("says which of them is running and which is waiting", async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();

		const running = await screen.findByTestId("task-manager-row-Team-7");
		const queued = screen.getByTestId("task-manager-row-Features-3");

		expect(running).toHaveTextContent(/running/i);
		expect(queued).toHaveTextContent(/queued|waiting/i);

		// Both kinds are named, not only the team one - a portfolio refresh and a team refresh sitting in
		// the same list have to be tellable apart by more than their names.
		expect(running).toHaveTextContent(/team/i);
		expect(queued).toHaveTextContent(/portfolio/i);
	});

	// The naming half of deferred item G. #5877 is a user who read three teams queued behind one
	// portfolio as a hang; a row that says only "queued" is what let that happen.
	it("says what a waiting refresh is waiting behind", async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();

		expect(
			await screen.findByTestId("task-manager-row-Features-3"),
		).toHaveTextContent(/Lagunitas/);
	});

	// AC-02.4 — "Team" and "Portfolio" are renameable, and a tenant who renamed them should never meet
	// the seeded default in this list.
	it("names the kind of thing being refreshed in the reader's own words", async () => {
		mockGetTerm.mockImplementation((key: string) =>
			key === "team" ? "Squad" : key,
		);

		renderIcon([aRunningTeam]);

		await openThePopover();

		const row = await screen.findByTestId("task-manager-row-Team-7");
		expect(row).toHaveTextContent(/Squad/);
		expect(row).not.toHaveTextContent(/\bTeam\b/);
	});

	// AC-02.5 — the popover follows the work; no new transport, the existing GlobalUpdates group.
	it("refreshes itself when the instance says something changed", async () => {
		const announcers: Array<() => void> = [];

		const service = renderIcon([aRunningTeam], (s) => {
			s.subscribeToAllUpdates = vi
				.fn()
				.mockImplementation(async (callback: () => void) => {
					announcers.push(callback);
				});
		});

		await openThePopover();
		await screen.findByText(/Lagunitas/);

		(service.getRunningTasks as ReturnType<typeof vi.fn>).mockResolvedValue([
			{ ...aRunningTeam, name: "Sierra Nevada" },
		]);

		await waitFor(() => expect(announcers).not.toHaveLength(0));
		for (const announce of announcers) {
			announce();
		}

		expect(await screen.findByText(/Sierra Nevada/)).toBeInTheDocument();
	});

	// AC-02.3 / S11 — UpdateType has five members and the browser knew three, so deletes used to reach
	// this list as a type nothing could render. Saying a removal is a removal is the point: an operator
	// who reads it as an ordinary refresh will wait for data that is never coming back.
	it("says when the work in the list is a removal rather than a refresh", async () => {
		renderIcon([
			{
				updateType: "TeamDelete",
				id: 9,
				name: "Petaluma",
				status: "Queued",
			},
		]);

		await openThePopover();

		const row = await screen.findByTestId("task-manager-row-TeamDelete-9");
		expect(row).toHaveTextContent(/Petaluma/);
		expect(row).toHaveTextContent(/removal/i);
	});

	// A removal is still a removal of that kind of thing, so it is named in the reader's words too.
	it("names a removal after the kind of thing being removed", async () => {
		mockGetTerm.mockImplementation((key: string) =>
			key === "team" ? "Squad" : key,
		);

		renderIcon([
			{
				updateType: "TeamDelete",
				id: 9,
				name: "Petaluma",
				status: "Queued",
			},
		]);

		await openThePopover();

		expect(
			await screen.findByTestId("task-manager-row-TeamDelete-9"),
		).toHaveTextContent(/Squad/);
	});

	// An ordinary refresh must not read as a removal, or the word stops meaning anything.
	it("does not call an ordinary refresh a removal", async () => {
		renderIcon([aRunningTeam]);

		await openThePopover();

		expect(
			await screen.findByTestId("task-manager-row-Team-7"),
		).not.toHaveTextContent(/removal/i);
	});

	// A portfolio removal reads the same way a team one does; the list would otherwise be honest about
	// one kind of deletion and silent about the other.
	it("says when a portfolio is being removed too", async () => {
		renderIcon([
			{
				updateType: "PortfolioDelete",
				id: 4,
				name: "Q3 Platform",
				status: "Queued",
			},
		]);

		await openThePopover();

		const row = await screen.findByTestId("task-manager-row-PortfolioDelete-4");
		expect(row).toHaveTextContent(/Q3 Platform/);
		expect(row).toHaveTextContent(/removal/i);
	});

	// The first thing in an empty queue is waiting for its turn, not behind anything. Saying "behind"
	// with nothing after it would be the list inventing a blocker.
	it("does not claim a queued refresh is behind anything when nothing is running", async () => {
		renderIcon([{ ...aQueuedPortfolio, waitingBehind: null }]);

		await openThePopover();

		const row = await screen.findByTestId("task-manager-row-Features-3");
		expect(row).toHaveTextContent(/queued/i);
		expect(row).not.toHaveTextContent(/behind/i);
	});

	// The popover subscribes for as long as it is on screen and no longer. A header that is torn down
	// and rebuilt - a sign-out, a route that remounts it - would otherwise leave listeners behind.
	it("stops listening when it goes away", async () => {
		const service = createMockUpdateSubscriptionService();
		service.getRunningTasks = vi.fn().mockResolvedValue([aRunningTeam]);

		const { unmount } = render(
			<MemoryRouter>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({
						updateSubscriptionService: service,
					})}
				>
					<TaskManagerIcon />
				</ApiServiceContext.Provider>
			</MemoryRouter>,
		);

		await waitFor(() =>
			expect(service.subscribeToAllUpdates).toHaveBeenCalled(),
		);

		unmount();

		expect(service.unsubscribeFromAllUpdates).toHaveBeenCalled();
	});

	// AC-02.7 — an idle instance is an ordinary answer. An empty box reads as broken.
	it("says in words that nothing is running, rather than showing an empty box", async () => {
		renderIcon([]);

		await openThePopover();

		expect(await screen.findByText(/nothing/i)).toBeInTheDocument();
	});

	// AC-02.6 — the list names every entity on the instance, which is why it is administrator-only.
	it("does not appear at all for somebody who is not a System Administrator", async () => {
		mockIsSystemAdmin.mockReturnValue(false);

		renderIcon([aRunningTeam]);

		await waitFor(() => {
			expect(
				screen.queryByRole("button", { name: /activity/i }),
			).not.toBeInTheDocument();
		});
	});

	// AC-02.6 — and it does not go asking for the list either.
	it("does not ask for the list when the reader may not see it", async () => {
		mockIsSystemAdmin.mockReturnValue(false);

		const service = renderIcon([aRunningTeam]);

		await waitFor(() => {
			expect(service.getRunningTasks).not.toHaveBeenCalled();
		});
	});

	/**
	 * DISTILL specifications, slice 03 / #5841. US-03: AC-03.5 (a duration, and a row that still reads
	 * without one) and the browser half of AC-03.4 (the number is the instance's, not this machine's).
	 *
	 * The backend half of AC-03.4 — that the number is computed against the instance clock at all — is in
	 * `Slice03HowLongHasItBeenGoing*`. AC-03.1, AC-03.2 and AC-03.3 are about what the store records and
	 * have no frontend surface.
	 */
	describe("how long it has been going", () => {
		// AC-03.5 — the difference the slice exists for: a refresh four seconds old and one forty minutes
		// old are the same row without this.
		it("says how long a running refresh has been running", async () => {
			renderIcon([{ ...aRunningTeam, elapsedMs: 12_000 }]);

			await openThePopover();

			expect(
				await screen.findByTestId("task-manager-row-Team-7"),
			).toHaveTextContent(/running for 12s/i);
		});

		// AC-03.5 — the waiting half. A queue that has been moving and one that is wedged look identical
		// until the row says how long it has been sitting there.
		it("says how long a waiting refresh has been waiting, as well as what it waits behind", async () => {
			renderIcon([{ ...aQueuedPortfolio, elapsedMs: 3 * 60_000 }]);

			await openThePopover();

			const queued = await screen.findByTestId("task-manager-row-Features-3");

			expect(queued).toHaveTextContent(/3m/);
			expect(queued).toHaveTextContent(/Lagunitas/);
		});

		// AC-03.5 — mid-rolling-upgrade a replica on the older build admits work without recording
		// anything. The row loses its duration and nothing else; it must not vanish, say "NaN", or read
		// as a duration of nothing.
		it("still lists a refresh whose duration the instance never recorded", async () => {
			renderIcon([aRunningTeam]);

			await openThePopover();

			const row = await screen.findByTestId("task-manager-row-Team-7");

			expect(row).toHaveTextContent(/Lagunitas/);
			expect(row).toHaveTextContent(/running/i);
			expect(row).not.toHaveTextContent(/NaN|undefined|null|for\s*$/i);
		});

		// AC-03.5 — a mixed list is the real shape of a rolling upgrade, and the row that can answer has
		// to keep answering while the row that cannot stays quiet.
		it("gives the rows that have a duration theirs, without inventing one for the row that has none", async () => {
			renderIcon([
				{ ...aRunningTeam, elapsedMs: 12_000 },
				{ ...aQueuedPortfolio, elapsedMs: null },
			]);

			await openThePopover();

			expect(
				await screen.findByTestId("task-manager-row-Team-7"),
			).toHaveTextContent(/12s/);
			expect(
				screen.getByTestId("task-manager-row-Features-3"),
			).not.toHaveTextContent(/\d+\s*[smhd]\b/);
		});

		/**
		 * AC-03.4, browser half. The row shows what the instance measured; it does not start a stopwatch
		 * of its own. A component counting locally is wrong after a reload, wrong for a refresh that began
		 * before the tab was opened, and wrong by however far this machine's clock has drifted — and those
		 * are most of the occasions somebody opens this popover.
		 */
		it("does not count time on its own, however long the reader leaves the popover open", async () => {
			vi.useFakeTimers({ shouldAdvanceTime: true });

			try {
				renderIcon([{ ...aRunningTeam, elapsedMs: 12_000 }]);

				await openThePopover();
				const row = await screen.findByTestId("task-manager-row-Team-7");

				expect(row).toHaveTextContent(/12s/);

				await vi.advanceTimersByTimeAsync(60 * 60_000);

				expect(row).toHaveTextContent(/12s/);
			} finally {
				vi.useRealTimers();
			}
		});
	});
});
