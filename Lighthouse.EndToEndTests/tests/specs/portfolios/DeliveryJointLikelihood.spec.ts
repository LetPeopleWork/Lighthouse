import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";

/**
 * Delivery joint likelihood — Playwright spec
 *
 * Feature: delivery-joint-likelihood (ADO User Story #5587, Epic #5459)
 *
 * ONE thin walking skeleton for the whole feature's UI surface, per the project's standing E2E rule.
 * Slice-01 deliberately has no E2E: the shape that most needed proving there (a contributing pair
 * with no forecast row) needs a seeded-then-resynced sequence that does not belong in a browser test,
 * and its discrimination lives in DeliveryJointForecastIntegrationTest against the same HTTP endpoint.
 * Slice-03 owns this one because the relabel is the only part of the feature a user reads off the
 * screen and nothing below the browser can prove the string reached it.
 *
 * What this asserts, and what it deliberately does NOT:
 *   DOES  — the delivery header states it covers ALL features and carries the date (AC-03.1), the
 *           breakdown column is named plainly (AC-03.2), and both render in the real app with real
 *           terminology rather than a mocked getTerm.
 *   NOT   — the joint MATHS. Demo throughput moves, so pinning a percentage here would be a flake
 *           generator; the number is pinned in DeliveryJointForecastIntegrationTest, which asserts
 *           81 % against the same endpoint where the governing-feature answer is 90 %.
 *   NOT   — a renamed-vocabulary permutation, a long-terminology permutation, or a team/portfolio
 *           twin. Those are DeliverySection.likelihoodCopy.test.tsx's, at jsdom speed.
 *
 * Truncation (slice-03's learning hypothesis / deferred question 8) is the one thing only a browser
 * can answer, and it is NOT asserted here: it needs a terminology override applied to the instance,
 * which this spec has no seam for. It stays a manual check before the copy is final — recorded in
 * distill-red-classification.md rather than silently dropped.
 */

const DEMO_SCENARIO_ID = 0;
const DEMO_PORTFOLIO_NAME = "Project Apollo";
const DEMO_DELIVERY_NAME = "Apollo Release";

test("@walking_skeleton @driving_adapter @real-io @US-03 forecaster reads which probability each delivery surface is showing", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const portfolioDetail = await overviewPage.goToPortfolio(DEMO_PORTFOLIO_NAME);
	const deliveries = await portfolioDetail.goToDeliveries();
	const delivery = deliveries.getDeliveryByName(DEMO_DELIVERY_NAME);

	await test.step("the header says it covers all features, by the delivery date", async () => {
		// "All <term> by <date>: NN%" — the term is whatever this instance calls a Feature, so the
		// regex leaves it open rather than hardcoding "Features" and re-introducing the literal the
		// unit tests exist to forbid (AC-03.3).
		await expect(delivery.forecastChip).toContainText(/^All .+ by .+: \d+%$/);

		const deliveryDate = await delivery.getDeliveryDate();
		expect(deliveryDate).not.toBeNull();
		// AC-03.8 — one formatter. The date inside the chip is the date beside it.
		await expect(delivery.forecastChip).toContainText(deliveryDate as string);

		// AC-03.4 / D1 constraint B — the header may EQUAL a row, so no copy may promise otherwise.
		await expect(delivery.forecastChip).not.toContainText(/lower than/i);
	});

	await test.step("the breakdown column is named plainly", async () => {
		await delivery.toggleDetails();

		await expect(delivery.likelihoodColumnHeader).toContainText("Likelihood");
		await expect(delivery.likelihoodColumnHeader).not.toContainText(
			"each on its own",
		);
	});
});

const DEPENDENCIES_SCENARIO_ID = 12;
const DEPENDENCIES_PORTFOLIO_NAME = "Project Ocean Explorer";
const MULTI_TEAM_DELIVERY_NAME = "Ocean Explorer Milestone";
const TEAM_WITHOUT_THROUGHPUT = "Team Meridian";

/**
 * Slice-01's maths, on real data — the one thing no unit or integration fixture can show: that the
 * rollup reaches a real delivery built from a real Monte Carlo run over several teams.
 *
 * The Dependencies scenario is the only demo portfolio whose features span teams, and one of those
 * teams ("Team Meridian") has closed nothing, so it has no throughput to forecast from. That makes
 * every feature it touches un-forecastable, and one un-forecastable feature makes the whole delivery
 * un-forecastable (ADR-112 D8). Deleting the team removes its work pairs — FeatureWork.Team cascades
 * — so the remaining teams' rows are all that is left and the delivery becomes forecastable.
 *
 * The assertion that matters is the LAST one: the delivery number is at or below every feature's own
 * number. That is the invariant the whole feature exists to establish, and it is the one thing a
 * governing-feature rollup could not satisfy for a multi-team delivery. Equality IS permitted (D5),
 * so this asserts `<=`, never `<`.
 *
 * Deliberately NOT asserted: an exact percentage. Demo throughput moves with the calendar, so a pinned
 * number is a flake generator; the exact joint is pinned in DeliveryJointForecastIntegrationTest.
 */
test("@premium @real-io @US-01 a delivery is unforecastable while one team has no throughput, then reports the joint across the rest", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEPENDENCIES_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const portfolioDetail = await overviewPage.goToPortfolio(
		DEPENDENCIES_PORTFOLIO_NAME,
	);
	let deliveries = await portfolioDetail.goToDeliveries();
	let delivery = deliveries.getDeliveryByName(MULTI_TEAM_DELIVERY_NAME);

	await test.step("cannot be forecast while a contributing team has no throughput", async () => {
		await expect(delivery.forecastChip).toContainText("Cannot forecast");
	});

	await test.step("the team with no throughput is removed", async () => {
		await page.goto("/");
		const deletionDialog = await overviewPage.deleteTeam(
			TEAM_WITHOUT_THROUGHPUT,
		);
		await deletionDialog.delete();
	});

	await test.step("the delivery can now be forecast and publishes dates", async () => {
		await page.goto("/");
		const refreshedPortfolio = await overviewPage.goToPortfolio(
			DEPENDENCIES_PORTFOLIO_NAME,
		);
		deliveries = await refreshedPortfolio.goToDeliveries();
		delivery = deliveries.getDeliveryByName(MULTI_TEAM_DELIVERY_NAME);

		await expect(delivery.forecastChip).not.toContainText("Cannot forecast");
	});

	await test.step("and that forecast is at or below every feature's own", async () => {
		// Asserted against the same endpoint the page just rendered rather than off the header: another
		// team in this scenario has thin history, and DeliverySection renders the insufficient-data
		// label INSTEAD of a percentage (strict either/or), which no amount of team deletion changes.
		// This is the invariant the whole feature exists to establish, on real Monte Carlo output over
		// four teams and thirteen features - the one thing hand-built fixtures cannot be. Equality is
		// PERMITTED (D5), so this is <= and never <.
		const portfolios = (await (
			await request.get("/api/latest/portfolios")
		).json()) as { id: number; name: string }[];
		const portfolioId = portfolios.find(
			(candidate) => candidate.name === DEPENDENCIES_PORTFOLIO_NAME,
		)?.id;

		const response = await request.get(
			`/api/latest/deliveries/portfolio/${portfolioId}`,
		);
		expect(response.ok()).toBe(true);

		const payloads = (await response.json()) as DeliveryPayload[];
		const payload = payloads.find(
			(candidate) => candidate.name === MULTI_TEAM_DELIVERY_NAME,
		) as DeliveryPayload;

		const rowLikelihoods = payload.featureLikelihoods
			.map((row) => row.likelihoodPercentage)
			.filter((likelihood): likelihood is number => likelihood !== null);

		expect(payload.likelihoodPercentage).not.toBeNull();
		expect(rowLikelihoods.length).toBeGreaterThan(1);
		expect(payload.likelihoodPercentage as number).toBeLessThanOrEqual(
			Math.min(...rowLikelihoods),
		);
	});
});

interface DeliveryPayload {
	name: string;
	likelihoodPercentage: number | null;
	featureLikelihoods: { likelihoodPercentage: number | null }[];
}
