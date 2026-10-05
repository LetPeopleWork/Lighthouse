import { act, renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../../../hooks/useUsageDataConsent";
import type {
	IRefinementView,
	RefinementVerdict,
} from "../../../../models/Refinement/Refinement";
import type { IUsageDataState } from "../../../../models/UsageData/UsageData";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import {
	type IUsageDataService,
	UsageDataEventName,
} from "../../../../services/Api/UsageDataService";
import {
	forgetWhatWasNoticed,
	takeWhatWasNoticed,
} from "../../../../services/UsageData/usageDataBuffer";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import {
	aNeedOfFiveToEight,
	GRAVITY_TEAM_ID,
	gravitysRefinement,
	THURSDAY_THE_EIGHTH,
} from "../../../../tests/RefinementTabTestKit";
import type { ShownRefinement } from "./useRefinement";
import { useVerdictShownReporter } from "./useVerdictShownReporter";

/**
 * Whether this browser may send usage data is the server's answer, and it can arrive after the tab has
 * already shown its verdict. The opening still counts once the answer says yes.
 */

const NEBULA_TEAM_ID = 8;

const aRefinementDayShowing = (
	readyCount: number,
	verdict: RefinementVerdict = "Below",
	rows?: IRefinementView["workItems"],
): IRefinementView =>
	gravitysRefinement(
		{
			readyCount,
			nextRefinementDate: THURSDAY_THE_EIGHTH,
			daysUntilNextRefinement: 0,
			isRefinementDay: true,
			need: aNeedOfFiveToEight({ verdict }),
		},
		rows,
	);

const aBrowserThatAgreed: IUsageDataState = {
	sending: true,
	decision: "Granted",
	mayAsk: false,
	reAskAfterDays: 90,
	administratorDisabled: false,
};

const aUsageDataService = (
	getState: IUsageDataService["getState"],
): IUsageDataService => ({
	getState,
	recordDecision: vi.fn(),
	revoke: vi.fn(),
	acknowledgeAsked: vi.fn(),
	postEvents: vi.fn(),
});

const consentFrom = (usageDataService: IUsageDataService) => {
	const services = createMockApiServiceContext({ usageDataService });
	return ({ children }: { children: ReactNode }) => (
		<ApiServiceContext.Provider value={services}>
			<UsageDataConsentProvider>{children}</UsageDataConsentProvider>
		</ApiServiceContext.Provider>
	);
};

const renderWithConsentStillOnItsWay = (refinement: IRefinementView) => {
	let answerConsent: (state: IUsageDataState) => void = () => {};
	const wrapper = consentFrom(
		aUsageDataService(
			vi.fn(
				() =>
					new Promise<IUsageDataState>((resolve) => {
						answerConsent = resolve;
					}),
			),
		),
	);

	const rendered = renderHook(
		({ shown }: { shown: IRefinementView }) =>
			useVerdictShownReporter(GRAVITY_TEAM_ID, {
				teamId: GRAVITY_TEAM_ID,
				view: shown,
			}),
		{ wrapper, initialProps: { shown: refinement } },
	);

	return {
		...rendered,
		consentArrives: (state: IUsageDataState) =>
			act(async () => {
				answerConsent(state);
			}),
	};
};

/** The tab as a browser that already agreed sees it, with nothing read yet. */
const renderOnABrowserThatAgreed = async () => {
	const wrapper = consentFrom(
		aUsageDataService(vi.fn().mockResolvedValue(aBrowserThatAgreed)),
	);
	const rendered = renderHook(
		({ teamId, shown }: { teamId: number; shown: ShownRefinement | null }) =>
			useVerdictShownReporter(teamId, shown),
		{
			wrapper,
			initialProps: {
				teamId: GRAVITY_TEAM_ID,
				shown: null as ShownRefinement | null,
			},
		},
	);
	await act(async () => {
		await Promise.resolve();
	});
	return rendered;
};

const verdictsReported = () =>
	takeWhatWasNoticed().filter(
		(event) => event.name === UsageDataEventName.TeamRefinementDayVerdictShown,
	);

describe("useVerdictShownReporter", () => {
	beforeEach(() => {
		forgetWhatWasNoticed();
	});

	afterEach(() => {
		forgetWhatWasNoticed();
	});

	it("reports the opening once this browser's consent arrives after the verdict was shown", async () => {
		const { consentArrives, rerender } = renderWithConsentStillOnItsWay(
			aRefinementDayShowing(3),
		);

		await consentArrives(aBrowserThatAgreed);
		rerender({ shown: aRefinementDayShowing(4) });

		expect(verdictsReported()).toEqual([
			expect.objectContaining({ refinementVerdict: "Below" }),
		]);
	});

	it("reports once per Team the tab is opened on, however often that Team's Refinement is read again", async () => {
		const { rerender } = await renderOnABrowserThatAgreed();
		const gravitys = aRefinementDayShowing(3, "Below");

		rerender({
			teamId: GRAVITY_TEAM_ID,
			shown: { teamId: GRAVITY_TEAM_ID, view: gravitys },
		});
		rerender({
			teamId: NEBULA_TEAM_ID,
			shown: { teamId: GRAVITY_TEAM_ID, view: gravitys },
		});
		rerender({
			teamId: NEBULA_TEAM_ID,
			shown: { teamId: NEBULA_TEAM_ID, view: aRefinementDayShowing(6, "In") },
		});
		rerender({
			teamId: NEBULA_TEAM_ID,
			shown: { teamId: NEBULA_TEAM_ID, view: aRefinementDayShowing(7, "In") },
		});

		expect(verdictsReported()).toEqual([
			expect.objectContaining({ refinementVerdict: "Below" }),
			expect.objectContaining({ refinementVerdict: "In" }),
		]);
	});

	// With no Work Items in Refinement the tab says so instead of showing a verdict, so there is none to count.
	it("reports nothing on a Refinement day with no Work Items in Refinement", async () => {
		const { rerender } = await renderOnABrowserThatAgreed();

		rerender({
			teamId: GRAVITY_TEAM_ID,
			shown: {
				teamId: GRAVITY_TEAM_ID,
				view: aRefinementDayShowing(0, "Below", []),
			},
		});

		expect(verdictsReported()).toEqual([]);
	});
});
