import type { Locator, Page } from "@playwright/test";
import { ArchivedDeliveryItem } from "./ArchivedDeliveryItem";
import { DeliveryItem } from "./DeliveryItem";
import { ModifyDeliveriesDialog } from "./ModifyDeliveriesDialog";

export class DeliveriesPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async addDelivery(): Promise<ModifyDeliveriesDialog> {
		await this.page.getByRole("button", { name: "Add Delivery" }).click();

		return new ModifyDeliveriesDialog(this.page);
	}

	/** The Archived section, which is collapsed until somebody opens it. */
	get archivedSection(): Locator {
		return this.page.getByRole("button", { name: /^Archived Deliveries \(/ });
	}

	async openArchivedSection(): Promise<void> {
		await this.archivedSection.click();
	}

	getArchivedDeliveryByName(name: string): ArchivedDeliveryItem {
		return new ArchivedDeliveryItem(
			this.page.getByRole("button", {
				name: new RegExp(`unarchive delete ${name}`),
			}),
		);
	}

	getDeliveryByName(name: string): DeliveryItem {
		const deliveryButton = this.page.getByRole("button", {
			name: new RegExp(`edit delete ${name}`),
		});

		return new DeliveryItem(deliveryButton);
	}
}
