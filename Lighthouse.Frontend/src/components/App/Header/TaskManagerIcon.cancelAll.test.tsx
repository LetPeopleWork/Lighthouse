import { render, screen, waitFor, within } from "@testing-library/react";
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

vi.mock("../../../services/TerminologyContext", async () => {
	const actual = await vi.importActual<
		typeof import("../../../services/TerminologyContext")
	>("../../../services/TerminologyContext");
	return {
		...actual,
		useTerminology: () => ({ getTerm: (key: string) => key }),
	};
});

const aRunningTeam: IUpdateTask = {
	updateType: "Team",
	id: 7,
	name: "Payments",
	status: "InProgress",
};

const aQueuedPortfolio: IUpdateTask = {
	updateType: "Features",
	id: 3,
	name: "Platform Modernisation",
	status: "Queued",
};

const aQueuedRemoval: IUpdateTask = {
	updateType: "TeamDelete",
	id: 9,
	name: "Old",
	status: "Queued",
};

const THE_TOOLTIP =
	/^Stops everything running or queued, except removals\. Scheduled updates carry on as usual\.$/;

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
	await userEvent.click(
		await screen.findByRole("button", { name: /^Activity$/ }),
	);
};

const theActivityHeading = () =>
	screen.getByTestId("task-manager-section-activity");

const cancelAll = () =>
	within(theActivityHeading()).findByRole("button", { name: /^Cancel all$/ });

/** A cancel the instance has not answered yet, so the moment in between can be looked at. */
const anUnansweredCancel = () => {
	let answer: (value: undefined) => void = () => undefined;
	let refuse: (reason: Error) => void = () => undefined;
	const pending = new Promise<undefined>((resolve, reject) => {
		answer = resolve;
		refuse = reject;
	});
	return {
		pending,
		answer: () => answer(undefined),
		refuse: () => refuse(new Error("refused")),
	};
};

/**
 * Ingrid has pressed Update All and the instance is drowning. "Cancel all" sits at the right of the
 * Activity heading: one click, no dialog, every refresh running or queued is asked to stop except a
 * removal, which is never stopped. The rows say so at once; the list the instance gives back next is the
 * answer.
 */
describe("Cancel all in the Task Manager", () => {
	beforeEach(() => {
		mockIsSystemAdmin.mockReturnValue(true);
	});

	it(`offers "Cancel all" in the Activity heading when there is something to cancel`, async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();

		expect(await cancelAll()).toBeEnabled();
	});

	it(`says what it stops and what it leaves`, async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();
		await userEvent.hover(await cancelAll());

		expect(await screen.findByText(THE_TOOLTIP)).toBeInTheDocument();
	});

	it(`asks the instance once and puts no confirmation in the way`, async () => {
		const service = renderIcon([aRunningTeam, aQueuedPortfolio]);

		await openThePopover();
		await userEvent.click(await cancelAll());

		expect(service.cancelAllTasks).toHaveBeenCalledTimes(1);
		expect(service.cancelTask).not.toHaveBeenCalled();
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it(`reads "Cancelling…" with a spinner and cannot be pressed again while the instance answers`, async () => {
		const cancel = anUnansweredCancel();
		renderIcon([aRunningTeam, aQueuedPortfolio], (svc) => {
			svc.cancelAllTasks = vi.fn().mockReturnValue(cancel.pending);
		});

		await openThePopover();
		await userEvent.click(await cancelAll());

		const inFlight = await within(theActivityHeading()).findByRole("button", {
			name: /^Cancelling…$/,
		});
		expect(inFlight).toBeDisabled();
		expect(within(inFlight).getByRole("progressbar")).toBeInTheDocument();

		cancel.answer();
	});

	it(`has every row it can stop read "Stopping…" and leaves the removal as it was`, async () => {
		const cancel = anUnansweredCancel();
		renderIcon([aRunningTeam, aQueuedPortfolio, aQueuedRemoval], (svc) => {
			svc.cancelAllTasks = vi.fn().mockReturnValue(cancel.pending);
		});

		await openThePopover();
		await userEvent.click(await cancelAll());

		await waitFor(() => {
			expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
				/— Stopping…$/,
			);
		});
		expect(screen.getByTestId("task-manager-row-Features-3")).toHaveTextContent(
			/— Stopping…$/,
		);
		expect(
			screen.getByTestId("task-manager-row-TeamDelete-9"),
		).toHaveTextContent(/— Queued$/);

		cancel.answer();
	});

	it(`says "Could not cancel. Try again." under the heading and puts the rows back when the instance refuses`, async () => {
		renderIcon([aRunningTeam, aQueuedPortfolio], (svc) => {
			svc.cancelAllTasks = vi.fn().mockRejectedValue(new Error("refused"));
		});

		await openThePopover();
		await userEvent.click(await cancelAll());

		expect(
			await screen.findByText(/^Could not cancel\. Try again\.$/),
		).toBeInTheDocument();
		expect(screen.getByTestId("task-manager-row-Team-7")).toHaveTextContent(
			/— Running$/,
		);
		expect(screen.getByTestId("task-manager-row-Features-3")).toHaveTextContent(
			/— Queued$/,
		);
		expect(await cancelAll()).toBeEnabled();
	});

	it(`does not bring "Could not cancel. Try again." back over the next popover's rows`, async () => {
		const service = renderIcon([aRunningTeam, aQueuedPortfolio], (svc) => {
			svc.cancelAllTasks = vi.fn().mockRejectedValue(new Error("refused"));
		});

		await openThePopover();
		await userEvent.click(await cancelAll());
		await screen.findByText(/^Could not cancel\. Try again\.$/);

		await userEvent.click(screen.getByRole("button", { name: /^Close$/ }));
		vi.mocked(service.getRunningTasks).mockResolvedValue([aQueuedPortfolio]);
		await openThePopover();
		await screen.findByTestId("task-manager-row-Features-3");

		expect(
			screen.queryByText(/^Could not cancel\. Try again\.$/),
		).not.toBeInTheDocument();
	});

	it(`does not bring back a refusal that arrives after the popover was closed`, async () => {
		const cancel = anUnansweredCancel();
		const service = renderIcon([aRunningTeam, aQueuedPortfolio], (svc) => {
			svc.cancelAllTasks = vi.fn().mockReturnValue(cancel.pending);
		});

		await openThePopover();
		await userEvent.click(await cancelAll());
		await userEvent.click(screen.getByRole("button", { name: /^Close$/ }));
		cancel.refuse();
		vi.mocked(service.getRunningTasks).mockResolvedValue([aQueuedPortfolio]);
		await openThePopover();

		await waitFor(() => {
			expect(
				screen.getByTestId("task-manager-row-Features-3"),
			).toHaveTextContent(/— Queued$/);
		});
		expect(
			screen.queryByText(/^Could not cancel\. Try again\.$/),
		).not.toBeInTheDocument();
	});

	it(`drops "Could not cancel. Try again." once the list shows nothing left to cancel`, async () => {
		const service = renderIcon([aRunningTeam, aQueuedPortfolio], (svc) => {
			svc.cancelAllTasks = vi.fn().mockRejectedValue(new Error("refused"));
		});

		await openThePopover();
		await userEvent.click(await cancelAll());
		await screen.findByText(/^Could not cancel\. Try again\.$/);

		vi.mocked(service.getRunningTasks).mockResolvedValue([aQueuedRemoval]);
		const somethingChanged = vi.mocked(service.subscribeToAllUpdates).mock
			.calls[0][0];
		somethingChanged();

		await waitFor(() => {
			expect(
				screen.queryByText(/^Could not cancel\. Try again\.$/),
			).not.toBeInTheDocument();
		});
		expect(
			screen.getByTestId("task-manager-row-TeamDelete-9"),
		).toBeInTheDocument();
	});

	it(`re-reads the list once the instance has answered, so only the removals remain`, async () => {
		const service = renderIcon([
			aRunningTeam,
			aQueuedPortfolio,
			aQueuedRemoval,
		]);

		await openThePopover();
		await screen.findByTestId("task-manager-row-Team-7");
		vi.mocked(service.getRunningTasks).mockResolvedValue([aQueuedRemoval]);
		await userEvent.click(await cancelAll());

		await waitFor(() => {
			expect(
				screen.queryByTestId("task-manager-row-Team-7"),
			).not.toBeInTheDocument();
		});
		expect(
			screen.queryByTestId("task-manager-row-Features-3"),
		).not.toBeInTheDocument();
		expect(
			screen.getByTestId("task-manager-row-TeamDelete-9"),
		).toBeInTheDocument();
	});

	/** Already true while there is no such button; it has to stay true once there is. */
	it("is not offered when nothing is running or queued", async () => {
		renderIcon([]);

		await openThePopover();

		expect(
			await screen.findByText(/^Nothing is being refreshed right now\.$/),
		).toBeInTheDocument();
		expect(
			within(theActivityHeading()).queryByRole("button", { name: /cancel/i }),
		).not.toBeInTheDocument();
	});

	/** Already true while there is no such button; a removal is never stopped, so there is nothing to offer. */
	it("is not offered when only removals are in the list", async () => {
		renderIcon([aQueuedRemoval]);

		await openThePopover();
		await screen.findByTestId("task-manager-row-TeamDelete-9");

		expect(
			within(theActivityHeading()).queryByRole("button", { name: /cancel/i }),
		).not.toBeInTheDocument();
	});

	/**
	 * Already true. The whole Task Manager is a System Administrator's: for anybody else the icon that opens
	 * the popover is not drawn, so the Activity heading and its Cancel all cannot be reached. That absence is
	 * the assertion; there is no popover to open.
	 */
	it("is not offered to anybody who is not a System Administrator", async () => {
		mockIsSystemAdmin.mockReturnValue(false);
		renderIcon([aRunningTeam]);

		expect(
			screen.queryByRole("button", { name: /^Cancel all$/ }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /^Activity$/ }),
		).not.toBeInTheDocument();
	});
});
