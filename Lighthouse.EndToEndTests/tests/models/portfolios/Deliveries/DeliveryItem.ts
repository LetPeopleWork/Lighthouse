import { expect, type Locator } from "@playwright/test";
import { DeliveryDeletionDialog } from "./DeliveryDeletionDialog";
import { ModifyDeliveriesDialog } from "./ModifyDeliveriesDialog";

export class DeliveryItem {
	readonly container: Locator;
	readonly heading: Locator;

	constructor(container: Locator) {
		this.container = container;
		this.heading = container.getByRole("heading", { level: 3 });
	}

	async getName(): Promise<string> {
		return (await this.heading.textContent()) || "";
	}

	async getTargetDate(): Promise<string | null> {
		const text = await this.container.textContent();
		const match = text?.match(/Target Date:\s*(\d{1,2}\/\d{1,2}\/\d{4})/);
		return match ? match[1] : null;
	}

	async getScope(): Promise<number | null> {
		const text = await this.container.textContent();
		const match = text?.match(/Scope:\s*(\d+)\s*Feature/);
		return match ? parseInt(match[1], 10) : null;
	}

	async getLikelihood(): Promise<number | null> {
		const text = await this.container.textContent();
		const match = text?.match(/Likelihood:\s*(\d+)%/);
		return match ? parseInt(match[1], 10) : null;
	}

	async getProgress(): Promise<string | null> {
		const text = await this.container.textContent();
		const match = text?.match(/(\d+%\s*\(\d+\/\d+\))/);
		return match ? match[1] : null;
	}

	async getDetails() {
		return {
			name: await this.getName(),
			targetDate: await this.getTargetDate(),
			scope: await this.getScope(),
			likelihood: await this.getLikelihood(),
			progress: await this.getProgress(),
		};
	}

	async toggleDetails(): Promise<void> {
		await this.container.click();

		await expect(this.container.page().getByText("Feature Name")).toBeVisible();
	}

	async getFeatureLikelihoods(): Promise<number[]> {
		const likelihoodCells = this.container
			.page()
			.locator('[data-field="likelihood"] .MuiChip-label');
		const count = await likelihoodCells.count();
		const likelihoods: number[] = [];

		for (let i = 0; i < count; i++) {
			const text = await likelihoodCells.nth(i).textContent();
			if (text) {
				const number = parseInt(text.trim().replace("%", ""), 10);
				if (!Number.isNaN(number)) {
					likelihoods.push(number);
				}
			}
		}

		return likelihoods;
	}

	async edit(): Promise<ModifyDeliveriesDialog> {
		await this.EditButton.click();

		return new ModifyDeliveriesDialog(this.container.page());
	}

	async delete(): Promise<DeliveryDeletionDialog> {
		await this.DeleteButton.click();

		return new DeliveryDeletionDialog(this.container.page());
	}

	get EditButton(): Locator {
		return this.container
			.page()
			.getByRole("button", { name: "edit", exact: true });
	}

	get DeleteButton(): Locator {
		return this.container
			.page()
			.getByRole("button", { name: "delete", exact: true });
	}
}
