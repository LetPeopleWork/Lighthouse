/// <reference lib="dom" />
import { expect, type Locator, type Page } from "@playwright/test";

export type LogLevels =
	| "Verbose"
	| "Debug"
	| "Information"
	| "Warning"
	| "Error"
	| "Fatal";

type BillOfMaterials = {
	metadata?: { component?: { name?: string } };
	components?: { name: string; scope?: string }[];
};

export class SystemInfoPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async setLogLevel(level: LogLevels): Promise<void> {
		const currentLogLevel = await this.getCurrentLogLevel();
		await this.page.getByText(currentLogLevel, { exact: true }).click();

		await this.page.getByTestId(level).click();
	}

	async getCurrentLogLevel(): Promise<LogLevels> {
		const selectedOption = await this.page.$eval(
			"select#logLevel",
			(select: HTMLSelectElement) => select.value,
		);
		return selectedOption as LogLevels;
	}

	async download(): Promise<void> {
		await this.page.getByRole("button", { name: "Download" }).click();
	}

	async refresh(): Promise<void> {
		await this.page.getByRole("button", { name: "Refresh" }).click();
	}

	/** The Third Party Packages card, so its pager is never confused with another list's on this page. */
	get thirdPartyPackages(): Locator {
		return this.page.locator(".MuiCard-root").filter({
			has: this.page.getByRole("heading", {
				name: "Third Party Packages",
				exact: true,
			}),
		});
	}

	get packageRows(): Locator {
		return this.thirdPartyPackages.locator(".MuiDataGrid-row");
	}

	get firstPackageName(): Locator {
		return this.packageRows.first().locator('[data-field="name"]');
	}

	/** The pager's "1–100 of 243"; the dash is an en dash. */
	get packagesRowRange(): Locator {
		return this.thirdPartyPackages.getByText(/^\d+–\d+ of \d+$/);
	}

	get packagesRowsPerPageChoice(): Locator {
		return this.thirdPartyPackages.getByText(/^Rows per page/);
	}

	get nextPackagesPageButton(): Locator {
		return this.thirdPartyPackages.getByRole("button", {
			name: "Go to next page",
			exact: true,
		});
	}

	/**
	 * How many packages the list holds by the grid's own account. The grid counts its header row too,
	 * and the count covers every page, not just the rows on screen.
	 */
	async packagesGridRowCount(): Promise<number> {
		const rowCount = await this.thirdPartyPackages
			.getByRole("grid")
			.getAttribute("aria-rowcount");
		return Number(rowCount) - 1;
	}

	/**
	 * How many packages this build serves, read from its two bills of materials rather than from the
	 * list, so a list that drops packages cannot vouch for itself. It keeps what the Third Party
	 * Packages list keeps: every component except the application itself and any marked optional or
	 * excluded.
	 */
	async servedPackageCount(): Promise<number> {
		const counts = await Promise.all(
			["/sbom/backend.cdx.json", "/sbom/frontend.cdx.json"].map(
				async (path) => {
					const response = await this.page.request.get(
						new URL(path, this.page.url()).toString(),
					);
					expect(response.ok(), `${path} could not be loaded`).toBe(true);
					const bill = (await response.json()) as BillOfMaterials;
					const application = bill.metadata?.component?.name;
					return (bill.components ?? []).filter(
						(component) =>
							component.name !== application &&
							component.scope !== "optional" &&
							component.scope !== "excluded",
					).length;
				},
			),
		);
		return counts.reduce((total, count) => total + count, 0);
	}

	/** The total the range names, the count left after any search or filter. */
	async packagesRangeTotal(): Promise<number> {
		const range = (await this.packagesRowRange.textContent()) ?? "";
		const total = /^\d+–\d+ of (\d+)$/.exec(range)?.[1];
		if (total === undefined) {
			throw new Error(`The range "${range}" names no total.`);
		}
		return Number(total);
	}

	/** Waits for the packages themselves: both bills of materials load after the tab has opened. */
	async openThirdPartyPackages(): Promise<void> {
		await this.thirdPartyPackages.scrollIntoViewIfNeeded();
		await this.packageRows.first().waitFor();
	}

	/** What the list shows right now: its range (or nothing, without a footer) and its first package. */
	private async packagesOnScreen(): Promise<[string | null, string | null]> {
		const range =
			(await this.packagesRowRange.count()) === 0
				? null
				: await this.packagesRowRange.textContent();
		return [range, await this.firstPackageName.textContent()];
	}

	/** Returns once the new page is on screen, so a caller never reads the previous page's range. */
	async goToNextPackagesPage(): Promise<void> {
		const rangeBefore = await this.packagesRowRange.textContent();
		await this.thirdPartyPackages
			.getByRole("button", { name: "Go to next page", exact: true })
			.click();
		await expect(this.packagesRowRange).not.toHaveText(rangeBefore ?? "");
	}

	/** Returns once the list has answered the search: its range or its first package has changed. */
	async searchPackages(text: string): Promise<void> {
		const before = await this.packagesOnScreen();
		await this.thirdPartyPackages
			.getByPlaceholder("Search packages...", { exact: true })
			.fill(text);
		await expect(async () => {
			expect(await this.packagesOnScreen()).not.toEqual(before);
		}).toPass();
	}
}
