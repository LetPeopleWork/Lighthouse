import type { Page } from "@playwright/test";
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

	getDeliveryByName(name: string): DeliveryItem {
		const deliveryButton = this.page.getByRole("button", {
			name: new RegExp(`edit delete ${name}`),
		});

		return new DeliveryItem(deliveryButton);
	}
}
