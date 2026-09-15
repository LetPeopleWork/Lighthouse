import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useParams } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	IConnectionHealth,
	IConnectionHealthService,
} from "../../../services/Api/ConnectionHealthService";
import type {
	IUpdateSubscriptionService,
	IUpdateTask,
} from "../../../services/UpdateSubscriptionService";
import {
	createMockApiServiceContext,
	createMockConnectionHealthService,
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

const renderIconWithConnections = (
	connections: IConnectionHealth[],
	configure?: (service: IConnectionHealthService) => void,
) => {
	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.getRunningTasks = vi.fn().mockResolvedValue([]);

	const connectionHealthService = createMockConnectionHealthService();
	connectionHealthService.getHealth = vi.fn().mockResolvedValue(connections);
	configure?.(connectionHealthService);

	const { container } = render(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					updateSubscriptionService,
					connectionHealthService,
				})}
			>
				<TaskManagerIcon />
				{/* Somewhere for the row's Edit button to actually arrive, so "it offers a route" and "the
				    route goes to this connection" are different claims. */}
				<Routes>
					<Route
						path="/connections/:connectionId/edit"
						element={<EditConnectionPage />}
					/>
				</Routes>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

	return { service: connectionHealthService, container };
};

const EditConnectionPage = () => {
	const { connectionId } = useParams();

	return <div data-testid="edit-connection-page">{connectionId}</div>;
};

/**
 * The colour is the only part of AC-05.7 a reader takes in without hovering, and MUI expresses it as
 * a class on the badge rather than as anything the accessibility tree carries.
 */
const theBadgeColour = (container: HTMLElement): string => {
	const badge = container.querySelector(".MuiBadge-badge");
	const colour = [...(badge?.classList ?? [])].find((name) =>
		name.startsWith("MuiBadge-color"),
	);

	return colour ?? "no badge was rendered";
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

	/**
	 * DISTILL specifications, slice 04 / #5842. US-04: AC-04.6 (the control exists and asking is accepted
	 * whatever state the work is in) and AC-04.7 (cancelling one row does not touch another).
	 *
	 * What actually stops is the backend's promise, pinned in Slice04StopARefresh*. What this pins is that
	 * the operator can ask at all, asks about the right row, and is not told a comfortable lie when the ask
	 * fails.
	 */
	describe("stopping a refresh", () => {
		it("offers a way to stop each refresh, named after what it would stop", async () => {
			renderIcon([aRunningTeam, aQueuedPortfolio]);

			await openThePopover();

			expect(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: /stop refreshing Q4 Platform/i }),
			).toBeInTheDocument();
		});

		it("asks the instance to stop the row that was clicked, and no other", async () => {
			const service = renderIcon([aRunningTeam, aQueuedPortfolio]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			expect(service.cancelTask).toHaveBeenCalledWith("Team", 7);
			expect(service.cancelTask).toHaveBeenCalledTimes(1);
		});

		// The instance decides what stopped, not this component. Re-reading is what keeps the row honest
		// when the ask arrived too late, or was refused.
		it("re-reads the list rather than assuming the refresh stopped", async () => {
			const service = renderIcon([aRunningTeam]);

			await openThePopover();
			vi.mocked(service.getRunningTasks).mockClear();

			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(service.getRunningTasks).toHaveBeenCalled();
			});
		});

		// A refused or failed ask must not leave the row claiming something the instance never agreed to.
		it("leaves the row as the instance last described it when the ask fails", async () => {
			const service = renderIcon([aRunningTeam], (svc) => {
				svc.cancelTask = vi.fn().mockRejectedValue(new Error("refused"));
			});

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(service.getRunningTasks).toHaveBeenCalled();
			});
			expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
				/running/i,
			);
		});
	});

	/**
	 * Slice 05 / #5019. US-05: AC-05.6 (one icon, the OAuth one is gone), AC-05.7 (the colour and the
	 * tooltip name what is wrong) and the rendering half of AC-05.1 … AC-05.5.
	 *
	 * AC-05.6's other half — that the header no longer mounts a second status icon — is a promise about
	 * the header's composition and lives in `Header.test.tsx`.
	 */
	describe("connections", () => {
		const aBrokenCredential: IConnectionHealth = {
			connectionId: 11,
			connectionName: "Jira Cloud",
			workTrackingSystem: "Jira",
			state: "AuthenticationFailed",
			message: "Authentication failed for Jira.",
			observedAt: "2026-09-15T02:00:00+00:00",
		};

		const anUnreachableTracker: IConnectionHealth = {
			connectionId: 12,
			connectionName: "Linear",
			workTrackingSystem: "Linear",
			state: "Unreachable",
			message: "Could not validate the Linear connection.",
			observedAt: "2026-09-15T02:00:00+00:00",
		};

		const anUntestedConnection: IConnectionHealth = {
			connectionId: 13,
			connectionName: "Contoso Board",
			workTrackingSystem: "AzureDevOps",
			state: "Unknown",
		};

		const aHealthyConnectionWithSomethingToSay: IConnectionHealth = {
			connectionId: 14,
			connectionName: "Contoso Board",
			workTrackingSystem: "AzureDevOps",
			state: "Healthy",
			message: "Connection validated successfully.",
			observedAt: "2026-09-15T09:00:00+00:00",
		};

		// AC-05.1 — a connection with no OAuth credential row is precisely the one the deleted aggregator
		// could not see, so every connection has to appear whatever it authenticates with.
		it("lists every connection with what is known about it", async () => {
			renderIconWithConnections([aBrokenCredential, anUntestedConnection]);

			await openThePopover();

			expect(
				await screen.findByTestId("connection-health-row-11"),
			).toHaveTextContent(/Jira Cloud.*Authentication failed/i);
			expect(screen.getByTestId("connection-health-row-13")).toHaveTextContent(
				/Contoso Board/,
			);
		});

		// AC-05.3 — the whole point of the state. Rendering "Unknown" as healthy is how the icon this
		// replaced came to be decorative, and "Healthy" is a word this row may only use after a test.
		it("says a connection has not been checked rather than calling it healthy", async () => {
			renderIconWithConnections([anUntestedConnection]);

			await openThePopover();

			const row = await screen.findByTestId("connection-health-row-13");
			expect(row).toHaveTextContent(/not checked yet/i);
			expect(row).not.toHaveTextContent(/healthy/i);
		});

		// AC-05.7 — the tooltip is what an administrator reads without opening anything, so it has to name
		// the connection rather than say that something, somewhere, is wrong.
		it("names what is wrong on the icon itself", async () => {
			renderIconWithConnections([aBrokenCredential]);

			expect(
				await screen.findByRole("button", { name: /Jira Cloud/i }),
			).toBeInTheDocument();
		});

		// AC-05.7 — the positive control. Without it, "always warn" satisfies the scenario above.
		//
		// The row is waited for first, and that is not ceremony: the icon is labelled "Activity" before
		// the health read comes back, so asserting straight away would pass on a component that had not
		// yet looked at anything.
		it("says nothing is wrong when nothing is wrong", async () => {
			renderIconWithConnections([anUntestedConnection]);

			// Opening and closing is how this test knows the health read has been applied. The icon reads
			// "Activity" before the read comes back too, so asserting straight away would pass on a
			// component that had not yet looked at anything.
			await openThePopover();
			await screen.findByTestId("connection-health-row-13");
			await userEvent.keyboard("{Escape}");

			expect(
				await screen.findByRole("button", { name: /activity/i }),
			).toHaveAccessibleName("Activity");
		});

		// AC-05.7 — a credential that was refused is something an administrator can go and fix; a tracker
		// that could not be reached may well fix itself. One icon has to show the worse of the two, and
		// the unreachable connection is listed first precisely so "the first one" cannot pass this.
		it("colours the icon for the worst of the connection states, not the first", async () => {
			const { container } = renderIconWithConnections([
				anUnreachableTracker,
				aBrokenCredential,
			]);

			await screen.findByRole("button", { name: /Jira Cloud/i });

			expect(theBadgeColour(container)).toBe("MuiBadge-colorError");
		});

		// AC-05.7 — the other two rungs, so "always red" cannot satisfy the one above.
		it("colours the icon for a tracker it could not reach differently from a credential it could not use", async () => {
			const { container } = renderIconWithConnections([anUnreachableTracker]);

			await screen.findByRole("button", { name: /Linear/i });

			expect(theBadgeColour(container)).toBe("MuiBadge-colorWarning");
		});

		// AC-05.4 — one outbound check, for the connection whose button was pressed.
		it("tests only the connection whose button was pressed", async () => {
			const { service } = renderIconWithConnections([
				aBrokenCredential,
				anUntestedConnection,
			]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /test connection Jira Cloud/i,
				}),
			);

			expect(service.testConnection).toHaveBeenCalledWith(11);
			expect(service.testConnection).toHaveBeenCalledTimes(1);
		});

		// AC-05.4 — the row shows what the instance answered, not what the click hoped for.
		it("shows what the test answered rather than assuming it worked", async () => {
			const { service } = renderIconWithConnections(
				[aBrokenCredential],
				(svc) => {
					svc.testConnection = vi
						.fn()
						.mockResolvedValue({ ...aBrokenCredential, state: "Healthy" });
				},
			);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /test connection Jira Cloud/i,
				}),
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("connection-health-row-11"),
				).toHaveTextContent(/Healthy/i);
			});
			expect(service.testConnection).toHaveBeenCalledTimes(1);
		});

		// AC-05.4 — a refused or failed ask must not leave the row claiming something the instance never said.
		it("leaves the row as the instance last described it when the test fails", async () => {
			renderIconWithConnections([aBrokenCredential], (svc) => {
				svc.testConnection = vi.fn().mockRejectedValue(new Error("refused"));
			});

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /test connection Jira Cloud/i,
				}),
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("connection-health-row-11"),
				).toHaveTextContent(/Authentication failed/i);
			});
		});

		// AC-05.5 — the route the deleted icon offered. Being told a credential is broken without a way to
		// go and fix it is the same dead end as not being told at all.
		it("offers the way to the connection that needs fixing", async () => {
			renderIconWithConnections([aBrokenCredential]);

			await openThePopover();

			expect(
				await screen.findByRole("button", {
					name: /edit connection Jira Cloud/i,
				}),
			).toBeInTheDocument();
		});

		// AC-05.2 — the state says something is wrong; the message says what to do about it, and getting
		// that wrong is an afternoon spent reissuing a credential that was never the problem.
		it("shows what to do about a connection that is broken", async () => {
			renderIconWithConnections([aBrokenCredential]);

			await openThePopover();

			expect(
				await screen.findByTestId("connection-health-explanation-11"),
			).toHaveTextContent(/Authentication failed for Jira/i);
		});

		// The inverse, so the explanation does not become a line that is always there saying nothing.
		// A connection that has just been tested successfully carries a message too — "connection
		// validated" — and repeating it under every healthy row is noise in a box read at a glance.
		it("says nothing further about a connection that is not broken", async () => {
			renderIconWithConnections([aHealthyConnectionWithSomethingToSay]);

			await openThePopover();

			await screen.findByTestId("connection-health-row-14");
			expect(
				screen.queryByTestId("connection-health-explanation-14"),
			).not.toBeInTheDocument();
		});

		// AC-05.2 — the third rung. Without it the wording table can lose this entry and nothing notices.
		it("says a tracker it could not reach was not reached, rather than blaming the credential", async () => {
			renderIconWithConnections([anUnreachableTracker]);

			await openThePopover();

			const row = await screen.findByTestId("connection-health-row-12");
			expect(row).toHaveTextContent(/Unreachable/i);
			expect(row).not.toHaveTextContent(/Authentication failed/i);
		});

		// AC-05.7 — the bottom rung of the colour ladder. Without it "always warn" passes every other
		// colour scenario in this file.
		it("leaves the icon its ordinary colour when no connection is in trouble", async () => {
			const { container } = renderIconWithConnections([anUntestedConnection]);

			await openThePopover();
			await screen.findByTestId("connection-health-row-13");

			expect(theBadgeColour(container)).toBe("MuiBadge-colorPrimary");
		});

		// AC-05.7 — a tracker that could not be reached still warns when it sits beside a connection that
		// is perfectly fine. "All of them" and "any of them" are different questions.
		it("warns when only one of several connections cannot be reached", async () => {
			const { container } = renderIconWithConnections([
				anUnreachableTracker,
				anUntestedConnection,
			]);

			await screen.findByRole("button", { name: /Linear/i });

			expect(theBadgeColour(container)).toBe("MuiBadge-colorWarning");
		});

		// AC-05.7 — an administrator with two broken connections has to learn both from the tooltip, or
		// the second one is a surprise waiting after they fix the first.
		it("names every connection that is in trouble, not just one", async () => {
			renderIconWithConnections([aBrokenCredential, anUnreachableTracker]);

			const icon = await screen.findByRole("button", { name: /Jira Cloud/i });

			expect(icon).toHaveAccessibleName(/Jira Cloud/i);
			expect(icon).toHaveAccessibleName(/Linear/i);
		});

		// AC-05.5 — the route the deleted icon offered has to actually go somewhere.
		it("takes the reader to the connection that needs fixing", async () => {
			renderIconWithConnections([aBrokenCredential]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /edit connection Jira Cloud/i,
				}),
			);

			expect(
				await screen.findByTestId("edit-connection-page"),
			).toHaveTextContent("11");
		});

		// AC-05.1 — an instance with no connections says so, rather than rendering a heading over nothing.
		it("says so when there are no connections at all", async () => {
			renderIconWithConnections([]);

			await openThePopover();

			expect(
				await screen.findByText(/no connections are configured/i),
			).toBeInTheDocument();
		});

		// AC-05.8 — the section names every connection on the instance, so it is administrator-only for the
		// same reason the rest of the popover is.
		it("shows nothing at all to somebody who is not a System Administrator", () => {
			mockIsSystemAdmin.mockReturnValue(false);

			const { container } = renderIconWithConnections([aBrokenCredential]);

			expect(container).toBeEmptyDOMElement();
		});
	});
});
