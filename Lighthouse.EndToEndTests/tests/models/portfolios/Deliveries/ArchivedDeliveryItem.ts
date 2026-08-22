import type { Locator } from "@playwright/test";

/**
 * A Delivery in the Archived section. It shows what was written down on the day it closed, so
 * everything here is read from the pinned record and nothing it offers fetches anything.
 */
export class ArchivedDeliveryItem {
	readonly container: Locator;

	constructor(container: Locator) {
		this.container = container;
	}

	get archivedMarker(): Locator {
		return this.container.getByTestId("archived-marker");
	}

	async getArchivedOn(): Promise<string> {
		return (await this.archivedMarker.textContent()) ?? "";
	}

	async toggleDetails(): Promise<void> {
		await this.container.click();
	}

	async unarchive(): Promise<void> {
		await this.UnarchiveButton.click();
	}

	get UnarchiveButton(): Locator {
		return this.container.getByRole("button", {
			name: "unarchive",
			exact: true,
		});
	}

	get DeleteButton(): Locator {
		return this.container.getByRole("button", { name: "delete", exact: true });
	}
}
