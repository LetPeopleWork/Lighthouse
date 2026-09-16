import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
	MemoryRouter,
	Route,
	Routes,
	useParams,
	useSearchParams,
} from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	IConnectionHealth,
	IConnectionHealthService,
} from "../../../services/Api/ConnectionHealthService";
import type {
	ILogService,
	IRecentProblem,
} from "../../../services/Api/LogService";
import type {
	IUpdateSubscriptionService,
	IUpdateTask,
} from "../../../services/UpdateSubscriptionService";
import {
	createMockApiServiceContext,
	createMockConnectionHealthService,
	createMockLogService,
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
	/** Left empty unless the scenario is about the two halves together, as the badge count is. */
	tasks: IUpdateTask[] = [],
) => {
	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.getRunningTasks = vi.fn().mockResolvedValue(tasks);

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

/**
 * DISTILL specifications (Epic #5511 — Task Manager), slice 06 / #5843, frontend half. US-06:
 * AC-06.1 (what a row says), AC-06.5 (the copy that stops this being read as an audit log) and
 * AC-06.6 (an instance with nothing wrong says so). AC-06.2, AC-06.3, AC-06.4 and AC-06.7 are backend
 * promises and live in `Slice06TheWarningsWithoutTheLog{Scenarios,Specifications}.cs`.
 *
 * The section under test belongs in its own file — `TaskManager/RecentProblemsSection.tsx`, beside
 * `ActivitySection` and `ConnectionsSection` — rather than inline in the icon, whose render body is
 * already close to the cognitive-complexity limit this repository has lost CI cycles to.
 */
const renderIconWithProblems = (
	problems: IRecentProblem[],
	configure?: (service: ILogService) => void,
) => {
	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.getRunningTasks = vi.fn().mockResolvedValue([]);

	const logService = createMockLogService();
	logService.getRecentProblems = vi.fn().mockResolvedValue(problems);
	configure?.(logService);

	render(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					updateSubscriptionService,
					logService,
				})}
			>
				<TaskManagerIcon />
				{/* Somewhere for the link to the full log to actually arrive, so "it offers a way there" and
				    "it goes to the log viewer" are different claims. */}
				<Routes>
					<Route path="/settings" element={<LogViewerPage />} />
				</Routes>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

	return logService;
};

const LogViewerPage = () => {
	const [searchParams] = useSearchParams();

	return <div data-testid="log-viewer-page">{searchParams.get("tab")}</div>;
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

/**
 * What the badge says without the box being opened. An empty string is what MUI renders for a count of
 * zero, which is the same thing a reader sees when there is genuinely nothing to report.
 */
const theBadgeCount = (container: HTMLElement): string =>
	container.querySelector(".MuiBadge-badge")?.textContent ??
	"no badge was rendered";

/**
 * Which of MUI's inks a drawing was given, which it carries as a class rather than as anything the
 * accessibility tree exposes. Shape is what a reader who cannot tell the colours apart goes on - that
 * is asserted separately - and colour is what everyone else takes in first.
 */
const theInkOfTheIconNamed = (name: string): string => {
	const icon = screen.getByRole("img", { name });
	const ink = [...icon.classList].find((className) =>
		className.startsWith("MuiSvgIcon-color"),
	);

	return ink ?? `${name} was drawn in no particular ink`;
};

const openThePopover = async () => {
	const user = userEvent.setup();
	await user.click(await screen.findByRole("button", { name: /activity/i }));
};

/**
 * The three headings a reader scans down, named so a test can point at one. AC-07A.7 is about all three
 * together — a mark beside one and not the others is worse than none, because the two without look like
 * they are missing something.
 */
const THE_THREE_SECTIONS = [
	"task-manager-section-activity",
	"task-manager-section-connections",
	"task-manager-section-problems",
];

/**
 * Everything the popover reads, all answering. The section promises are about the whole box rather than
 * about one section's contents, and a helper that supplies only one leaves the other two rendering their
 * "nothing to report" state — which is a different arrangement of the same headings.
 */
const renderTheWholePopover = () => {
	const updateSubscriptionService = createMockUpdateSubscriptionService();
	updateSubscriptionService.getRunningTasks = vi
		.fn()
		.mockResolvedValue([aRunningTeam]);

	const connectionHealthService = createMockConnectionHealthService();
	connectionHealthService.getHealth = vi.fn().mockResolvedValue([
		{
			connectionId: 21,
			connectionName: "Jira Cloud",
			workTrackingSystem: "Jira",
			state: "Healthy",
		} satisfies IConnectionHealth,
	]);

	const logService = createMockLogService();
	logService.getRecentProblems = vi.fn().mockResolvedValue([]);

	render(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					updateSubscriptionService,
					connectionHealthService,
					logService,
				})}
			>
				<TaskManagerIcon />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
};

/**
 * The outline of whatever is drawn for a state, read off the rendered geometry. This is the assertion
 * that answers AC-07C.2 honestly: somebody who cannot tell green from grey can still tell a ring from a
 * tick, and only a difference in the path itself gives them that. A test comparing colours would pass on
 * two icons that are the same drawing in two inks, which is the failure the criterion names.
 */
const theShapeOfTheIconNamed = (name: string): string => {
	const icon = screen.getByRole("img", { name });
	const outline = [...icon.querySelectorAll("path")]
		.map((path) => path.getAttribute("d"))
		.join(" ");

	return outline === "" ? `${name} was drawn with no outline at all` : outline;
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
	//
	// The whole sentence, not merely a word out of it: the popover holds more than one section that
	// answers with a "nothing", and a matcher loose enough to catch any of them is satisfied by the
	// wrong one — or, once two are on screen together, by neither, because `findByText` refuses a
	// match it cannot make unambiguously.
	it("says in words that nothing is running, rather than showing an empty box", async () => {
		renderIcon([]);

		await openThePopover();

		expect(
			await screen.findByText(/nothing is being refreshed right now/i),
		).toBeInTheDocument();
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
	 * DISTILL specifications, slice 07 / #6011. US-07A: AC-07A.4 (progress shown rather than spelled),
	 * AC-07A.5 (no row carries a duration), AC-07A.6 (what a waiting row is waiting behind survives),
	 * AC-07A.7 (an icon beside each heading and nothing to fold a section away) and AC-07A.8 (the
	 * headings keep the tenant's own words).
	 *
	 * Slice 03's promises about a rendered duration are gone from this file rather than inverted. D14
	 * withdrew them: the number was not acted on and was wrong the moment it was drawn, so the rows say
	 * `Running` and `Queued` and nothing about elapsed time. What slice 03 built on the backend stands —
	 * the moments are what slice 07 sorts on.
	 *
	 * AC-07A.1 … AC-07A.3 are the instance's, in `Slice07TheQueueReadsLikeAQueue*`: the browser renders
	 * the list in the order it arrives, which is the whole point of sorting it on the way out.
	 */
	describe("the queue reads like a queue", () => {
		// AC-07A.4 — what is turning and what is waiting, told apart without reading. The running row gets
		// the affordance that means "something is happening"; the waiting ones deliberately do not, because
		// three spinners in a column say three things are under way when one is.
		it("shows the refresh that is running turning, and the ones waiting waiting", async () => {
			renderIcon([aRunningTeam, aQueuedPortfolio]);

			await openThePopover();

			expect(
				await screen.findByRole("progressbar", { name: /running/i }),
			).toBeInTheDocument();
			expect(screen.getAllByRole("progressbar")).toHaveLength(1);
			expect(
				screen.getByRole("img", { name: /queued|waiting/i }),
			).toBeInTheDocument();
		});

		// AC-07A.5 — the instance still measures and still sends the number; the row no longer spends a
		// reader's attention on it. Asserted against a row that HAS one, because a row with nothing to
		// render satisfies "renders no duration" whatever the component does with it.
		it("says a refresh is running without saying for how long", async () => {
			renderIcon([{ ...aRunningTeam, elapsedMs: 134_000 }]);

			await openThePopover();

			const row = await screen.findByTestId("task-manager-row-Team-7");

			expect(row).toHaveTextContent(/running/i);
			expect(row).not.toHaveTextContent(/2m|14s/);
			expect(row).not.toHaveTextContent(/\d+\s*[smhd]\b/);
		});

		// AC-07A.6 — naming what a row is waiting for is a claim about the queue, not a measurement, and
		// #5877 is a user who read three teams queued behind one portfolio as a hang. It survives D14.
		it("still says what a waiting refresh is waiting behind", async () => {
			renderIcon([{ ...aQueuedPortfolio, elapsedMs: 3 * 60_000 }]);

			await openThePopover();

			const row = await screen.findByTestId("task-manager-row-Features-3");

			expect(row).toHaveTextContent(/queued behind Lagunitas/i);
			expect(row).not.toHaveTextContent(/\d+\s*[smhd]\b/);
		});

		// AC-07A.7 — three sections in a box opened for a glance. An icon beside each heading is what makes
		// them findable; a control that folds one away puts a click in front of content that is already
		// short enough to read, and a preference to remember afterwards.
		it("puts a mark beside each heading, and offers no way to fold a section away", async () => {
			renderTheWholePopover();

			await openThePopover();

			for (const section of THE_THREE_SECTIONS) {
				const heading = await screen.findByTestId(section);

				expect(heading.querySelector("svg")).not.toBeNull();
				expect(heading.closest("button")).toBeNull();
			}
		});

		// AC-07A.8 — a regression guard rather than a repair. The heading already resolves the tenant's
		// word, and this story edits the lines it sits on.
		it("still names the connections section in the reader's own words", async () => {
			mockGetTerm.mockImplementation((key: string) =>
				key === "workTrackingSystems" ? "Trackers" : key,
			);

			renderTheWholePopover();

			await openThePopover();

			expect(await screen.findByText("Trackers")).toBeInTheDocument();
		});

		// The only thing that provokes a read today is refresh activity, so a connection added a minute
		// ago is missing until the page is reloaded. Opening the popover is the moment somebody wants the
		// answer to be current, and it is the only moment worth spending a read on.
		it("asks the instance again every time the popover is opened", async () => {
			const service = renderIcon([aRunningTeam]);

			await openThePopover();
			await screen.findByText(/Lagunitas/);
			const readsBeforeTheSecondOpen = vi.mocked(service.getRunningTasks).mock
				.calls.length;

			await userEvent.keyboard("{Escape}");
			await openThePopover();

			await waitFor(() => {
				expect(
					vi.mocked(service.getRunningTasks).mock.calls.length,
				).toBeGreaterThan(readsBeforeTheSecondOpen);
			});
		});
	});

	/**
	 * DISTILL specifications, slice 07 / #6011. US-07B: AC-07B.1 (the row says the stop was asked for),
	 * AC-07B.2 (it leaves when the instance stops reporting the work), AC-07B.3 (the control does not
	 * re-arm) and AC-07B.4 (a refused stop leaves the row as it was).
	 *
	 * The fact being rendered lives in this component and nowhere else (OQ-07.1, settled client-side), so
	 * these are the whole of the promise rather than the browser half of one. AC-07B.5 is not here and
	 * cannot be: the gap it is about is the eleven seconds a real connector spends inside a page fetch,
	 * and a doubled service that resolves immediately has no gap to observe.
	 */
	describe("a cancel that was heard", () => {
		// AC-07B.1 — measured at eleven seconds on the backend log, during which the row is unchanged and
		// the control reads as dead. Clicking twice is what an operator does next.
		it("says the stop was asked for while the instance is still working on it", async () => {
			renderIcon([aRunningTeam]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
					/stopping/i,
				);
			});
		});

		// AC-07B.2 — the instance decides what stopped. Removing the row on the click would claim an
		// outcome nobody agreed to, and the row would come back on the next read when the stop landed too
		// late — which is worse than never having moved.
		it("keeps the row until the instance stops reporting the work", async () => {
			const service = renderIcon([aRunningTeam]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
					/stopping/i,
				);
			});

			vi.mocked(service.getRunningTasks).mockResolvedValue([]);
			await userEvent.keyboard("{Escape}");
			await openThePopover();

			await waitFor(() => {
				expect(
					screen.queryByTestId("task-manager-row-Team-7"),
				).not.toBeInTheDocument();
			});
		});

		// AC-07B.3 — the second click is the thing this story exists to prevent. A control that still
		// invites one is a control that has not answered.
		it("does not invite the stop to be asked for a second time", async () => {
			renderIcon([aRunningTeam]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /stop refreshing Lagunitas/i }),
				).toBeDisabled();
			});
		});

		// The mark beside the word has to say the same thing the word does. Deciding what the row is
		// doing separately in each is how they come to disagree, and a row reading "Stopping…" beside a
		// mark that still means "waiting its turn" is worse than either alone.
		it("marks the row as still being worked on while the stop is being heard", async () => {
			renderIcon([aRunningTeam]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			expect(
				await screen.findByRole("progressbar", { name: "Stopping…" }),
			).toBeInTheDocument();
		});

		// A team and a portfolio are numbered separately, so two rows can carry the same number and be
		// different work. Remembering the ask by number alone would have one press of Stop put every row
		// that happens to share it into a state nobody asked for.
		it("remembers which row the stop was asked for, not merely its number", async () => {
			renderIcon([
				aRunningTeam,
				{
					updateType: "Features",
					id: 7,
					name: "Q4 Platform",
					status: "Queued",
				},
			]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
					/stopping/i,
				);
			});
			expect(
				screen.getByTestId("task-manager-row-Features-7"),
			).not.toHaveTextContent(/stopping/i);
		});

		// The control is about to stop accepting input, and a disabled element receives no key presses,
		// so focus has to go somewhere a keyboard reader can still work from. The row takes it — which
		// also puts the row's new state in front of a screen reader — and it takes it without joining the
		// tab order, because a row that did would sit ahead of everything else on the page.
		it("moves focus onto the row rather than leaving it on a control that has gone dead", async () => {
			renderIcon([aRunningTeam]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			const row = screen
				.getByTestId("task-manager-row-Team-7")
				.closest("[data-task-row]");

			await waitFor(() => {
				expect(row).toHaveFocus();
			});
			expect(row).toHaveAttribute("tabindex", "-1");
		});

		// The browser remembers the ask only for as long as the instance still reports the work. A key
		// that comes back — because the ask arrived after the refresh had already been requeued — has to
		// read as running again, or the word "stopping" outlives everything it was ever true about.
		it("stops saying a refresh is stopping once the instance lists it afresh", async () => {
			const service = renderIcon([aRunningTeam]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
					/stopping/i,
				);
			});

			vi.mocked(service.getRunningTasks).mockResolvedValue([]);
			await userEvent.keyboard("{Escape}");
			await openThePopover();
			await waitFor(() => {
				expect(
					screen.queryByTestId("task-manager-row-Team-7"),
				).not.toBeInTheDocument();
			});

			vi.mocked(service.getRunningTasks).mockResolvedValue([aRunningTeam]);
			await userEvent.keyboard("{Escape}");
			await openThePopover();

			await waitFor(() => {
				expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
					/running/i,
				);
			});
			expect(
				screen.getByTestId("task-manager-row-Team-7"),
			).not.toHaveTextContent(/stopping/i);
		});

		// AC-07B.4 — a removal is refused by design, and the refusal has to stay legible. A row stuck on
		// "stopping" for something that will never stop teaches the reader the word means nothing.
		it("leaves the row as it was when the instance refuses to stop it", async () => {
			renderIcon([aRunningTeam], (svc) => {
				svc.cancelTask = vi.fn().mockRejectedValue(new Error("refused"));
			});

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", {
					name: /stop refreshing Lagunitas/i,
				}),
			);

			await waitFor(() => {
				expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
					/running/i,
				);
			});
			expect(
				screen.getByTestId("task-manager-row-Team-7"),
			).not.toHaveTextContent(/stopping/i);
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

			// The state moved out of the row's own text and into the drawing beside it, so this reads it
			// from there. Scoped to the connection it belongs to rather than looked up across the whole
			// popover, because "every connection, with what is known about it" is a claim about the pairing
			// and a loose lookup would be satisfied by the right word beside the wrong name.
			const theBrokenOne = within(
				await screen.findByTestId("connection-health-11"),
			);
			expect(
				theBrokenOne.getByTestId("connection-health-row-11"),
			).toHaveTextContent(/Jira Cloud/);
			expect(theBrokenOne.getByRole("img")).toHaveAccessibleName(
				"Authentication failed",
			);

			expect(screen.getByTestId("connection-health-row-13")).toHaveTextContent(
				/Contoso Board/,
			);
		});

		// AC-05.3 — the whole point of the state. Rendering "Unknown" as healthy is how the icon this
		// replaced came to be decorative, and "Healthy" is a word this row may only use after a test.
		it("says a connection has not been checked rather than calling it healthy", async () => {
			renderIconWithConnections([anUntestedConnection]);

			await openThePopover();

			const state = within(
				await screen.findByTestId("connection-health-13"),
			).getByRole("img");
			expect(state).toHaveAccessibleName(/not checked yet/i);
			expect(state).not.toHaveAccessibleName(/healthy/i);
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
					within(screen.getByTestId("connection-health-11")).getByRole("img"),
				).toHaveAccessibleName(/Healthy/i);
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
					within(screen.getByTestId("connection-health-11")).getByRole("img"),
				).toHaveAccessibleName(/Authentication failed/i);
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

			const state = within(
				await screen.findByTestId("connection-health-12"),
			).getByRole("img");
			expect(state).toHaveAccessibleName(/Unreachable/i);
			expect(state).not.toHaveAccessibleName(/Authentication failed/i);
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

		/**
		 * DISTILL specifications, slice 07 / #6011. US-07C: AC-07C.1 (the state is drawn rather than
		 * spelled), AC-07C.2 (not-checked differs from healthy by more than its colour) and AC-07C.3 (the
		 * word it replaced stays within reach).
		 *
		 * AC-07C.4 and AC-07C.5 promise that the explanation under a broken row, and the Test connection
		 * and Edit buttons, are unchanged. The scenarios above already hold them and are not repeated here:
		 * a second copy of a promise is a second place for it to drift.
		 */
		describe("state at a glance", () => {
			// AC-07C.1 — four connections were four sentences to read and compare. The word does not
			// disappear, it stops taking a line: it moves to the accessible name, asserted below.
			it("draws each connection's state instead of spelling it out in the row", async () => {
				renderIconWithConnections([
					aBrokenCredential,
					anUntestedConnection,
					anUnreachableTracker,
				]);

				await openThePopover();

				expect(
					await screen.findByRole("img", { name: "Authentication failed" }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("img", { name: "Not checked yet" }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("img", { name: "Unreachable" }),
				).toBeInTheDocument();

				expect(
					screen.getByTestId("connection-health-row-13"),
				).not.toHaveTextContent(/not checked/i);
			});

			// AC-07C.2 — the icon this replaced claimed health from an absence of evidence, and an icon
			// set where not-checked reads as a muted tick would be that bug wearing a redesign. The outline
			// is what keeps "nobody has asked" apart from "asked, and the answer was yes" for a reader who
			// gets nothing from the colour.
			it("tells a connection nobody has checked from a healthy one without relying on colour", async () => {
				renderIconWithConnections([
					anUntestedConnection,
					aHealthyConnectionWithSomethingToSay,
				]);

				await openThePopover();
				await screen.findByTestId("connection-health-row-13");

				expect(theShapeOfTheIconNamed("Not checked yet")).not.toEqual(
					theShapeOfTheIconNamed("Healthy"),
				);
			});

			// AC-07C.3 — a distinction drawn in fill and shape is not one a screen reader can make, and a
			// reader who is unsure what a drawing means has to be able to ask. Both answers are the word the
			// row used to carry, so nothing is lost by moving it.
			it("keeps the state word within reach of a screen reader and of a hover", async () => {
				renderIconWithConnections([anUntestedConnection]);

				await openThePopover();
				await userEvent.hover(
					await screen.findByRole("img", { name: "Not checked yet" }),
				);

				expect(
					await screen.findByRole("tooltip", { name: /not checked yet/i }),
				).toBeInTheDocument();
			});

			// Shape is what a reader who cannot separate the colours goes on, and it is asserted above.
			// Colour is what everybody else takes in first, and it has to mean the same thing the shape
			// does: red for something to go and fix, green for something that answered, grey for a
			// question nobody has asked. All four states, so one ink used everywhere cannot satisfy this.
			it("draws a connection in trouble in a different ink from one that is fine", async () => {
				renderIconWithConnections([
					aBrokenCredential,
					anUnreachableTracker,
					anUntestedConnection,
					aHealthyConnectionWithSomethingToSay,
				]);

				await openThePopover();
				await screen.findByRole("img", { name: "Healthy" });

				expect(theInkOfTheIconNamed("Authentication failed")).toBe(
					"MuiSvgIcon-colorError",
				);
				expect(theInkOfTheIconNamed("Unreachable")).toBe(
					"MuiSvgIcon-colorError",
				);
				expect(theInkOfTheIconNamed("Healthy")).toBe("MuiSvgIcon-colorSuccess");
				expect(theInkOfTheIconNamed("Not checked yet")).toBe(
					"MuiSvgIcon-colorDisabled",
				);
			});
		});

		// The answer names the connection it is about, and it belongs to that row alone. A verdict
		// written across the whole list would have one press of Test connection re-describe connections
		// nobody asked about — and describe them, every time, as whatever the tested one turned out to be.
		it("writes the test's answer into the row it was pressed from and no other", async () => {
			renderIconWithConnections(
				[aBrokenCredential, anUntestedConnection],
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
					within(screen.getByTestId("connection-health-11")).getByRole("img"),
				).toHaveAccessibleName(/Healthy/i);
			});
			expect(
				within(screen.getByTestId("connection-health-13")).getByRole("img"),
			).toHaveAccessibleName(/Not checked yet/i);
		});

		// The number is the whole of what an operator reads without opening the box, and it answers one
		// question: how many things want attention. Work in hand and a connection that is broken are both
		// such things, so leaving either out — or setting one against the other — describes an instance
		// nobody is running.
		it("counts both the work in hand and the connections that need attention", async () => {
			const { container } = renderIconWithConnections(
				[aBrokenCredential, aHealthyConnectionWithSomethingToSay],
				undefined,
				[aRunningTeam],
			);

			await screen.findByRole("button", { name: /Jira Cloud/i });

			await waitFor(() => {
				expect(theBadgeCount(container)).toBe("2");
			});
		});
	});

	describe("recent problems", () => {
		const aRefreshThatBroke: IRecentProblem = {
			recordedAt: "2026-09-15T09:31:00+00:00",
			level: "Error",
			source: "UpdateQueueService",
			// What the instance sends: a row about a refresh names what the refresh was of. A fixture
			// carrying the id instead would have this file describing a sentence nobody produces, and the
			// section renders whatever it is given either way.
			message: "Error processing update task for Lagunitas",
			// The short name, which is what the instance actually sends: the type is trimmed to its last
			// segment for the same reason the source is, so a namespace in front of it here would have the
			// fixture describing a contract nobody implements.
			exceptionType: "InvalidOperationException",
		};

		const somethingThatOnlyWarned: IRecentProblem = {
			recordedAt: "2026-09-15T09:12:00+00:00",
			level: "Warning",
			source: "UpdateQueueService",
			message: "Update queue drain exceeded the shutdown timeout",
			exceptionType: null,
		};

		// AC-06.1 — the row is the whole feature. An operator reading it has to learn what went wrong and
		// how seriously to take it without opening anything else.
		it("says what went wrong and how serious it was", async () => {
			renderIconWithProblems([aRefreshThatBroke]);

			await openThePopover();

			const row = await screen.findByTestId("recent-problem-row");
			expect(row).toHaveTextContent(/Error processing update task/i);
			expect(row).toHaveTextContent(/error/i);
		});

		// AC-06.1 — the instance decides the order, and a short list is read from the top. Re-sorting here
		// would put the failure that has already been dealt with above the one that has not.
		it("reads newest first, in the order the instance gave them", async () => {
			renderIconWithProblems([aRefreshThatBroke, somethingThatOnlyWarned]);

			await openThePopover();

			const rows = await screen.findAllByTestId("recent-problem-row");
			expect(rows[0]).toHaveTextContent(/Error processing update task/i);
			expect(rows[1]).toHaveTextContent(/drain exceeded/i);
		});

		// AC-07D.4 — the promise that this section is not an audit log used to be pinned here, against the
		// literal sentence. It is gone rather than inverted: the reader it was written for has used it and
		// reports a paragraph above four rows as a reason not to read the four rows. A test asserting the
		// sentence is absent would pin a decision that has now been reversed once, and can be again.
		// AC-07D.1 is therefore carried by no test at all, deliberately.

		// AC-06.6 — an empty box reads as "this feature is broken". Saying nothing has gone wrong is the
		// answer, and it is a different answer from saying nothing at all.
		// Written out in full rather than matched loosely: this sentence is JSX text, which the mutation
		// runner does not rewrite, so a substring match is the only thing standing behind it and a
		// substring match would survive losing the half that says how far back "nothing" reaches.
		it("says nothing has gone wrong rather than rendering an empty section", async () => {
			renderIconWithProblems([]);

			await openThePopover();

			expect(
				await screen.findByText(
					"Nothing has gone wrong since this instance started.",
				),
			).toBeInTheDocument();
		});

		// AC-06.5 — the section holds a bounded handful; everything else is still in the log. Being shown
		// the last few problems with no way on to the rest is a dead end.
		it("offers the way through to the full log", async () => {
			renderIconWithProblems([aRefreshThatBroke]);

			await openThePopover();
			await userEvent.click(
				await screen.findByRole("button", { name: /open the full log/i }),
			);

			expect(await screen.findByTestId("log-viewer-page")).toHaveTextContent(
				"system-info",
			);
		});

		// The instance could not be asked, which is not the same as the instance having nothing to report.
		// Rendering the reassuring answer to a question that was never answered is the failure mode the
		// icon this popover replaced was built out of.
		it("does not claim nothing has gone wrong when it could not ask", async () => {
			renderIconWithProblems([], (service) => {
				service.getRecentProblems = vi
					.fn()
					.mockRejectedValue(new Error("refused"));
			});

			await openThePopover();

			await screen.findByText(/nothing is being refreshed right now/i);
			expect(
				screen.queryByText(/nothing has gone wrong/i),
			).not.toBeInTheDocument();
		});
	});
});
