import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../hooks/useUsageDataConsent";
import type { IUsageDataState } from "../../models/UsageData/UsageData";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import { ApiServiceContext } from "../Api/ApiServiceContext";
import {
	type IUsageDataService,
	UsageDataEventName,
} from "../Api/UsageDataService";
import { useUsageDataEventDetector } from "./usageDataEvents";
import { useUsageDataReporter } from "./usageDataReporter";

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const anAnswerOf = (
	overrides: Partial<IUsageDataState> = {},
): IUsageDataState => ({
	sending: true,
	decision: "Granted",
	mayAsk: false,
	reAskAfterDays: 90,
	administratorDisabled: false,
	...overrides,
});

/**
 * The reporter and the detector together, which is how they are mounted: the reporter records what
 * somebody did, and the detector is what hands anything in. Testing the reporter without it would
 * assert into a buffer nobody empties.
 *
 * The page is one this browser has no name for, so the detector notices nothing of its own and
 * everything handed in came from the reporter.
 */
const renderReporterBesideTheDetector = (
	state: IUsageDataState,
	getState?: IUsageDataService["getState"],
) => {
	const usageDataService: IUsageDataService = {
		getState: getState ?? vi.fn().mockResolvedValue(state),
		recordDecision: vi.fn().mockResolvedValue("freshly-minted-token"),
		revoke: vi.fn().mockResolvedValue(undefined),
		acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
		postEvents: vi.fn().mockResolvedValue(undefined),
	};

	const wrapper = ({ children }: { children: ReactNode }) =>
		createElement(
			MemoryRouter,
			{ initialEntries: ["/settings"] },
			createElement(
				ApiServiceContext.Provider,
				{ value: createMockApiServiceContext({ usageDataService }) },
				createElement(UsageDataConsentProvider, null, children),
			),
		);

	const rendered = renderHook(
		() => {
			useUsageDataEventDetector();
			return useUsageDataReporter();
		},
		{ wrapper },
	);

	return { ...rendered, usageDataService };
};

const settle = async (usageDataService: IUsageDataService) => {
	await waitFor(() => expect(usageDataService.getState).toHaveBeenCalled());
	await act(async () => {
		await Promise.resolve();
	});
};

/**
 * Somebody switching away or closing the lid, which is what hands in what is waiting without a
 * test having to wait out the clock the detector otherwise runs on.
 */
const hideTheTab = () => {
	vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");
	act(() => {
		document.dispatchEvent(new Event("visibilitychange"));
	});
};

const everythingHandedIn = (usageDataService: IUsageDataService) =>
	vi
		.mocked(usageDataService.postEvents)
		.mock.calls.flatMap(([, events]) => events);

describe("useUsageDataReporter", () => {
	afterEach(() => {
		localStorage.clear();
		vi.restoreAllMocks();
	});

	it("hands in what somebody did, from a browser that agreed", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: UsageDataEventName.TeamCreated });
		});
		hideTheTab();

		await waitFor(() => expect(usageDataService.postEvents).toHaveBeenCalled());
		expect(everythingHandedIn(usageDataService)).toEqual([
			expect.objectContaining({ name: UsageDataEventName.TeamCreated }),
		]);
	});

	it("sends no address with something that happened on no particular page", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: UsageDataEventName.TeamCreated });
		});
		hideTheTab();

		await waitFor(() => expect(usageDataService.postEvents).toHaveBeenCalled());
		expect(everythingHandedIn(usageDataService)[0]).not.toHaveProperty("route");
	});

	it("says which kind of work tracking system was connected", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({
				name: UsageDataEventName.WorkTrackingSystemConnected,
				workTrackingSystem: "Jira",
			});
		});
		hideTheTab();

		await waitFor(() => expect(usageDataService.postEvents).toHaveBeenCalled());
		expect(everythingHandedIn(usageDataService)).toEqual([
			expect.objectContaining({
				name: UsageDataEventName.WorkTrackingSystemConnected,
				workTrackingSystem: "Jira",
			}),
		]);
	});

	/**
	 * Refusing mints a token too, so a browser that said no is holding one. A reporter that decided
	 * from the token rather than from the answer would collect on behalf of exactly the person who
	 * asked us not to.
	 */
	it("records nothing for a browser that refused, token or no token", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-a-refusal-also-mints");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf({ sending: false, decision: "Declined" }),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: UsageDataEventName.TeamCreated });
		});
		hideTheTab();

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	it("records nothing on an instance whose administrator stopped usage data", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf({ sending: false, administratorDisabled: true }),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: UsageDataEventName.TeamCreated });
		});
		hideTheTab();

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	/**
	 * An answer that never arrived is not a yes. Every uncertainty in this feature resolves to not
	 * sending, and this is the one place a screen could quietly opt somebody in by being rendered
	 * before the server replied.
	 */
	it("records nothing while the answer about this browser has not arrived", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
			vi.fn().mockReturnValue(new Promise(() => {})),
		);

		act(() => {
			result.current({ name: UsageDataEventName.TeamCreated });
		});
		hideTheTab();

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	it("keeps two things somebody did in the order they were done", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: UsageDataEventName.TeamCreated });
			result.current({
				name: UsageDataEventName.WorkTrackingSystemConnected,
				workTrackingSystem: "Linear",
			});
		});
		hideTheTab();

		await waitFor(() => expect(usageDataService.postEvents).toHaveBeenCalled());
		expect(
			everythingHandedIn(usageDataService).map((event) => event.name),
		).toEqual([
			UsageDataEventName.TeamCreated,
			UsageDataEventName.WorkTrackingSystemConnected,
		]);
	});
});
