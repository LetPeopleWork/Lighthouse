import type { Locator, Page } from "@playwright/test";

export type PeriodicRefreshSettingType = "Team" | "Feature" | "Forecast";

/** Settings > Organization > Refresh & ordering. */
export class RefreshAndOrderingPage {
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
				response.url().includes("/api/latest/optionalfeatures") &&
				response.status() === 200,
		);

		await featureToggle.click();

		await featureToggleRequest;
	}

	/**
	 * Hands ordering ownership to this instance and waits for the switch to be taken, so a caller never
	 * navigates away on a stale "saved".
	 */
	async handOrderingOverToThisInstance(): Promise<void> {
		await this.enableFeature("FeatureOrdering");
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

	get teamRefreshSettings(): Locator {
		return this.page.getByText("Team RefreshInterval (Minutes");
	}

	get featureRefreshSettings(): Locator {
		return this.page.getByText("Feature RefreshInterval (");
	}

	get behaviourSettings(): Locator {
		return this.page.getByText("Behaviour SettingsNameDescriptionEnabled", {
			exact: false,
		});
	}
}
