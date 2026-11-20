import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

// Premium scenario: it seeds Team Lightspeed with its history, and a premium instance has no cap on
// reports. CI uploads a licence to the instance before Playwright runs.
const TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID = 11;
const testWithDemo = testWithDemoData(TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID);

const LIGHTSPEED = "Team Lightspeed";
const LONG_NAME =
	"Lightspeed this quarter set against the quarter before it, with cycle time, throughput, work in progress and the age of every item the team still has open";
const SHORT_NAME = "Since August";
const DESKTOP = { width: 1280, height: 900 };
const PHONE = { width: 375, height: 800 };
const STORED_NAME_WIDTH = 900;
const NAME_MINIMUM = 160;

testWithDemo(
	"a coach finds Delete on every report of a Team, at desktop and at phone width, and reads a long name in full",
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);
		const reportNames = [LONG_NAME, SHORT_NAME];

		await page.setViewportSize(DESKTOP);
		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		let reports = await lightspeed.goToReports();
		reports = await (
			await reports.createReport({ name: LONG_NAME })
		).backToReports();
		reports = await (
			await reports.createReport({ name: SHORT_NAME })
		).backToReports();

		expect(
			await reports.horizontalScrollPosition(),
			"the table opens scrolled to its left edge",
		).toBe(0);
		expect(
			await reports.reportsWithDeleteOutOfView(reportNames),
			"at 1280 px Delete must be inside the table for every report without scrolling, so the person finds it",
		).toEqual([]);
		expect(
			await reports.isCutWithEllipsis(reports.nameCellOf(LONG_NAME)),
			"a name longer than its column must end with an ellipsis instead of wrapping the row",
		).toBe(true);
		for (const name of reportNames) {
			expect(
				await reports.isShownWhole(reports.createdCellOf(name)),
				`the Created date of "${name}" must show whole; dates are never cut`,
			).toBe(true);
		}
		await reports.hoverNameOf(LONG_NAME);
		await expect(
			reports.tooltipShowing(LONG_NAME),
			"hovering the cut name must show the full name in a tooltip",
		).toBeVisible();
		const fittedNameWidth = await reports.nameColumnWidth();
		await expect
			.poll(() => reports.overflowState(), {
				message: "at 1280 px everything fits, so no cue may be marked",
			})
			.toBe("none");
		expect(
			await reports.sidesWithACue(),
			"a table that fits must show no shadow on either edge",
		).toEqual([]);

		reports = await reports.reopenWithStoredNameWidth(STORED_NAME_WIDTH);
		expect(
			await reports.nameColumnWidth(),
			"a Name width the person set must be used as set after a reload",
		).toBeCloseTo(STORED_NAME_WIDTH, 0);
		expect(
			await reports.reportsWithDeleteOutOfView(reportNames),
			"with a wide stored Name the columns overflow; the pinned actions must keep Delete inside the table",
		).toEqual([]);
		await expect
			.poll(() => reports.overflowState(), {
				message:
					"scrolled to the left, columns hidden on the right must be marked",
			})
			.toBe("right");
		expect(
			await reports.sidesWithACue(),
			"the shadow must sit on the right edge while the table is at its left edge",
		).toEqual(["right"]);

		await reports.resetLayout();
		await expect
			.poll(() => reports.nameColumnWidth(), {
				message: "Reset must return Name to the fitted width",
			})
			.toBeCloseTo(fittedNameWidth, 0);

		await reports.viewAt(PHONE.width, PHONE.height);
		await expect
			.poll(() => reports.nameColumnWidth(), {
				message: `at phone width Name shrinks to no less than its ${NAME_MINIMUM} px minimum`,
			})
			.toBeGreaterThanOrEqual(NAME_MINIMUM - 1);
		expect(await reports.horizontalScrollPosition()).toBe(0);
		expect(
			await reports.reportsWithDeleteOutOfView(reportNames),
			"at 375 px, scrolled to the left, the pinned actions must keep Delete inside the table",
		).toEqual([]);
		expect(
			await reports.tableScrollsSideways(),
			"at 375 px the table must scroll sideways inside its box",
		).toBe(true);
		expect(
			await reports.pageScrollsSideways(),
			"at 375 px the page must not scroll sideways; the table scrolls instead",
		).toBe(false);
		await expect
			.poll(() => reports.overflowState(), {
				message:
					"at 375 px, scrolled to the left, columns hidden on the right must be marked",
			})
			.toBe("right");
		expect(await reports.sidesWithACue()).toEqual(["right"]);
		await reports.scrollTableToTheEnd();
		await expect
			.poll(() => reports.overflowState(), {
				message:
					"at the end of the range nothing is hidden on the right, so only the left may be marked",
			})
			.toBe("left");
		expect(
			await reports.sidesWithACue(),
			"at the end of the range the shadow must leave the right edge and show on the left",
		).toEqual(["left"]);
		expect(
			await reports.reportsWithDeleteOutOfView(reportNames),
			"at 375 px, scrolled to the end, Delete must still be inside the table",
		).toEqual([]);
	},
);
