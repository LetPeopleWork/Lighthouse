import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import type { OverviewTable } from "../../models/overview/OverviewPage";

const PRODUCT_LAUNCH_SCENARIO_ID = 2;
const testWithTeams = testWithDemoData(PRODUCT_LAUNCH_SCENARIO_ID);

const LONG_NAME =
	"Platform Engineering Delivery Squad for the Northern Region with cross-functional ownership of billing";
const DESKTOP = { width: 1280, height: 900 };
const PHONE = { width: 375, height: 800 };
const MID_WIDTH = { width: 700, height: 900 };
const NAME_MINIMUM = 160;
const MENU_COLUMN_MAXIMUM = 56;
const TEXT_MINIMUM = 120;

const thenDeleteIsInView = async (table: OverviewTable, when: string) => {
	await expect
		.poll(() => table.deleteIsOutOfView(), {
			message: `${when}, Delete must be inside the table so the person finds it`,
		})
		.toBe(false);
};

const thenActionsAreReachable = async (table: OverviewTable, when: string) => {
	await expect
		.poll(() => table.actionsMenuIsOutOfView(), {
			message: `${when}, the actions menu button must be inside the table so the person finds it`,
		})
		.toBe(false);
	expect(
		await table.openActionsMenu(),
		`${when}, the menu must offer Delete along with the other actions`,
	).toEqual(["Details", "Edit", "Clone", "Delete"]);
	await table.closeActionsMenu();
	await expect
		.poll(() => table.actionsMenuButtonHasFocus(), {
			message: `${when}, after Escape the keyboard focus must be on the menu button`,
		})
		.toBe(true);
	await expect
		.poll(() => table.actionsMenuIsOutOfView(), {
			message: `${when}, after Escape the menu button must still be inside the table`,
		})
		.toBe(false);
};

testWithTeams(
	"a coach finds Delete on every Team of the Overview, at desktop and at phone width, and reads a long name in full",
	async ({ testData, overviewPage }) => {
		const original = testData.teams[0].name;
		await overviewPage.viewAt(DESKTOP.width, DESKTOP.height);
		const overview = await overviewPage.renameTeam(original, LONG_NAME);
		const names = [LONG_NAME, ...testData.teams.slice(1).map((t) => t.name)];
		const tables: OverviewTable[] = [];
		for (const name of names) {
			tables.push(await overview.tableOf(name).waitUntilReady());
		}
		const teams = tables[0];

		await expect
			.poll(() => teams.horizontalScrollPosition(), {
				message: "the table opens scrolled to its left edge",
			})
			.toBe(0);
		for (const table of tables) {
			await thenDeleteIsInView(table, "at 1280 px");
		}
		await expect
			.poll(() => teams.isCutWithEllipsis("name"), {
				message:
					"a name longer than its column must end with an ellipsis instead of wrapping the row",
			})
			.toBe(true);
		for (const field of ["remainingFeatures", "lastUpdated"]) {
			await expect
				.poll(() => teams.isShownWhole(field), {
					message: `the ${field} cell must show whole; numbers and dates are never cut`,
				})
				.toBe(true);
		}
		await teams.hoverName();
		await expect(
			teams.tooltipShowing(LONG_NAME),
			"hovering the cut name must show the full name in a tooltip",
		).toBeVisible();
		await teams.focusName();
		await expect(
			teams.tooltipShowing(LONG_NAME),
			"reaching the cut name with the keyboard must show the full name in a tooltip",
		).toBeVisible();
		await expect
			.poll(() => teams.nameColumnWidth(), {
				message: `the fitted Name column is no narrower than its ${NAME_MINIMUM} px minimum`,
			})
			.toBeGreaterThanOrEqual(NAME_MINIMUM - 1);
		await expect
			.poll(() => teams.tagsColumnWidth(), {
				message: `the fitted Tags column is no narrower than its ${TEXT_MINIMUM} px minimum`,
			})
			.toBeGreaterThanOrEqual(TEXT_MINIMUM - 1);
		await expect
			.poll(() => teams.lastColumnEndsInsideTheBox(), {
				message:
					"at 1280 px the last column must end inside the table's box, so a missing shadow means nothing is hidden",
			})
			.toBe(true);
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
		await expect
			.poll(() => teams.tagsColumnWidth(), {
				message: `at phone width Tags shrinks to no less than its ${TEXT_MINIMUM} px minimum`,
			})
			.toBeGreaterThanOrEqual(TEXT_MINIMUM - 1);
		await expect
			.poll(() => teams.actionsColumnWidth(), {
				message: `at phone width the pinned actions column is only the menu button, no wider than ${MENU_COLUMN_MAXIMUM} px, so the Name has room`,
			})
			.toBeLessThanOrEqual(MENU_COLUMN_MAXIMUM);
		for (const table of tables) {
			await expect
				.poll(() => table.iconActionsShown(), {
					message:
						"at phone width the four action icons are gone from the row; they sit in the menu",
				})
				.toBe(0);
		}
		await expect
			.poll(() => teams.focusedField(), {
				message:
					"the Name cell focused at desktop width must keep the focus when the window narrows",
			})
			.toBe("name");
		await expect
			.poll(() => teams.focusedCellIsClearOfActions(), {
				message:
					"a Name focused before the window narrowed must still lie clear of the pinned actions column at 375 px, not under it",
			})
			.toBe(true);
		await expect.poll(() => teams.horizontalScrollPosition()).toBe(0);
		for (const table of tables) {
			await thenActionsAreReachable(table, "at 375 px, scrolled to the left");
		}
		// Not a check: puts the table back at its start so the shadow checks below read a known position.
		await teams.scrollToTheStart();
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

		const visited = await teams.walkToActionsWithTheKeyboard();
		expect(
			visited[visited.length - 1],
			"the arrow keys must reach the actions cell of the Team, whatever is scrolled out of view",
		).toBe("actions");
		await thenActionsAreReachable(teams, "after the keyboard walk");

		await teams.scrollToTheEnd();
		await expect
			.poll(() => teams.sidesWithACue(), {
				message:
					"at the end of the range the shadow must leave the right edge and show on the left",
			})
			.toEqual(["left"]);
		for (const table of tables) {
			await thenActionsAreReachable(table, "at 375 px, scrolled to the end");
		}

		await overview.viewAt(DESKTOP.width, DESKTOP.height);
		for (const table of tables) {
			await expect
				.poll(() => table.actionsMenuButtonsShown(), {
					message: "back at 1280 px the menu button gives way to the icons",
				})
				.toBe(0);
			await expect
				.poll(() => table.iconActionsShown(), {
					message:
						"back at 1280 px the Details, Edit, Clone and Delete icons are back",
				})
				.toBe(4);
			await thenDeleteIsInView(table, "back at 1280 px");
		}
	},
);

testWithTeams(
	"a coach finds Delete on the Portfolios table of the Overview at phone width, and the table says it scrolls",
	async ({ testData, overviewPage }) => {
		const portfolio = testData.portfolios[0].name;
		await overviewPage.viewAt(PHONE.width, PHONE.height);
		const overview = overviewPage;
		const portfolios = await overview.tableOf(portfolio).waitUntilReady();

		await thenActionsAreReachable(portfolios, "at 375 px");
		await expect
			.poll(() => portfolios.tableScrollsSideways(), {
				message:
					"at 375 px the Portfolios table must scroll sideways inside its box",
			})
			.toBe(true);
		await expect
			.poll(() => portfolios.sidesWithACue(), {
				message:
					"at 375 px the Portfolios table must show the shadow on its right edge",
			})
			.toEqual(["right"]);
		await expect
			.poll(() => overview.pageScrollsSideways(), {
				message:
					"at 375 px the page must not scroll sideways; the table scrolls instead",
			})
			.toBe(false);
	},
);

testWithTeams(
	"a coach finds Delete on a Team with a long name in a mid-width window, where the icons still show and the columns overflow",
	async ({ testData, overviewPage }) => {
		const original = testData.teams[0].name;
		await overviewPage.viewAt(DESKTOP.width, DESKTOP.height);
		const overview = await overviewPage.renameTeam(original, LONG_NAME);
		await overview.viewAt(MID_WIDTH.width, MID_WIDTH.height);
		const teams = await overview.tableOf(LONG_NAME).waitUntilReady();

		await expect
			.poll(() => teams.iconActionsShown(), {
				message:
					"at 700 px the four action icons still show in the row; the menu is for phone width only",
			})
			.toBe(4);
		await expect
			.poll(() => teams.tableScrollsSideways(), {
				message:
					"at 700 px the columns do not fit, so the table scrolls sideways",
			})
			.toBe(true);
		await thenDeleteIsInView(teams, "at 700 px, scrolled to the start");
		await expect
			.poll(() => teams.sidesWithACue(), {
				message: "at 700 px the shadow must show on the right edge only",
			})
			.toEqual(["right"]);
		await teams.scrollToTheEnd();
		await thenDeleteIsInView(teams, "at 700 px, scrolled to the end");
		await expect
			.poll(() => teams.actionsColumnEndsAtTheRightEdge(), {
				message:
					"at 700 px the actions column must be the last column and sit at the table's right edge",
			})
			.toBe(true);
		await expect
			.poll(() => teams.sidesWithACue(), {
				message:
					"at the end of the range the shadow must show on the left only",
			})
			.toEqual(["left"]);
	},
);
