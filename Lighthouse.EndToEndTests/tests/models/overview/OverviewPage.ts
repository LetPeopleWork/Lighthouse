import { expect, type Locator, type Page } from "@playwright/test";
import type { LighthousePage } from "../app/LighthousePage";
import { DataGridView, pageScrollsSideways } from "../common/DataGridView";
import { AddPortfolioWizard } from "../portfolios/AddPortfolioWizard";
import { PortfolioDeletionDialog } from "../portfolios/PortfolioDeletionDialog";
import { PortfolioDetailPage } from "../portfolios/PortfolioDetailPage";
import { PortfolioEditPage } from "../portfolios/PortfolioEditPage";
import { ReportPage } from "../reports/ReportPage";
import { AddTeamWizard } from "../teams/AddTeamWizard";
import { TeamDeletionDialog } from "../teams/TeamDeletionDialog";
import { TeamDetailPage } from "../teams/TeamDetailPage";
import { TeamEditPage } from "../teams/TeamEditPage";

/**
 * One table of the Overview as the person sees it: its box is what is in view, whatever scrolls
 * inside it. Scoped to the table that lists the given owner, because the Overview shows a table of
 * Portfolios and one of Teams.
 */
export class OverviewTable {
	private readonly view: DataGridView;

	constructor(
		private readonly page: Page,
		private readonly ownerName: string,
	) {
		this.view = new DataGridView(
			page,
			this.table,
			this.container.locator("[data-overflow]"),
			"Overview table",
		);
	}

	private get container(): Locator {
		return this.page.getByTestId("datagrid-container").filter({
			has: this.page.getByRole("link", { name: this.ownerName, exact: true }),
		});
	}

	get table(): Locator {
		return this.container.getByRole("grid");
	}

	/** The owner's Reports cell once it has read the reports, so the table has settled. */
	async waitUntilReady(): Promise<OverviewTable> {
		await expect(
			this.table,
			"exactly one table of the Overview must list this owner",
		).toHaveCount(1);
		await this.row
			.locator('[data-field="reports"]')
			.filter({ has: this.page.locator('[data-cell-status="ready"]') })
			.waitFor();
		return this;
	}

	private get row(): Locator {
		return this.table.getByRole("row").filter({
			has: this.page.getByRole("link", { name: this.ownerName, exact: true }),
		});
	}

	cell(field: string): Locator {
		return this.row.locator(`[role="gridcell"][data-field="${field}"]`);
	}

	private get nameHeader(): Locator {
		return this.table.locator('[role="columnheader"][data-field="name"]');
	}

	tooltipShowing(text: string): Locator {
		return this.page.getByRole("tooltip", { name: text });
	}

	async hoverName(): Promise<void> {
		await this.row
			.getByRole("link", { name: this.ownerName, exact: true })
			.first()
			.hover();
	}

	async nameColumnWidth(): Promise<number> {
		return (await this.nameHeader.boundingBox())?.width ?? 0;
	}

	async tagsColumnWidth(): Promise<number> {
		const header = this.table.locator(
			'[role="columnheader"][data-field="tags"]',
		);
		await expect(
			header,
			"the Overview table must have exactly one Tags column",
		).toHaveCount(1);
		return (await header.boundingBox())?.width ?? 0;
	}

	/** True when this owner's Delete is not wholly inside the table's box, rendered or not. */
	async deleteIsOutOfView(): Promise<boolean> {
		return !(await this.view.isWhollyInView(
			this.row.getByRole("button", { name: "Delete", exact: true }),
		));
	}

	private get actionsMenuButton(): Locator {
		return this.row.getByRole("button", {
			name: `Actions for ${this.ownerName}`,
			exact: true,
		});
	}

	/** True when this owner's one-button actions menu is not wholly inside the table's box, rendered or not. */
	async actionsMenuIsOutOfView(): Promise<boolean> {
		return !(await this.view.isWhollyInView(this.actionsMenuButton));
	}

	/** The width of the actions column, the pinned one at the right edge. */
	async actionsColumnWidth(): Promise<number> {
		const header = this.table.locator(
			'[role="columnheader"][data-field="actions"]',
		);
		await expect(
			header,
			"the Overview table must have exactly one actions column",
		).toHaveCount(1);
		return (await header.boundingBox())?.width ?? 0;
	}

	/** How many of the Details, Edit, Clone and Delete icons this owner's row shows. */
	async iconActionsShown(): Promise<number> {
		let shown = 0;
		for (const name of ["Details", "Edit", "Clone", "Delete"]) {
			shown += await this.row
				.getByRole(name === "Clone" || name === "Delete" ? "button" : "link", {
					name,
					exact: true,
				})
				.count();
		}
		return shown;
	}

	/** How many menu buttons this owner's row shows. */
	async actionsMenuButtonsShown(): Promise<number> {
		return this.actionsMenuButton.count();
	}

	/** Opens this owner's actions menu and returns what it offers, in order. */
	async openActionsMenu(): Promise<string[]> {
		await this.actionsMenuButton.click();
		const menu = this.page.getByRole("menu");
		await expect(menu, "exactly one actions menu must be open").toHaveCount(1);
		return (await menu.getByRole("menuitem").allTextContents()).map((item) =>
			item.trim(),
		);
	}

	/** True when this owner's menu button holds the keyboard focus. */
	async actionsMenuButtonHasFocus(): Promise<boolean> {
		return this.actionsMenuButton.evaluate(
			(button) => button === document.activeElement,
		);
	}

	async closeActionsMenu(): Promise<void> {
		await this.page.keyboard.press("Escape");
		await expect(
			this.page.getByRole("menu"),
			"Escape must close the actions menu",
		).toHaveCount(0);
	}

	/** True when the cell shows its text cut off with an ellipsis. */
	async isCutWithEllipsis(field: string): Promise<boolean> {
		return this.view.isCutWithEllipsis(this.cell(field));
	}

	/** True when nothing in the cell is wider than the room it has, so the whole content shows. */
	async isShownWhole(field: string): Promise<boolean> {
		return this.view.isShownWhole(this.cell(field));
	}

	async horizontalScrollPosition(): Promise<number> {
		return this.view.horizontalScrollPosition();
	}

	/** The sides of the table that carry a shadow. */
	async sidesWithACue(): Promise<string[]> {
		return this.view.sidesWithACue();
	}

	/** Pixels between the right shadow's end and the actions column's start; infinite while there is no right shadow. */
	async rightCueDistanceFromActions(): Promise<number> {
		return this.view.rightCueDistanceFromActions();
	}

	/** True when the table is wider than its box, so the table scrolls rather than the page. */
	async tableScrollsSideways(): Promise<boolean> {
		return this.view.tableScrollsSideways();
	}

	/** True when the last column ends inside the table's box, so a fitting table hides nothing on its right. */
	async lastColumnEndsInsideTheBox(): Promise<boolean> {
		const scroller = await this.table
			.locator(".MuiDataGrid-virtualScroller")
			.boundingBox();
		const last = await this.table
			.locator('[role="columnheader"]')
			.last()
			.boundingBox();
		return (
			scroller !== null &&
			last !== null &&
			last.x + last.width <= scroller.x + scroller.width + 1
		);
	}

	/** True when the actions column is the table's last column and ends at the right edge of the table's box. */
	async actionsColumnEndsAtTheRightEdge(): Promise<boolean> {
		const scroller = await this.table
			.locator(".MuiDataGrid-virtualScroller")
			.boundingBox();
		const last = this.table.locator('[role="columnheader"]').last();
		const lastBox = await last.boundingBox();
		return (
			(await last.getAttribute("data-field")) === "actions" &&
			scroller !== null &&
			lastBox !== null &&
			Math.abs(lastBox.x + lastBox.width - (scroller.x + scroller.width)) <= 20
		);
	}

	/** Puts the keyboard focus on the owner's name, the way a person tabbing through the table does. */
	async focusName(): Promise<void> {
		await this.row
			.getByRole("link", { name: this.ownerName, exact: true })
			.first()
			.focus();
	}

	/**
	 * Moves through the cells of the owner's row with the arrow keys, starting on its name,
	 * until the actions cell has the focus. Every cell on the way must come to rest clear of the pinned actions column.
	 */
	async walkToActionsWithTheKeyboard(): Promise<string[]> {
		await this.page.evaluate(() =>
			(document.activeElement as HTMLElement | null)?.blur(),
		);
		await this.focusName();
		const MOST_CELLS = 8;
		return this.view.walkToActionsWithTheKeyboard(MOST_CELLS);
	}

	async scrollToTheStart(): Promise<void> {
		await this.table
			.locator(".MuiDataGrid-virtualScroller")
			.evaluate((element) => {
				element.scrollLeft = 0;
				element.dispatchEvent(new Event("scroll"));
			});
	}

	async scrollToTheEnd(): Promise<void> {
		await this.view.scrollToTheEnd();
	}

	/** The data field of the cell that holds the keyboard focus, or "unknown" when none does. */
	async focusedField(): Promise<string> {
		return this.view.focusedField();
	}

	/** True when the focused cell lies inside the table and ends before the pinned actions column starts. */
	async focusedCellIsClearOfActions(): Promise<boolean> {
		return this.view.focusedCellIsClearOfActions();
	}
}

export class OverviewPage {
	readonly page: Page;
	readonly lightHousePage: LighthousePage;

	constructor(page: Page, lightHousePage: LighthousePage) {
		this.page = page;
		this.lightHousePage = lightHousePage;
	}

	get lighthousePage(): LighthousePage {
		return this.lightHousePage;
	}

	async search(searchTerm: string): Promise<void> {
		await this.page
			.getByTestId("overview-filter-bar")
			.getByRole("textbox")
			.fill(searchTerm);
	}

	async getPortfolioLink(portfolio: string): Promise<Locator> {
		const portfolioLink = this.page.getByRole("link", { name: portfolio });
		return portfolioLink;
	}

	async goToPortfolio(portfolio: string): Promise<PortfolioDetailPage> {
		await this.search(portfolio);

		const portfolioLink = await this.getPortfolioLink(portfolio);
		await portfolioLink.click();

		return new PortfolioDetailPage(this.page).waitUntilFeatureListReady();
	}

	async editPortfolio(portfolio: {
		name: string;
		id: number;
	}): Promise<PortfolioEditPage> {
		await this.search(portfolio.name);

		const portfolioEditIcon = this.page.getByLabel("Edit");
		await portfolioEditIcon.click();

		await this.page.waitForURL(/\/portfolios\/\d+\/settings/);

		return new PortfolioEditPage(this.page);
	}

	async addNewPortfolio(): Promise<AddPortfolioWizard> {
		await this.page
			.getByRole("button", { name: "Add Portfolio" })
			.first()
			.click();

		return new AddPortfolioWizard(this.page);
	}

	async deletePortfolio(portfolio: {
		name: string;
		id: number;
	}): Promise<PortfolioDeletionDialog> {
		await this.search(portfolio.name);
		const portfolioDeleteIcon = this.page.getByLabel("Delete");
		await portfolioDeleteIcon.click();

		return new PortfolioDeletionDialog(this.page);
	}

	async getTeamLink(teamName: string): Promise<Locator> {
		const teamLink = this.page.getByRole("link", { name: teamName });
		return teamLink;
	}

	async goToTeam(teamName: string): Promise<TeamDetailPage> {
		await this.search(teamName);

		const teamLink = await this.getTeamLink(teamName);
		await teamLink.click();

		return new TeamDetailPage(this.page);
	}

	/**
	 * A Team's Reports cell once it has read the reports. Waiting for mere visibility would accept the
	 * spinner the cell shows while the reports are on their way.
	 */
	teamReportsCell(teamName: string): Locator {
		return this.page
			.getByRole("row")
			.filter({
				has: this.page.getByRole("link", { name: teamName, exact: true }),
			})
			.locator('[data-field="reports"]')
			.filter({ has: this.page.locator('[data-cell-status="ready"]') });
	}

	async openTeamReport(
		teamName: string,
		reportName: string,
	): Promise<ReportPage> {
		await this.search(teamName);
		const cell = this.teamReportsCell(teamName);
		await cell.waitFor();
		await cell.getByRole("link", { name: reportName, exact: true }).click();
		await this.page.waitForURL(/\/teams\/\d+\/reports\/\d+$/);
		return new ReportPage(this.page).waitUntilReady();
	}

	async editTeam(teamName: string): Promise<TeamEditPage> {
		await this.search(teamName);

		const teamEditIcon = this.page.getByLabel("Edit");
		await teamEditIcon.click();

		await this.page.waitForURL(/\/teams\/\d+\/settings/);

		return new TeamEditPage(this.page);
	}

	async addNewTeam(): Promise<AddTeamWizard> {
		await this.page.getByRole("button", { name: "Add Team" }).first().click();

		return new AddTeamWizard(this.page);
	}

	async deleteTeam(teamName: string): Promise<TeamDeletionDialog> {
		await this.search(teamName);
		const teamDeleteIcon = this.page.getByLabel("Delete");
		await teamDeleteIcon.click();

		return new TeamDeletionDialog(this.page);
	}

	async cloneTeam(teamName: string): Promise<TeamEditPage> {
		await this.search(teamName);
		const teamCloneIcon = this.page.getByLabel("Clone");
		await teamCloneIcon.click();

		return new TeamEditPage(this.page);
	}

	async clonePortfolio(portfolioName: string): Promise<PortfolioEditPage> {
		await this.search(portfolioName);

		const portfolioCloneIcon = this.page.getByLabel("Clone");

		await portfolioCloneIcon.click();

		return new PortfolioEditPage(this.page);
	}

	/** The table of the Overview that lists the given Team or Portfolio. */
	tableOf(ownerName: string): OverviewTable {
		return new OverviewTable(this.page, ownerName);
	}

	async viewAt(width: number, height: number): Promise<void> {
		await this.page.setViewportSize({ width, height });
	}

	/** True when the whole page can be scrolled sideways, which a table that stays in its box never causes. */
	async pageScrollsSideways(): Promise<boolean> {
		return pageScrollsSideways(this.page);
	}

	/** Renames a Team through its settings, so the Overview lists it under a name of the given length. */
	async renameTeam(teamName: string, newName: string): Promise<OverviewPage> {
		const edit = await this.editTeam(teamName);
		await edit.setName(newName);
		await edit.save();
		return this.lightHousePage.goToOverview();
	}

	// The whole app bar, not the nav links inside it — the licensing screenshot clips its right third,
	// which is where the status icons live.
	get toolbar(): Locator {
		return this.page.getByRole("banner");
	}
}
