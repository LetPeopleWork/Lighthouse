import { expect, type Locator, type Page } from "@playwright/test";

export class ParentSourcesList {
	constructor(private readonly page: Page) {}

	get heading(): Locator {
		return this.page.getByRole("heading", {
			name: "Parent sources",
			exact: true,
		});
	}

	get section(): Locator {
		return this.heading.locator("xpath=../..");
	}

	get rows(): Locator {
		return this.section.getByRole("list").getByRole("listitem");
	}

	async override(): Promise<void> {
		await this.section.getByRole("radio", { name: "Override" }).check();
	}

	async addSource(name: string): Promise<void> {
		await this.section.getByRole("combobox", { name: "Add a source" }).click();
		await this.page.getByRole("option", { name, exact: true }).click();
	}

	async moveDown(name: string): Promise<void> {
		await this.section
			.getByRole("button", { name: `Move ${name} down` })
			.click();
	}

	async expectOrder(names: string[]): Promise<void> {
		await expect(this.rows).toHaveCount(names.length);
		for (const [position, name] of names.entries()) {
			await expect(this.rows.nth(position)).toContainText(name);
		}
	}
}
