import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const PRODUCT_LAUNCH_SCENARIO_ID = 2;
const testWithTeams = testWithDemoData(PRODUCT_LAUNCH_SCENARIO_ID);

const LONG_NAME =
	"Platform Engineering Delivery Squad for the Northern Region with cross-functional ownership of billing";
const DESKTOP = { width: 1280, height: 900 };
const PHONE = { width: 375, height: 800 };
const NAME_MINIMUM = 160;

testWithTeams(
	"a coach finds Delete on every Team of the Overview, at desktop and at phone width, and reads a long name in full",
	async ({ testData, overviewPage }) => {
		const original = testData.teams[0].name;
		await overviewPage.viewAt(DESKTOP.width, DESKTOP.height);
		const overview = await overviewPage.renameTeam(original, LONG_NAME);
		await overview.search(LONG_NAME);
		const teams = await overview.tableOf(LONG_NAME).waitUntilReady();

		expect(
			await teams.horizontalScrollPosition(),
			"the table opens scrolled to its left edge",
		).toBe(0);
		expect(
			await teams.deleteIsOutOfView(),
			"at 1280 px Delete must be inside the table without scrolling, so the person finds it",
		).toBe(false);
		expect(
			await teams.isCutWithEllipsis("name"),
			"a name longer than its column must end with an ellipsis instead of wrapping the row",
		).toBe(true);
		for (const field of ["remainingFeatures", "lastUpdated"]) {
			expect(
				await teams.isShownWhole(field),
				`the ${field} cell must show whole; numbers and dates are never cut`,
			).toBe(true);
		}
		await teams.hoverName();
		await expect(
			teams.tooltipShowing(LONG_NAME),
			"hovering the cut name must show the full name in a tooltip",
		).toBeVisible();
		const fittedNameWidth = await teams.nameColumnWidth();
		expect(fittedNameWidth).toBeGreaterThanOrEqual(NAME_MINIMUM - 1);
		await expect
			.poll(() => teams.sidesWithACue(), {
				message: "a table that fits must show no shadow on either edge",
			})
			.toEqual([]);

		await overview.viewAt(PHONE.width, PHONE.height);
		await expect
			.poll(() => teams.nameColumnWidth(), {
				message: `at phone width Name shrinks to no less than its ${NAME_MINIMUM} px minimum`,
			})
			.toBeGreaterThanOrEqual(NAME_MINIMUM - 1);
		expect(await teams.horizontalScrollPosition()).toBe(0);
		expect(
			await teams.deleteIsOutOfView(),
			"at 375 px, scrolled to the left, the pinned actions must keep Delete inside the table",
		).toBe(false);
		await expect
			.poll(() => teams.tableScrollsSideways(), {
				message: "at 375 px the table must scroll sideways inside its box",
			})
			.toBe(true);
		await expect
			.poll(() => overview.pageScrollsSideways(), {
				message:
					"at 375 px the page must not scroll sideways; the table scrolls instead",
			})
			.toBe(false);
		await expect
			.poll(() => teams.sidesWithACue(), {
				message: "at 375 px the shadow must show on the right edge",
			})
			.toEqual(["right"]);
		await expect
			.poll(() => teams.rightCueDistanceFromActions(), {
				message:
					"the right shadow must end where the pinned actions column starts, so it lies over the columns that scroll and not over Delete",
			})
			.toBeLessThanOrEqual(1);

		await teams.scrollToTheEnd();
		await expect
			.poll(() => teams.sidesWithACue(), {
				message:
					"at the end of the range the shadow must leave the right edge and show on the left",
			})
			.toEqual(["left"]);
		expect(
			await teams.deleteIsOutOfView(),
			"at 375 px, scrolled to the end, Delete must still be inside the table",
		).toBe(false);
	},
);
