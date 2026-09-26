import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import {
	aRealityCheckAnswer,
	expectALine,
	pressRunRealityCheck,
	renderTheForecastTab,
	setMatchMedia,
	theDialogWithTheAnswer,
	theRealityCheckDialog,
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
	setMatchMedia(false);
});

describe("@us-01 @kpi-OUT-4172-reality-check-used-outside-the-vendor reporting a reality check", () => {
	it(`is on the list of names the browser may send, as the word itself (${PENDING})`, () => {
		expect(
			(UsageDataEventName as Record<string, string>)[
				TEAM_FORECAST_REALITY_CHECK_RUN
			],
		).toBe(TEAM_FORECAST_REALITY_CHECK_RUN);
	});

	it(`reports a run once, after the answer came back and not when the button was pressed (${PENDING})`, async () => {
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

	it(`@error reports a run whose every check was too thin to evaluate, because that is still an answer (${PENDING})`, async () => {
		await theDialogWithTheAnswer(
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
		);

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith(aRealityCheckWasRun);
	});

	it(`@error reports a run for a Team that forecasts from fixed dates (${PENDING})`, async () => {
		await theDialogWithTheAnswer(
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

	it(`@error reports nothing for a request that failed (${PENDING})`, async () => {
		runRealityCheck.mockRejectedValue(
			new Error("The reality check could not be run"),
		);
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await within(dialog).findByText(/the reality check could not be run/i);

		expect(reportUsage).not.toHaveBeenCalled();
	});

	it(`never reports a reality check as a forecast run by hand (${PENDING})`, async () => {
		await theDialogWithTheAnswer(runRealityCheck, aRealityCheckAnswer());

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).not.toHaveBeenCalledWith({
			name: UsageDataEventName.TeamManualForecastRun,
		});
	});
});

/**
 * Once the answer opens in a dialog (Story 6094, slice 04) the event means an answer somebody was shown.
 * Run again and a reopen that bring an answer each report again; reopening while a check is still running
 * shows that same check and reports it once; an answer that arrives after the dialog was closed was shown
 * to nobody and is not reported.
 */
const PENDING_DIALOG = "the reality check dialog is not built yet";

const anAnswerStillOnItsWay = () => {
	let answer: (value: unknown) => void = () => {};
	runRealityCheck.mockReturnValueOnce(
		new Promise((resolve) => {
			answer = resolve;
		}),
	);
	return (value: unknown) => act(async () => answer(value));
};

const theAnswerIsShownIn = async (dialog: HTMLElement) => {
	await waitFor(() => expectALine(dialog, /forecast runs were checked/i));
};

const closeTheDialog = async () => {
	await userEvent.keyboard("{Escape}");
	await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
};

describe("@us-04 @slice-04 @kpi-OUT-6094-use-survives-the-extra-click reporting a reality check from its dialog", () => {
	beforeEach(() => {
		setMatchMedia(false);
	});

	it.skip(`reports once when the dialog shows the answer, and not when it opened (${PENDING_DIALOG})`, async () => {
		const answerArrives = anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);

		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		expect(reportUsage).not.toHaveBeenCalled();

		await answerArrives(aRealityCheckAnswer());
		await theAnswerIsShownIn(dialog);

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith(aRealityCheckWasRun);
	});

	it.skip(`reports again when Run again brings a fresh answer (${PENDING_DIALOG})`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await theAnswerIsShownIn(dialog);

		await userEvent.click(
			within(dialog).getByRole("button", { name: /^run again$/i }),
		);

		await waitFor(() => expect(reportUsage).toHaveBeenCalledTimes(2));
		expect(runRealityCheck).toHaveBeenCalledTimes(2);
	});

	it.skip(`reports again when the dialog is closed and opened again after an answer (${PENDING_DIALOG})`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		await theAnswerIsShownIn(await theRealityCheckDialog());
		await closeTheDialog();

		await pressRunRealityCheck(group);
		await theAnswerIsShownIn(await theRealityCheckDialog());

		expect(reportUsage).toHaveBeenCalledTimes(2);
	});

	it.skip(`@error reopening while the check is still running shows that same check and reports it once (${PENDING_DIALOG})`, async () => {
		const answerArrives = anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		await theRealityCheckDialog();
		await closeTheDialog();

		await pressRunRealityCheck(group);
		const reopened = await theRealityCheckDialog();
		await answerArrives(aRealityCheckAnswer());
		await theAnswerIsShownIn(reopened);

		expect(runRealityCheck).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	it.skip(`@error an answer that arrives after the dialog was closed was shown to nobody and is not reported, and reopening asks afresh (${PENDING_DIALOG})`, async () => {
		const droppedAnswerArrives = anAnswerStillOnItsWay();
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		await theRealityCheckDialog();
		await closeTheDialog();

		await droppedAnswerArrives(aRealityCheckAnswer());
		expect(reportUsage).not.toHaveBeenCalled();

		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		await pressRunRealityCheck(group);
		await theAnswerIsShownIn(await theRealityCheckDialog());

		expect(runRealityCheck).toHaveBeenCalledTimes(2);
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	it.skip(`@error reports nothing when Run again fails (${PENDING_DIALOG})`, async () => {
		runRealityCheck
			.mockResolvedValueOnce(aRealityCheckAnswer())
			.mockRejectedValueOnce(new Error("The reality check could not be run"));
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await theAnswerIsShownIn(dialog);

		await userEvent.click(
			within(dialog).getByRole("button", { name: /^run again$/i }),
		);
		await within(dialog).findByText(/the reality check could not be run/i);

		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	it.skip(`never reports a check from the dialog as a forecast run by hand, however often it runs (${PENDING_DIALOG})`, async () => {
		runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
		const group = renderTheForecastTab(runRealityCheck);
		await pressRunRealityCheck(group);
		const dialog = await theRealityCheckDialog();
		await theAnswerIsShownIn(dialog);
		await userEvent.click(
			within(dialog).getByRole("button", { name: /^run again$/i }),
		);
		await waitFor(() => expect(reportUsage).toHaveBeenCalledTimes(2));

		for (const [event] of reportUsage.mock.calls) {
			expect(event).toEqual(aRealityCheckWasRun);
		}
	});
});
