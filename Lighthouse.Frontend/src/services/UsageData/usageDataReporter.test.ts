import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../hooks/useUsageDataConsent";
import type { IUsageDataState } from "../../models/UsageData/UsageData";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import { ApiServiceContext } from "../Api/ApiServiceContext";
import type { IUsageDataService } from "../Api/UsageDataService";
import {
	FLUSH_INTERVAL_MS,
	useUsageDataEventDetector,
} from "./usageDataEvents";
import {
	type UsageDataCapabilityUse,
	useUsageDataReporter,
} from "./usageDataReporter";

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

/**
 * The names are written out here rather than imported, because a name that exists in the code
 * without a line on the usage data page is data leaving that nobody was told about - so the list
 * these are added to is the last thing to change, not the first.
 */
const TeamCreated = "TeamCreated" as UsageDataCapabilityUse["name"];
const WorkTrackingSystemConnected =
	"WorkTrackingSystemConnected" as UsageDataCapabilityUse["name"];

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
 * somebody did, and the detector's clock is what hands anything in. Testing the reporter without it
 * would assert into a buffer nobody empties.
 */
const renderReporterBesideTheDetector = (state: IUsageDataState) => {
	const usageDataService: IUsageDataService = {
		getState: vi.fn().mockResolvedValue(state),
		recordDecision: vi.fn().mockResolvedValue("freshly-minted-token"),
		revoke: vi.fn().mockResolvedValue(undefined),
		acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
		postEvents: vi.fn().mockResolvedValue(undefined),
	};

	const wrapper = ({ children }: { children: ReactNode }) =>
		createElement(
			MemoryRouter,
			{ initialEntries: ["/"] },
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

const everythingHandedIn = (usageDataService: IUsageDataService) =>
	vi
		.mocked(usageDataService.postEvents)
		.mock.calls.flatMap(([, events]) => events);

describe.skip("useUsageDataReporter", () => {
	afterEach(() => {
		vi.useRealTimers();
		localStorage.clear();
		vi.restoreAllMocks();
	});

	it("hands in what somebody did, from a browser that agreed", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		vi.useFakeTimers();

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: TeamCreated });
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(everythingHandedIn(usageDataService)).toEqual([
			expect.objectContaining({ name: TeamCreated }),
		]);
	});

	it("sends no address with something that happened on no particular page", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		vi.useFakeTimers();

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: TeamCreated });
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(everythingHandedIn(usageDataService)[0]).not.toHaveProperty("route");
	});

	it("says which kind of work tracking system was connected", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		vi.useFakeTimers();

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({
				name: WorkTrackingSystemConnected,
				workTrackingSystem: "Jira",
			});
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(everythingHandedIn(usageDataService)).toEqual([
			expect.objectContaining({
				name: WorkTrackingSystemConnected,
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
		vi.useFakeTimers();

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf({ sending: false, decision: "Declined" }),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: TeamCreated });
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	it("records nothing on an instance whose administrator stopped usage data", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		vi.useFakeTimers();

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf({ sending: false, administratorDisabled: true }),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: TeamCreated });
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	/**
	 * An answer that never arrived is not a yes. Every uncertainty in this feature resolves to not
	 * sending, and this is the one place a screen could quietly opt somebody in by being rendered
	 * before the server replied.
	 */
	it("records nothing while the answer about this browser has not arrived", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		vi.useFakeTimers();

		const usageDataService: IUsageDataService = {
			getState: vi.fn().mockReturnValue(new Promise(() => {})),
			recordDecision: vi.fn(),
			revoke: vi.fn(),
			acknowledgeAsked: vi.fn(),
			postEvents: vi.fn().mockResolvedValue(undefined),
		};

		const wrapper = ({ children }: { children: ReactNode }) =>
			createElement(
				MemoryRouter,
				{ initialEntries: ["/"] },
				createElement(
					ApiServiceContext.Provider,
					{ value: createMockApiServiceContext({ usageDataService }) },
					createElement(UsageDataConsentProvider, null, children),
				),
			);

		const { result } = renderHook(
			() => {
				useUsageDataEventDetector();
				return useUsageDataReporter();
			},
			{ wrapper },
		);

		act(() => {
			result.current({ name: TeamCreated });
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	it("keeps two things somebody did in the order they were done", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-this-browser-holds");
		vi.useFakeTimers();

		const { result, usageDataService } = renderReporterBesideTheDetector(
			anAnswerOf(),
		);
		await settle(usageDataService);

		act(() => {
			result.current({ name: TeamCreated });
			result.current({
				name: WorkTrackingSystemConnected,
				workTrackingSystem: "Linear",
			});
		});
		await act(async () => {
			vi.advanceTimersByTime(FLUSH_INTERVAL_MS);
		});

		expect(
			everythingHandedIn(usageDataService).map((event) => event.name),
		).toEqual([TeamCreated, WorkTrackingSystemConnected]);
	});
});
