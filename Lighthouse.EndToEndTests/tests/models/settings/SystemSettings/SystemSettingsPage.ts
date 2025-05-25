import type { Page } from "@playwright/test";

export type PeriodicRefreshSettingType = "Team" | "Feature" | "Forecast";

export class SystemSettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async enableFeature(featureName: string): Promise<void> {
		const featureToggle = this.page
			.getByTestId(`${featureName}-toggle`)
			.getByRole("checkbox");

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
			.getByRole('spinbutton', { name: 'Maximum Data Retention Time (' })
			.fill(`${maxDays}`);
	}

	async getMaximumDataRetentionTime(): Promise<number> {
		const maxRetentionInput = this.page
			.getByRole('spinbutton', { name: /Maximum Data Retention Time/ });

		await maxRetentionInput.waitFor({state: 'visible'});

		const value =
			(await maxRetentionInput
				.inputValue()) ?? "0";
		return Number(value);
	}

	async updateDataRetentionSettings(): Promise<void> {
		await this.page
			.getByRole('button', { name: 'Update Data Retention Settings' })
			.click();
	}
}
