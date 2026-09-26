import { act, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import {
	aRealityCheckAnswer,
	pressRunRealityCheck,
	renderTheForecastTab,
	theAnswerIn,
} from "../../../tests/RealityCheckFixture";

/**
 * What the Team Forecast tab reports when somebody runs a Forecast Reality Check, and when it reports
 * nothing. Whether this browser agreed is the reporter's business, not the tab's, so the reporter
 * stands in here and every call it receives is one the tab chose to make.
 *
 * The event means somebody got an answer back. A check whose every window was too thin to evaluate is
 * still an answer - it says so, in words - and so is a check for a Team that forecasts from fixed dates.
 * A request that failed gave nobody anything, so it is not reported.
 *
 * The name travels as the word, never a number: the server writes and reads these names as words, so a
 * numbered mirror would compare false against everything the server sends.
 */

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));
vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canUseNewItemForecaster: true,
		newItemForecasterTooltip: "",
		isLoading: false,
		licenseStatus: {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		},
	}),
}));

vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	default: ({
		title,
		children,
	}: {
		title: string;
		children: React.ReactNode;
	}) => (
		<section aria-label={title}>
			<h2>{title}</h2>
			{children}
		</section>
	),
}));

vi.mock("./ManualForecaster", () => ({
	default: () => <div data-testid="manual-forecaster" />,
}));

vi.mock("./NewItemForecaster", () => ({
	default: () => <div data-testid="new-item-forecaster" />,
}));

vi.mock("./BacktestForecaster", () => ({
	default: () => <div data-testid="backtest-forecaster" />,
}));

const PENDING = "the Forecast Reality Check is not built yet";

const TEAM_FORECAST_REALITY_CHECK_RUN = "TeamForecastRealityCheckRun";

const aRealityCheckWasRun = { name: TEAM_FORECAST_REALITY_CHECK_RUN };

const runRealityCheck = vi.fn();

beforeEach(() => {
	vi.clearAllMocks();
});

describe("@us-01 @kpi-OUT-4172-reality-check-used-outside-the-vendor reporting a reality check", () => {
	it(`is on the list of names the browser may send, as the word itself (${PENDING})`, () => {
		expect(
			(UsageDataEventName as Record<string, string>)[
				TEAM_FORECAST_REALITY_CHECK_RUN
			],
		).toBe(TEAM_FORECAST_REALITY_CHECK_RUN);
	});

	it.skip(`reports a run once, after the answer came back and not when the button was pressed (${PENDING})`, async () => {
		let answer: (value: unknown) => void = () => {};
		runRealityCheck.mockReturnValue(
			new Promise((resolve) => {
				answer = resolve;
			}),
		);
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		expect(reportUsage).not.toHaveBeenCalled();

		await act(async () => answer(aRealityCheckAnswer()));

		await waitFor(() => expect(reportUsage).toHaveBeenCalledTimes(1));
		expect(reportUsage).toHaveBeenCalledWith(aRealityCheckWasRun);
	});

	it.skip(`@error reports a run whose every check was too thin to evaluate, because that is still an answer (${PENDING})`, async () => {
		await theAnswerIn(
			runRealityCheck,
			aRealityCheckAnswer({
				soundWindowDays: [],
				determination: "NotEnoughEvidence",
				standing: "NotDetermined",
				unevaluatedWindowDays: [14, 30, 60, 90],
				readings: {
					50: "NotEvaluated",
					70: "NotEvaluated",
					85: "NotEvaluated",
					95: "NotEvaluated",
				},
				unevaluable: [14, 30, 60, 90].flatMap((window) =>
					[7, 14, 28, 56].map((horizon) => ({
						window,
						horizon,
						reason: "TooFewActiveDays" as const,
						daysWithCompletedWork: 1,
					})),
				),
			}),
			/could not run/i,
		);

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith(aRealityCheckWasRun);
	});

	it.skip(`@error reports a run for a Team that forecasts from fixed dates (${PENDING})`, async () => {
		await theAnswerIn(
			runRealityCheck,
			aRealityCheckAnswer({
				currentSettingDays: 45,
				currentSettingWasTested: false,
				standing: "NotTested",
				notTestedReason: "UsesFixedDates",
			}),
		);

		expect(reportUsage).toHaveBeenCalledWith(aRealityCheckWasRun);
	});

	it.skip(`@error reports nothing for a request that failed (${PENDING})`, async () => {
		runRealityCheck.mockRejectedValue(
			new Error("The reality check could not be run"),
		);
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		await screen.findByText(/the reality check could not be run/i);

		expect(reportUsage).not.toHaveBeenCalled();
	});

	it.skip(`never reports a reality check as a forecast run by hand (${PENDING})`, async () => {
		await theAnswerIn(runRealityCheck, aRealityCheckAnswer());

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).not.toHaveBeenCalledWith({
			name: UsageDataEventName.TeamManualForecastRun,
		});
	});
});
