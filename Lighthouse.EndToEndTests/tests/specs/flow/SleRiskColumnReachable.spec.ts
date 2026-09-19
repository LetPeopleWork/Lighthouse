import type { Locator } from "@playwright/test";
import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import {
	MetricsCategories,
	MetricsWidgetNames,
} from "../../models/metrics/MetricsPage";
import { WorkItemAgingChart } from "../../models/metrics/WorkItemAgingChart";

/**
 * The criterion is about a screen, so the spec has to name one. Nothing else in this suite sets a
 * viewport, which means every other spec asserts against whatever the runner defaults to — fine for
 * a spec about content, useless for one about what fits.
 */
const NARROW_LAPTOP = { width: 1280, height: 900 };

/** "When Will This Be Done?" — seeds a team with finished history and work in flight. */
const DEMO_SCENARIO_ID = 0;

const testWithDemo = testWithDemoData(DEMO_SCENARIO_ID);

/**
 * Both the header and a cell, because a header can sit inside the grid's box while the cells
 * underneath it are drawn past the edge — which is this slice's own bug seen from the other side.
 *
 * Measured on the column rather than on the scroller's scrollWidth, which is not a usable signal
 * here in either direction. It reports 23px of overflow on a dialog where every column fits (rows
 * and content both measure exactly the visible width and no column accounts for the difference),
 * and the dialog a bubble opens genuinely overflows by 84px because it carries a Time in State
 * column the widget's own dialog does not — with the risk column ending well inside the edge
 * either way. What the criterion claims is that the risk column is readable without scrolling, and
 * that is what is asserted; no criterion in this slice promises that every column fits at 1280px,
 * and the dialog's enlarge toggle is the answer for the ones that do not.
 */
const columnIsOnScreenWithoutScrolling = async (
	header: Locator,
	cell: Locator,
	scroller: Locator,
) => {
	const scrollerBox = await scroller.boundingBox();
	expect(scrollerBox, "the grid's scroller must be laid out").not.toBeNull();
	const visibleRightEdge = (scrollerBox?.x ?? 0) + (scrollerBox?.width ?? 0);

	const headerBox = await header.boundingBox();
	expect(headerBox, "the risk column header must be laid out").not.toBeNull();
	expect(
		(headerBox?.x ?? 0) + (headerBox?.width ?? 0),
		"the risk column header's right edge must be inside the grid's visible width",
	).toBeLessThanOrEqual(visibleRightEdge + 1);

	const cellBox = await cell.boundingBox();
	expect(cellBox, "a risk cell must be laid out").not.toBeNull();
	expect(
		(cellBox?.x ?? 0) + (cellBox?.width ?? 0),
		"a risk cell's right edge must be inside the grid's visible width",
	).toBeLessThanOrEqual(visibleRightEdge + 1);
	expect(
		cellBox?.x ?? 0,
		"a risk cell must start inside the grid's visible width, not off its left edge",
	).toBeGreaterThanOrEqual((scrollerBox?.x ?? 0) - 1);
};

/**
 * The risk column is attached per call site, so "the column exists" and "a coach can see it" are
 * different facts, and only the second is the one that was reported. This drives both ways into the
 * dialog, on one page, at one size.
 *
 * What it claims is scoped on purpose: a viewer with NO stored grid layout. A coach who opened this
 * dialog before the column existed has a stored column order that does not name it, and the grid
 * appends what a stored order omits — so the column lands on the far right for them however wide
 * the dialog is. A fresh browser context starts with empty storage by construction, which is what
 * makes running in one honest rather than merely convenient. The in-product answer for the other
 * case is the grid toolbar's Reset layout; there is no test answer, and this spec does not pretend
 * to be one by clearing storage as a quiet setup step.
 */
testWithDemo(
	"a coach on a 1280px screen can read the SLE Risk column from either way into the dialog",
	async ({ page, testData, overviewPage }) => {
		await page.setViewportSize(NARROW_LAPTOP);

		const teamDetailPage = await overviewPage.goToTeam(testData.teams[0].name);
		const metricsPage = await teamDetailPage.goToMetrics();

		const flowMetricsWidgets = await metricsPage.switchCategory(
			MetricsCategories.FlowMetrics,
		);
		const agingWidget = await metricsPage.getWidgetByName(
			MetricsWidgetNames.WorkItemAgingChart,
			flowMetricsWidgets,
		);
		await expect(agingWidget.Widget).toBeVisible();

		const agingChart = new WorkItemAgingChart(page, "aging");
		const dialogGrid = page.getByRole("dialog");
		const scroller = dialogGrid.locator(".MuiDataGrid-virtualScroller");

		await testWithDemo.step(
			"the risk column is on screen when the dialog opens",
			async () => {
				const dialog = await agingWidget.openDialog();

				// Before any assertion about position. A count of zero reads as "not rendered yet",
				// and a one-sided position assertion is vacuously true on the loading frame.
				await expect
					.poll(() => agingChart.countSleRiskCells())
					.toBeGreaterThan(0);

				await columnIsOnScreenWithoutScrolling(
					agingChart.sleRiskColumnHeader,
					agingChart.sleRiskCells.first(),
					scroller,
				);

				// Closed through the named control on the way past, which is the locator the new
				// enlarge toggle would otherwise have silently retargeted.
				await dialog.close();
				await expect(dialog.dialog).toBeHidden();
			},
		);

		await testWithDemo.step(
			"the risk column is on screen when a bubble opens the dialog",
			async () => {
				await agingWidget.openDialogFromBubble();

				await expect
					.poll(() => agingChart.countSleRiskCells())
					.toBeGreaterThan(0);

				await columnIsOnScreenWithoutScrolling(
					agingChart.sleRiskColumnHeader,
					agingChart.sleRiskCells.first(),
					scroller,
				);
			},
		);
	},
);
