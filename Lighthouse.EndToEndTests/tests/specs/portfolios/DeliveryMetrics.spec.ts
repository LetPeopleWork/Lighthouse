import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";
import { DeliveryMetricsTab } from "../../models/portfolios/Deliveries/DeliveryMetricsTab";

const DEMO_SCENARIO_ID = 0;
const DEMO_PORTFOLIO_NAME = "Project Apollo";
const DEMO_DELIVERY_NAME = "Apollo Release";

test("@walking_skeleton @US-01 forecaster opens a delivery's Metrics tab and sees the backlog and done burnup", async ({
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
	await delivery.toggleDetails();

	const metricsTab = new DeliveryMetricsTab(delivery);
	await metricsTab.openMetricsTab();

	await expect(metricsTab.burnupChart).toBeVisible();
	await expect.poll(() => metricsTab.countSeriesLines()).toBeGreaterThan(0);

	await metricsTab.openWorkItemsTab();
	await expect(page.getByText("Feature Name")).toBeVisible();
});
