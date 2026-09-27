import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import dayjs from "dayjs";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import {
	aRealityCheckAnswer,
	OCEAN_EXPLORER,
	readingOf,
	setMatchMedia,
	tabUntilFocused,
	theRealityCheckDialog,
} from "../../../tests/RealityCheckFixture";
import TeamForecastView from "./TeamForecastView";

/**
 * Where the reality check is started. "Run reality check" moves under the Forecast
 * Backtesting inputs - after the date fields, the historical-window control and, when shown, the filter
 * switch - and above any single back-test result, with an info icon before it that says in plain words
 * what the check does.
 *
 * Unlike the other reality-check specs this file renders the real back-test forecaster, because where the
 * button sits among its inputs is the thing asserted. Only the back-test's result display and chart are
 * stood in for. What the specs need from the markup: the icon is a button named "What does the reality
 * check do?", whose tooltip describes it (the name stays the question), and it opens nothing.
 */

const PENDING = "06a: the trigger has not moved under the inputs yet";

const THE_QUESTION = "What does the reality check do?";

const { terms } = vi.hoisted(() => ({
	terms: new Map<string, string>(),
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.get(key) ?? key,
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

vi.mock("./BacktestResultDisplay", () => ({
	default: () => <div data-testid="backtest-result-display" />,
}));

vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: () => <div data-testid="bar-run-chart" />,
}));

const runRealityCheck = vi.fn();

const aSingleBacktestResult = (): BacktestResult => ({
	startDate: dayjs().subtract(31, "day").toDate(),
	endDate: dayjs().toDate(),
	historicalStartDate: dayjs().subtract(76, "day").toDate(),
	historicalEndDate: dayjs().subtract(32, "day").toDate(),
	percentiles: [
		new HowManyForecast(50, 10),
		new HowManyForecast(70, 12),
		new HowManyForecast(85, 15),
		new HowManyForecast(95, 18),
	],
	actualThroughput: 12,
});

const aTeamServiceWithoutAForecastFilter = () => {
	const teamService = createMockTeamService();
	vi.mocked(teamService.getTeamSettings).mockResolvedValue({} as ITeamSettings);
	return teamService;
};

/** The Forecasts tab with the real back-test forecaster; returns the Forecast Backtesting group. */
const renderTheForecastTabWithItsBacktestInputs = (
	teamService = aTeamServiceWithoutAForecastFilter(),
): HTMLElement => {
	const teamMetricsService = createMockTeamMetricsService();
	vi.mocked(teamMetricsService.getForecastInputCandidates).mockResolvedValue({
		currentWipCount: 3,
		backlogCount: 12,
		features: [],
	});
	const forecastService = {
		runManualForecast: vi.fn(),
		runItemPrediction: vi.fn().mockResolvedValue({}),
		runBacktest: vi.fn().mockResolvedValue(aSingleBacktestResult()),
		runRealityCheck,
	};

	render(
		<SnackbarErrorHandler>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					forecastService:
						forecastService as unknown as IApiServiceContext["forecastService"],
					teamMetricsService,
					teamService,
				})}
			>
				<TeamForecastView team={OCEAN_EXPLORER} />
			</ApiServiceContext.Provider>
		</SnackbarErrorHandler>,
	);

	return screen.getByRole("region", { name: "Forecast Backtesting" });
};

const theTrigger = (group: HTMLElement) =>
	within(group).getByRole("button", { name: /^run reality check$/i });

const follows = (earlier: Element, later: Element): boolean =>
	(earlier.compareDocumentPosition(later) &
		Node.DOCUMENT_POSITION_FOLLOWING) !==
	0;

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
	terms.set(TERMINOLOGY_KEYS.THROUGHPUT, "Throughput");
	runRealityCheck.mockResolvedValue(aRealityCheckAnswer());
});

describe("@us-06 @slice-06a @driving_port the check is started from under the back-test inputs", () => {
	// Green today and after the trigger moves: the single back-test keeps working beside its new neighbour, and this
	// is also what proves the harness can bring a back-test result into the group.
	it(`the single back-test beside the check still shows its result once an input changes`, async () => {
		const group = renderTheForecastTabWithItsBacktestInputs();

		fireEvent.change(
			within(group).getByLabelText(/Historical Window \(Days\)/i),
			{ target: { value: "45" } },
		);

		expect(
			await within(group).findByRole(
				"tablist",
				{ name: "Backtest result tabs" },
				{ timeout: 3000 },
			),
		).toBeInTheDocument();
		expect(theTrigger(group)).toBeInTheDocument();
	});

	it.skip(`"Run reality check" sits after the back-test inputs, with its explanation just before it, and above a single back-test result once there is one (${PENDING})`, async () => {
		const group = renderTheForecastTabWithItsBacktestInputs();
		const windowField = within(group).getByLabelText(
			/Historical Window \(Days\)/i,
		);
		const trigger = theTrigger(group);

		expect(follows(windowField, trigger)).toBe(true);
		const explanation = within(group).getByRole("button", {
			name: THE_QUESTION,
		});
		expect(follows(windowField, explanation)).toBe(true);
		expect(follows(explanation, trigger)).toBe(true);

		fireEvent.change(windowField, { target: { value: "45" } });
		const result = await within(group).findByRole(
			"tablist",
			{ name: "Backtest result tabs" },
			{ timeout: 3000 },
		);
		expect(follows(trigger, result)).toBe(true);
	});

	it.skip(`with the forecast filter switch shown, the trigger comes after the switch too (${PENDING})`, async () => {
		const teamService = createMockTeamService();
		const settings = Promise.resolve({
			forecastFilterRuleSetJson: JSON.stringify({ conditions: [{}] }),
		} as ITeamSettings);
		vi.mocked(teamService.getTeamSettings).mockReturnValue(settings);
		const group = renderTheForecastTabWithItsBacktestInputs(teamService);
		await act(async () => {
			await settings;
		});

		const filterSwitch = await within(group).findByLabelText(
			/^Use filtered Throughput$/,
		);
		expect(follows(filterSwitch, theTrigger(group))).toBe(true);
	});

	it.skip.each([
		{ how: "keyboard focus", arriveAt: tabUntilFocused },
		{
			how: "hover",
			arriveAt: (element: HTMLElement) => userEvent.hover(element),
		},
	])(
		`on $how the info icon explains in plain words what the check does, and keeps the question as its name (${PENDING})`,
		async ({ arriveAt }) => {
			const group = renderTheForecastTabWithItsBacktestInputs();
			const explanation = within(group).getByRole("button", {
				name: THE_QUESTION,
			});

			await arriveAt(explanation);
			const tooltip = await screen.findByRole("tooltip");

			expect(readingOf(tooltip)).toBe(
				"Replays this Team's recent forecasts — each recent period, forecast from several sampling windows — and compares every one with the Work Items actually completed.",
			);
			expect(explanation).toHaveAccessibleName(THE_QUESTION);
			expect(explanation).toHaveAccessibleDescription(readingOf(tooltip));
		},
	);

	it.skip(`@error pressing the info icon opens nothing and runs no check (${PENDING})`, async () => {
		const group = renderTheForecastTabWithItsBacktestInputs();

		const explanation = within(group).getByRole("button", {
			name: THE_QUESTION,
		});

		await userEvent.click(explanation);

		expect(screen.queryByRole("dialog")).toBeNull();
		expect(document.body.querySelector(".MuiPopover-root")).toBeNull();
		expect(explanation).not.toHaveAttribute("aria-expanded", "true");
		expect(runRealityCheck).not.toHaveBeenCalled();
	});

	it.skip(`pressing "Run reality check" from its new place still opens the dialog and runs one check (${PENDING})`, async () => {
		const group = renderTheForecastTabWithItsBacktestInputs();
		const explanation = within(group).getByRole("button", {
			name: THE_QUESTION,
		});

		await userEvent.click(theTrigger(group));

		expect(await theRealityCheckDialog()).toBeInTheDocument();
		expect(runRealityCheck).toHaveBeenCalledTimes(1);
		expect(explanation).toBeInTheDocument();
	});

	it.skip(`the explanation speaks the instance's own words for Team and Work Items (${PENDING})`, async () => {
		terms.set(TERMINOLOGY_KEYS.TEAM, "Squad");
		terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Tickets");
		const group = renderTheForecastTabWithItsBacktestInputs();

		await tabUntilFocused(
			within(group).getByRole("button", { name: THE_QUESTION }),
		);
		const tooltip = await screen.findByRole("tooltip");

		expect(readingOf(tooltip)).toBe(
			"Replays this Squad's recent forecasts — each recent period, forecast from several sampling windows — and compares every one with the Tickets actually completed.",
		);
	});
});
