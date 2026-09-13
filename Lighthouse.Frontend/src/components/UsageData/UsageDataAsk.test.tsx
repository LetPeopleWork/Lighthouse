import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../hooks/useUsageDataConsent";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import Footer from "../App/Footer/Footer";
import { UsageDataAsk } from "./UsageDataAsk";

/**
 * Slice 02, US-05. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 *
 * Mounted beside the real Footer rather than on its own, because the thing under test is that a
 * person ends up looking at the dialog. A test that asserted openDialog had been called would pass
 * against a provider whose dialog nothing renders, which is the wiring defect this slice is most
 * able to ship.
 */

const usageDataService = (state: {
	sending?: boolean;
	decision?: string | null;
	willAskAgain?: boolean;
	mayAsk?: boolean;
}) => ({
	getState: vi.fn().mockResolvedValue({
		sending: false,
		decision: null,
		willAskAgain: true,
		mayAsk: true,
		...state,
	}),
	recordDecision: vi.fn().mockResolvedValue("a-token"),
	revoke: vi.fn().mockResolvedValue(undefined),
	postEvents: vi.fn().mockResolvedValue(undefined),
	acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
});

// The real Footer carries router links, so it needs a router. Rendering it anyway - rather than a
// stub - is the point of this file: the thing under test is that somebody ends up looking at the
// dialog, and only the real Footer renders one.
const renderAsk = (service: ReturnType<typeof usageDataService>) =>
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ usageDataService: service })}
			>
				<UsageDataConsentProvider>
					<Footer />
					<UsageDataAsk />
				</UsageDataConsentProvider>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

describe("UsageDataAsk", () => {
	beforeEach(() => {
		localStorage.clear();
		sessionStorage.clear();
	});

	it("opens the dialog for a browser the server says is due", async () => {
		renderAsk(usageDataService({ mayAsk: true }));

		expect(await screen.findByRole("dialog")).toBeInTheDocument();
	});

	it("stays out of the way when the server says the browser is not due", async () => {
		const service = usageDataService({ mayAsk: false });
		renderAsk(service);

		// Wait for the state to have been fetched first. Asserting absence before the request
		// resolves passes against a component that would have opened the dialog a tick later.
		await waitFor(() => expect(service.getState).toHaveBeenCalled());

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	// AC-05.2. The record of having asked is written when the question is put, not when it is
	// answered - a browser that closes the dialog has answered nothing, and is exactly the browser
	// this has to remember.
	it("records that this browser has been asked, as it asks", async () => {
		renderAsk(usageDataService({ mayAsk: true }));

		await screen.findByRole("dialog");

		expect(localStorage.getItem("lighthouse:usagedata:asked")).not.toBeNull();
	});

	it("does not ask a browser that has already been asked and answered nothing", async () => {
		localStorage.setItem(
			"lighthouse:usagedata:asked",
			"2026-09-01T09:00:00.000Z",
		);
		const service = usageDataService({ mayAsk: true });
		renderAsk(service);

		await waitFor(() => expect(service.getState).toHaveBeenCalled());

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	// AC-05.6. The survey nudge got there first, so this waits for another session - consent
	// collected beside an unrelated request is not freely given.
	it("stays silent when another prompt already holds this session", async () => {
		sessionStorage.setItem("lighthouse:prompt-slot", "survey-nudge");
		const service = usageDataService({ mayAsk: true });
		renderAsk(service);

		await waitFor(() => expect(service.getState).toHaveBeenCalled());

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("takes the session slot when it does ask, so nothing else can appear beside it", async () => {
		renderAsk(usageDataService({ mayAsk: true }));

		await screen.findByRole("dialog");

		expect(sessionStorage.getItem("lighthouse:prompt-slot")).toBe("usage-data");
	});

	// AC-05.5 and AC-02.9 are one criterion: the sentence and the behaviour are the same promise,
	// so the boolean that decides whether this browser is asked again is the one that picks the
	// words. Two separate assertions could both pass while disagreeing with each other.
	it("tells a Community reader the question will come back", async () => {
		renderAsk(usageDataService({ mayAsk: true, willAskAgain: true }));

		const dialog = await screen.findByRole("dialog");

		expect(dialog).toHaveTextContent(/ask you again/i);
		expect(dialog).not.toHaveTextContent(/not ask you again/i);
	});

	it("tells a reader who will not be asked again that this is the last time", async () => {
		renderAsk(usageDataService({ mayAsk: true, willAskAgain: false }));

		const dialog = await screen.findByRole("dialog");

		expect(dialog).toHaveTextContent(/not ask you again/i);
	});

	// AC-05.3. There is no third button, and closing is not an answer - nothing is recorded on the
	// server and the footer indicator goes on saying what it said before.
	it("records no decision when the dialog is closed without one", async () => {
		const service = usageDataService({ mayAsk: true });
		renderAsk(service);

		await screen.findByRole("dialog");
		await userEvent.keyboard("{Escape}");

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(service.recordDecision).not.toHaveBeenCalled();
	});

	it("does not ask twice in one session", async () => {
		const service = usageDataService({ mayAsk: true });
		const { unmount } = renderAsk(service);

		await screen.findByRole("dialog");
		await userEvent.keyboard("{Escape}");
		unmount();

		renderAsk(service);
		await waitFor(() => expect(service.getState).toHaveBeenCalled());

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});
});
