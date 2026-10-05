import { act, renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../../../hooks/useUsageDataConsent";
import type { IRefinementView } from "../../../../models/Refinement/Refinement";
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
import { useVerdictShownReporter } from "./useVerdictShownReporter";

/**
 * Whether this browser may send usage data is the server's answer, and it can arrive after the tab has
 * already shown its verdict. The opening still counts once the answer says yes.
 */

const aRefinementDayShowing = (readyCount: number): IRefinementView =>
	gravitysRefinement({
		readyCount,
		nextRefinementDate: THURSDAY_THE_EIGHTH,
		daysUntilNextRefinement: 0,
		isRefinementDay: true,
		need: aNeedOfFiveToEight({ verdict: "Below" }),
	});

const aBrowserThatAgreed: IUsageDataState = {
	sending: true,
	decision: "Granted",
	mayAsk: false,
	reAskAfterDays: 90,
	administratorDisabled: false,
};

const renderWithConsentStillOnItsWay = (refinement: IRefinementView) => {
	let answerConsent: (state: IUsageDataState) => void = () => {};
	const usageDataService: IUsageDataService = {
		getState: vi.fn(
			() =>
				new Promise<IUsageDataState>((resolve) => {
					answerConsent = resolve;
				}),
		),
		recordDecision: vi.fn(),
		revoke: vi.fn(),
		acknowledgeAsked: vi.fn(),
		postEvents: vi.fn(),
	};
	const wrapper = ({ children }: { children: ReactNode }) => (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ usageDataService })}
		>
			<UsageDataConsentProvider>{children}</UsageDataConsentProvider>
		</ApiServiceContext.Provider>
	);

	const rendered = renderHook(
		({ shown }: { shown: IRefinementView }) =>
			useVerdictShownReporter(GRAVITY_TEAM_ID, shown),
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
});
