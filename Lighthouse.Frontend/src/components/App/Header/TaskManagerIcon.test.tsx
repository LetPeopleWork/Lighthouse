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
	it("lists what is running and what is waiting, by name", async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();

		expect(await screen.findByText(/Lagunitas/)).toBeInTheDocument();
		expect(screen.getByText(/Q4 Platform/)).toBeInTheDocument();
	});

	// AC-02.3 — running and waiting are different answers, and the whole point of the glance.
	it("says which of them is running and which is waiting", async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();

		const running = await screen.findByTestId("task-manager-row-Team-7");
		const queued = screen.getByTestId("task-manager-row-Features-3");

		expect(running).toHaveTextContent(/running/i);
		expect(queued).toHaveTextContent(/queued|waiting/i);
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
});
