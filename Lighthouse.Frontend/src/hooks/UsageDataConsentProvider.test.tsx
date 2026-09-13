import { act, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IUsageDataState } from "../models/UsageData/UsageData";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IUsageDataService } from "../services/Api/UsageDataService";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import {
	type UsageDataConsent,
	UsageDataConsentProvider,
	useUsageDataConsent,
} from "./useUsageDataConsent";

const undecided: IUsageDataState = {
	sending: false,
	decision: null,
	willAskAgain: true,
	mayAsk: false,
	reAskAfterDays: 90,
};

const granted: IUsageDataState = {
	sending: true,
	decision: "Granted",
	willAskAgain: false,
	mayAsk: false,
	reAskAfterDays: 90,
};

// Stands in for the footer: the one place that asks somebody the question.
const theFootersCopy: { current: UsageDataConsent | null } = { current: null };

const TheFooter: React.FC = () => {
	theFootersCopy.current = useUsageDataConsent();
	return null;
};

// Stands in for the detector: it asks nobody anything and only acts on the answer.
const TheDetector: React.FC = () => {
	const { indicatorState } = useUsageDataConsent();
	return <span data-testid="what-the-detector-sees">{indicatorState}</span>;
};

const renderBothUnderOneProvider = () => {
	let theServersAnswer = undecided;

	const usageDataService: IUsageDataService = {
		getState: vi.fn(() => Promise.resolve(theServersAnswer)),
		recordDecision: vi.fn(() => {
			theServersAnswer = granted;
			return Promise.resolve("freshly-minted-token");
		}),
		revoke: vi.fn().mockResolvedValue(undefined),
		acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
		postEvents: vi.fn().mockResolvedValue(undefined),
	};

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ usageDataService })}
		>
			<UsageDataConsentProvider>
				<TheFooter />
				<TheDetector />
			</UsageDataConsentProvider>
		</ApiServiceContext.Provider>,
	);

	return { usageDataService };
};

const whatTheDetectorSees = () =>
	screen.getByTestId("what-the-detector-sees").textContent;

const pressAgreeInTheFooter = async () => {
	await act(async () => {
		await theFootersCopy.current?.decide("granted");
	});
};

afterEach(() => {
	theFootersCopy.current = null;
	localStorage.clear();
	vi.restoreAllMocks();
});

describe("UsageDataConsentProvider", () => {
	// The defect this guards against shipped, and every test passed over it because no test ever put
	// two of these on screen at once. Agreeing in the footer flipped the footer's own copy of the
	// answer and nothing else, so the indicator said data was being sent while the part that
	// actually collects it went on reading "not sending" until its own hourly refresh came round.
	// Somebody agreed, nothing happened, and then up to an hour later collection began on its own.
	it("makes a decision taken in one part of the screen the answer every other part reads", async () => {
		const { usageDataService } = renderBothUnderOneProvider();
		await waitFor(() => expect(whatTheDetectorSees()).toBe("not-sending"));

		await pressAgreeInTheFooter();

		expect(usageDataService.recordDecision).toHaveBeenCalledWith("granted");
		expect(whatTheDetectorSees()).toBe("sending");
	});

	// Asking once per part of the screen would work, which is why it survived: the cost is only
	// duplicate requests, and the request that carries the question is also the one that keeps this
	// browser's consent from ageing out, so it is not free.
	it("asks the server once for an answer several parts of the screen share", async () => {
		const { usageDataService } = renderBothUnderOneProvider();

		await waitFor(() => expect(whatTheDetectorSees()).toBe("not-sending"));

		expect(usageDataService.getState).toHaveBeenCalledTimes(1);
	});

	// Answering anyway, out of a copy of its own, is the defect coming back unnoticed. Refusing is
	// the only version anybody finds out about.
	it("refuses to answer where nothing above it holds the decision", () => {
		expect(() => render(<TheDetector />)).toThrow(/UsageDataConsentProvider/);
	});
});
