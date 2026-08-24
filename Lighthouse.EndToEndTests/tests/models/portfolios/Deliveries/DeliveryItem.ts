import { expect, type Locator } from "@playwright/test";
import { ArchiveConfirmationDialog } from "./ArchiveConfirmationDialog";
import { DeliveryDeletionDialog } from "./DeliveryDeletionDialog";
import { ModifyDeliveriesDialog } from "./ModifyDeliveriesDialog";

export class DeliveryItem {
	readonly container: Locator;
	readonly heading: Locator;

	constructor(container: Locator) {
		this.container = container;
		this.heading = container.getByRole("heading", { level: 3 });
	}

	async modifyDelivery(): Promise<ModifyDeliveriesDialog> {
		await this.container
			.getByRole("button", { name: "edit", exact: true })
			.click();

		return new ModifyDeliveriesDialog(this.container.page(), true);
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
		const badge = this.container
			.locator(String.raw`text=/\d+\s*Features/`)
			.first();
		const text = await badge.textContent();
		const match = text?.match(/(\d+)/);
		return match ? Number.parseInt(match[1], 10) : null;
	}

	/**
	 * Read off the forecast chip's trailing percentage rather than a label prefix — Story #5587
	 * slice-03 relabelled it to "All {features} by {date}: NN%". Returns null for the non-numeric
	 * states ("Cannot forecast", "Not enough data"), which carry no percentage at all.
	 */
	async getLikelihood(): Promise<number | null> {
		const label = await this.getForecastChipLabel();
		const match = label.match(/:\s*>?(\d+)%$/);
		return match ? Number.parseInt(match[1], 10) : null;
	}

	/**
	 * The delivery header's forecast chip, read as rendered rather than parsed against a label
	 * prefix. Story #5587 slice-03 replaces "Likelihood: NN%" with "All {features} by {date}: NN%",
	 * so `getLikelihood()` above stops matching; a getter that returns the raw label survives the
	 * relabel and lets a spec assert the copy itself.
	 *
	 * Scoped to the filled chip: the same AccordionSummary also renders one OUTLINED chip per
	 * completion-date percentile, so an unqualified `.MuiChip-label` matches several (ci-learnings —
	 * a shared locator on a page that hosts the component twice breaks in strict mode).
	 */
	get forecastChip(): Locator {
		return this.container.locator(".MuiChip-filled").first();
	}

	async getForecastChipLabel(): Promise<string> {
		return (await this.forecastChip.textContent())?.trim() ?? "";
	}

	/**
	 * The date the header renders beside the forecast chip. NOT `getTargetDate()` above, which looks
	 * for a "Target Date:" prefix that DeliverySection does not render — it returns null on this page
	 * and always has (verified live, 2026-07-29). Left in place because other callers only read
	 * `name` and `scope` off `getDetails()`; flagged rather than silently repaired here.
	 */
	async getDeliveryDate(): Promise<string | null> {
		const text = await this.container.textContent();
		const match = text?.match(/Delivery Date:\s*(\d{1,2}\/\d{1,2}\/\d{4})/);
		return match ? match[1] : null;
	}

	/**
	 * The breakdown grid's Likelihood column header. Keyed on `data-field`, not on the header text, so
	 * the locator survives Story #5587 slice-03 relabelling it.
	 *
	 * Page-scoped, and it has to be: `container` is the AccordionSummary, while the grid renders in the
	 * sibling AccordionDetails, so a container-scoped locator finds nothing (verified live 2026-07-29).
	 * That makes this an exception to the scope-to-container rule in docs/ci-learnings.md, and it holds
	 * only because one delivery is expanded at a time - `toggleDetails()` establishes the single match.
	 */
	get likelihoodColumnHeader(): Locator {
		return this.container
			.page()
			.locator('[role="columnheader"][data-field="likelihood"]');
	}

	/**
	 * The marker beside the name saying this delivery takes its name, date and features from the
	 * named source rather than from whoever is looking at it. Rendered on the collapsed row, so
	 * nothing has to be opened to see it; its tooltip is also its accessible name.
	 */
	boundIndicator(sourceLabel: string): Locator {
		return this.container.getByLabel(`Bound to ${sourceLabel}`);
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
				const number = Number.parseInt(text.trim().replace(/[^0-9]/g, ""), 10);
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

	async archive(): Promise<ArchiveConfirmationDialog> {
		await this.ArchiveButton.click();

		return new ArchiveConfirmationDialog(this.container.page());
	}

	get ArchiveButton(): Locator {
		return this.container.getByRole("button", { name: "archive", exact: true });
	}

	get EditButton(): Locator {
		return this.container.getByRole("button", { name: "edit", exact: true });
	}

	get DeleteButton(): Locator {
		return this.container.getByRole("button", { name: "delete", exact: true });
	}
}
