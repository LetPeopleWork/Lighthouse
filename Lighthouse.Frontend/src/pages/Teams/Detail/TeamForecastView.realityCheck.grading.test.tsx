import { within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ForecastGrade } from "../../../models/Forecasts/RealityCheckResult";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	aRealityCheckAnswer,
	HORIZON_DAYS,
	levelCell,
	periodGroup,
	type RealityCheckAnswerOptions,
	setMatchMedia,
	theDialogWithTheAnswer,
	thePeriodChecked,
	theTableIn,
	windowRow,
} from "../../../tests/RealityCheckFixture";
import { appColors } from "../../../utils/theme/colors";

/**
 * How close each forecast landed, on Nick Brown's scale. Every graded cell is shaded
 * green when it held and red when it did not - green deepens the closer it landed, red the further it
 * missed. These specs pin the fill each cell is drawn in; what a cell says in words is pinned by the
 * compact specs.
 */

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

// Renders the slot the reality check's trigger moves into, so the button is found wherever it sits.
vi.mock("./BacktestForecaster", () => ({
	default: ({ realityCheck }: { realityCheck?: React.ReactNode }) => (
		<div data-testid="backtest-forecaster">{realityCheck}</div>
	),
}));

const runRealityCheck = vi.fn();

const theDialogFor = (options: RealityCheckAnswerOptions) =>
	theDialogWithTheAnswer(runRealityCheck, aRealityCheckAnswer(options));

const gradeFill = (grade: ForecastGrade): string =>
	appColors.forecastGrade[grade];

const oceanExplorerOverEightWeeks = (): RealityCheckAnswerOptions => ({
	checks: thePeriodChecked(56, {
		at95: 31,
		at85: 36,
		at70: 40,
		at50: 48,
		actual: 42,
	}),
});

beforeEach(() => {
	vi.clearAllMocks();
	setMatchMedia(false);
	terms.clear();
	terms.set(TERMINOLOGY_KEYS.TEAM, "Team");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEMS, "Work Items");
	terms.set(TERMINOLOGY_KEYS.WORK_ITEM, "Work Item");
});

describe("@us-05 @slice-05 each forecast graded by how close it landed", () => {
	it(`@kpi-OUT-6094-no-percentage-without-its-work-items a forecast that held is shaded by how close it landed - Ocean Explorer's 30-day row over 8 weeks`, async () => {
		const dialog = await theDialogFor(oceanExplorerOverEightWeeks());
		const row = windowRow(periodGroup(theTableIn(dialog), 56), 30);

		expect(levelCell(row, 95)).toHaveStyle({
			backgroundColor: gradeFill("HeldOver25"),
		});
		expect(levelCell(row, 85)).toHaveStyle({
			backgroundColor: gradeFill("Held10To25"),
		});
		expect(levelCell(row, 70)).toHaveStyle({
			backgroundColor: gradeFill("HeldWithin10"),
		});
	});

	it(`@kpi-OUT-6094-no-percentage-without-its-work-items a forecast that did not hold is graded by how far it fell short - the same row's 50th`, async () => {
		const dialog = await theDialogFor(oceanExplorerOverEightWeeks());
		const cell = levelCell(
			windowRow(periodGroup(theTableIn(dialog), 56), 30),
			50,
		);

		expect(cell).toHaveStyle({ backgroundColor: gradeFill("NotHeld10To25") });
	});

	it(`@error @kpi-OUT-6094-no-percentage-without-its-work-items a one-Work-Item miss on a small period shows the Work Item beside the percentage - Coastal Survey's week`, async () => {
		const dialog = await theDialogFor({
			teamName: "Coastal Survey",
			checks: [
				{
					window: 30,
					horizon: 7,
					at95: 1,
					at85: 2,
					at70: 3,
					at50: 4,
					actual: 3,
				},
			],
		});
		const cell = levelCell(
			windowRow(periodGroup(theTableIn(dialog), 7), 30),
			50,
		);

		expect(cell).toHaveStyle({ backgroundColor: gradeFill("NotHeldOver25") });
	});

	it(`@error @kpi-OUT-6094-no-percentage-without-its-work-items a period in which nothing was delivered has no percentage: 0 against 0 held exactly, 2 against 0 did not hold by the most - Harbour Pilots' week`, async () => {
		const dialog = await theDialogFor({
			teamName: "Harbour Pilots",
			checks: [
				{
					window: 30,
					horizon: 7,
					at95: 0,
					at85: 1,
					at70: 1,
					at50: 2,
					actual: 0,
				},
			],
		});
		const row = windowRow(periodGroup(theTableIn(dialog), 7), 30);

		expect(levelCell(row, 95)).toHaveStyle({
			backgroundColor: gradeFill("HeldWithin10"),
		});
		expect(levelCell(row, 50)).toHaveStyle({
			backgroundColor: gradeFill("NotHeldOver25"),
		});
	});

	it.each<{
		actual: number;
		forecast: number;
		shown: string;
		grade: ForecastGrade;
	}>([
		{ actual: 40, forecast: 36, shown: "10%", grade: "HeldWithin10" },
		{ actual: 39, forecast: 35, shown: "11%", grade: "Held10To25" },
		{ actual: 100, forecast: 75, shown: "25%", grade: "Held10To25" },
		{ actual: 99, forecast: 74, shown: "26%", grade: "HeldOver25" },
		{ actual: 300, forecast: 299, shown: "1%", grade: "HeldWithin10" },
	])(
		`@boundary shows $shown for a forecast of $forecast against $actual, never a number its shade contradicts`,
		async ({ actual, forecast, grade }) => {
			const dialog = await theDialogFor({
				checks: [
					{
						window: 30,
						horizon: 28,
						at95: forecast,
						at85: forecast,
						at70: forecast,
						at50: forecast,
						actual,
					},
				],
			});
			const cell = levelCell(
				windowRow(periodGroup(theTableIn(dialog), 28), 30),
				70,
			);

			expect(cell).toHaveStyle({ backgroundColor: gradeFill(grade) });
		},
	);

	it(`@error a check that could not run keeps its words and takes no grade colour`, async () => {
		const dialog = await theDialogFor({
			unevaluable: HORIZON_DAYS.map((horizon) => ({
				window: 14,
				horizon,
				reason: "TooFewActiveDays" as const,
				daysWithCompletedWork: 3,
			})),
		});
		const [cell] = within(
			windowRow(periodGroup(theTableIn(dialog), 14), 14),
		).getAllByRole("cell");

		expect(cell).toHaveTextContent(/not enough history in this window/i);
		for (const grade of [
			"HeldWithin10",
			"Held10To25",
			"HeldOver25",
			"NotHeldWithin10",
			"NotHeld10To25",
			"NotHeldOver25",
		] as const) {
			expect(cell).not.toHaveStyle({ backgroundColor: gradeFill(grade) });
		}
	});
});
