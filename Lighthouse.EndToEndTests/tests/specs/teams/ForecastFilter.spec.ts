// Forecast Filter — Playwright spec (walking-skeleton, US-01..US-06).
//
// Feature: filter-forecast-throughput (ADO Epic #4896).
// Companion Gherkin: ForecastFilter.feature (same scenario title).
//
// Walking Skeleton Strategy: B — Real local + faked WTS.
//   - Real backend, real database, real Vitest.
//   - Work-tracking system connector (Jira / ADO / Linear) supplied by the existing
//     testWithUpdatedTeams fixture; the fixture seeds teams against real ADO with a
//     mixed history of User Stories and Bugs so the filter has observable effect.
//   - Premium-license activation depends on the test environment seed; the spec
//     gates on the Forecast Filter editor being visible on the team edit page.

import { expect } from "@playwright/test";
import { testWithUpdatedTeams } from "../../fixutres/LighthouseFixture";

const test = testWithUpdatedTeams([0]);

const FORECAST_FILTER_HEADING = "Forecast Filter (Premium)";
const APPLY_FILTER_TOGGLE_LABEL = "Apply forecast-throughput filter";
const FILTERED_CHIP_LABEL = "Filtered throughput";

test.describe("Forecast filter — premium walking skeleton", () => {
	test("[@walking_skeleton @premium @driving_adapter @real-io @US-01 @US-02 @US-03 @US-04 @US-05 @US-06 @kpi-OUT-filter-adoption] Premium delivery-forecaster configures the filter and propagates it across every forecast surface", async ({
		page,
		testData,
		overviewPage,
	}) => {
		const team = testData.teams[0];

		const teamEditPage = await overviewPage.editTeam(team.name);
		await expect(
			page.getByRole("heading", { name: FORECAST_FILTER_HEADING }),
		).toBeVisible();

		const addRuleButton = page.getByRole("button", { name: /add rule/i });
		await addRuleButton.click();

		await page.getByRole("combobox", { name: /field/i }).first().click();
		await page.getByRole("option", { name: /^Type$/ }).click();

		await page
			.getByRole("combobox", { name: /operator/i })
			.first()
			.click();
		await page.getByRole("option", { name: /^equals$/i }).click();

		await page.getByLabel(/value/i).first().fill("Bug");

		await teamEditPage.save();

		await page.reload();
		await expect(page.getByText("Bug").first()).toBeVisible();

		await page.goto("/");
		const teamDetailPage = await overviewPage.goToTeam(team.name);
		await teamDetailPage.updateTeamData();
		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled({
			timeout: 90_000,
		});

		await teamDetailPage.goToMetrics();
		await expect(
			page.getByRole("group", { name: /Throughput filter view/i }),
		).toBeVisible();
		await expect(page.getByRole("button", { name: /^Raw$/ })).toHaveAttribute(
			"aria-pressed",
			"true",
		);

		await page.getByRole("button", { name: /^Filtered$/ }).click();
		await expect(
			page.getByRole("button", { name: /^Filtered$/ }),
		).toHaveAttribute("aria-pressed", "true");
		await expect(page.getByLabel(FILTERED_CHIP_LABEL).first()).toBeVisible();

		await teamDetailPage.goToForecasts();
		const teamForecastToggle = page.getByLabel(APPLY_FILTER_TOGGLE_LABEL);
		await expect(teamForecastToggle).toBeVisible();
		await expect(teamForecastToggle).toBeChecked();

		await teamForecastToggle.click();
		await page
			.getByRole("spinbutton", {
				name: /remaining items|number of work items/i,
			})
			.first()
			.fill("10");
		await page
			.getByRole("button", { name: /^Forecast$/ })
			.first()
			.click();
		await expect(page.getByLabel(FILTERED_CHIP_LABEL).first()).toHaveCount(0);

		await expect(teamDetailPage.backtestForecastingSection).toBeVisible();
		const backtestToggle = page.getByLabel(APPLY_FILTER_TOGGLE_LABEL).last();
		await expect(backtestToggle).toBeVisible();
		await expect(backtestToggle).toBeChecked();
		await teamDetailPage.runBacktest();
		await expect(teamDetailPage.backtestResultsSection).toBeVisible();
		await expect(page.getByLabel(FILTERED_CHIP_LABEL).first()).toBeVisible();
	});
});
