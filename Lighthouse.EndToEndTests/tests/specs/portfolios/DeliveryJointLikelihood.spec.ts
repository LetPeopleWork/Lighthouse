import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";

/**
 * Delivery joint likelihood — Playwright spec
 *
 * Feature: delivery-joint-likelihood (ADO User Story #5587, Epic #5459)
 * Wave: DISTILL (skeleton — the scenario starts as test.skip())
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
 *           breakdown column states it ignores the others (AC-03.2), and both render in the real app
 *           with real terminology rather than a mocked getTerm.
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

test.skip("@walking_skeleton @driving_adapter @real-io @US-03 forecaster reads which probability each delivery surface is showing", async ({
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

	await test.step("the header explains what ALL means", async () => {
		await expect(
			page.getByTitle("P(ALL of these land by the date)"),
		).toBeAttached();
	});

	await test.step("the breakdown column says it ignores the other features", async () => {
		await delivery.toggleDetails();

		await expect(delivery.likelihoodColumnHeader).toContainText(
			"each on its own",
		);
		await expect(
			page.getByTitle("P(this one lands), ignoring the others"),
		).toBeAttached();
	});
});
