import path from "node:path";
import type { Locator, Page } from "@playwright/test";
import { ImportDialog } from "./ImportDialog";

export type PeriodicRefreshSettingType = "Team" | "Feature" | "Forecast";

export class SystemSettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async enableFeature(featureName: string): Promise<void> {
		const featureToggle = this.page
			.getByTestId(`${featureName}-toggle`)
			.getByRole("switch");

		if (await featureToggle.isChecked()) {
			return;
		}

		const featureToggleRequest = this.page.waitForResponse(
			(response) =>
				response.url().includes("/api/optionalfeatures") &&
				response.status() === 200,
		);

		await featureToggle.click();

		// Make sure the request is completed
		await featureToggleRequest;
	}

	async setInterval(
		interval: number,
		settings: PeriodicRefreshSettingType,
	): Promise<void> {
		await this.page
			.getByTestId(`refresh-interval-${settings}`)
			.getByLabel("Interval (Minutes)")
			.fill(`${interval}`);
	}

	async getInterval(settings: PeriodicRefreshSettingType): Promise<number> {
		const value =
			(await this.page
				.getByTestId(`refresh-interval-${settings}`)
				.getByLabel("Interval (Minutes)")
				.inputValue()) ?? "0";
		return Number(value);
	}

	async setRefreshAfter(
		refreshAfter: number,
		settings: PeriodicRefreshSettingType,
	): Promise<void> {
		await this.page
			.getByTestId(`refresh-after-${settings}`)
			.getByLabel("Refresh After (Minutes)")
			.fill(`${refreshAfter}`);
	}

	async getRefreshAfter(settings: PeriodicRefreshSettingType): Promise<number> {
		const value =
			(await this.page
				.getByTestId(`refresh-after-${settings}`)
				.getByLabel("Refresh After (Minutes)")
				.inputValue()) ?? "0";
		return Number(value);
	}

	async setStartDelay(
		startDelay: number,
		settings: PeriodicRefreshSettingType,
	): Promise<void> {
		await this.page
			.getByTestId(`start-delay-${settings}`)
			.getByLabel("Start Delay (Minutes)")
			.fill(`${startDelay}`);
	}

	async getStartDelay(settings: PeriodicRefreshSettingType): Promise<number> {
		const value =
			(await this.page
				.getByTestId(`start-delay-${settings}`)
				.getByLabel("Start Delay (Minutes)")
				.inputValue()) ?? "0";
		return Number(value);
	}

	async updateSettings(settings: PeriodicRefreshSettingType): Promise<void> {
		await this.page
			.getByRole("button", { name: `Update ${settings} Settings` })
			.click();
	}

	async setMaximumDataRetentionTime(maxDays: number): Promise<void> {
		await this.page
			.getByRole("spinbutton", { name: "Maximum Data Retention Time (" })
			.fill(`${maxDays}`);
	}

	async getMaximumDataRetentionTime(): Promise<number> {
		const maxRetentionInput = this.page.getByRole("spinbutton", {
			name: /Maximum Data Retention Time/,
		});

		await maxRetentionInput.waitFor({ state: "visible" });

		const value = (await maxRetentionInput.inputValue()) ?? "0";
		return Number(value);
	}
	async updateDataRetentionSettings(): Promise<void> {
		await this.page
			.getByRole("button", { name: "Update Data Retention Settings" })
			.click();
	}

	async clickImportConfigurationButton(): Promise<void> {
		await this.page.getByTestId("import-configuration-button").click();
	}

	async isImportConfigurationDialogOpen(): Promise<boolean> {
		return await this.page
			.getByTestId("import-configuration-dialog")
			.isVisible();
	}

	async closeImportConfigurationDialog(): Promise<void> {
		await this.page.getByRole("button", { name: "Cancel" }).click();
	}

	async exportConfiguration(): Promise<string> {
		const downloadPromise = this.page.waitForEvent("download");

		await this.page
			.getByRole("button", { name: "Export Configuration" })
			.click();

		const download = await downloadPromise;
		const fileName = download.suggestedFilename();
		const filePath = path.join(process.cwd(), "temp-downloads", fileName);
		await download.saveAs(filePath);

		return filePath;
	}

	async importConfiguration(): Promise<ImportDialog> {
		await this.page
			.getByRole("button", { name: "Import Configuration" })
			.click();
		return new ImportDialog(this.page);
	}

	get teamRefreshSettings(): Locator {
		return this.page.getByText("Team RefreshInterval (Minutes");
	}

	get featureRefreshSettings(): Locator {
		return this.page.getByText("Feature RefreshInterval (");
	}

	get dataRetentionSettings(): Locator {
		return this.page.getByText(
			"Data Retention SettingsMaximum Data Retention Time (Days)Maximum Data Retention",
		);
	}

	get optionalFeatures(): Locator {
		return this.page.getByText("Optional FeaturesNameDescriptionEnabled", {
			exact: false,
		});
	}

	get lighthouseConfiguration(): Locator {
		return this.page.getByText(
			"Lighthouse ConfigurationExport ConfigurationImport Configuration",
		);
	}

	get terminologyConfiguration(): Locator {
		return this.page.getByText("Terminology ConfigurationUse");
	}
}
