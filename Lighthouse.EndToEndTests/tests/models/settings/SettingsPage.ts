import type { Page } from "@playwright/test";
import { ProjectEditPage } from "../projects/ProjectEditPage";
import { TeamEditPage } from "../teams/TeamEditPage";
import { DataRetentionSettingsPage } from "./DataRetentionSettings/DataRetentionSettingsPage";
import { LogsPage } from "./Logs/LogsPage";
import { PeriodicRefreshSettingsPage } from "./PeriodicRefreshSettings/PeriodicRefreshSettingsPage";
import { OptionalFeaturesPage } from "./OptionalFeatures/OptionalFeaturesPage";
import { WorkTrackingSystemsSettingsPage } from "./WorkTrackingSystems/WorkTrackingSystemsSettingsPage";

export class SettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async goToWorkTrackingSystems(): Promise<WorkTrackingSystemsSettingsPage> {
		await this.page.getByTestId("work-tracking-tab").click();

		return new WorkTrackingSystemsSettingsPage(this.page);
	}

	async goToDefaultTeamSettings(): Promise<TeamEditPage> {
		await this.page.getByTestId("default-team-settings-tab").click();

		return new TeamEditPage(this.page);
	}

	async goToDefaultProjectSettings(): Promise<ProjectEditPage> {
		await this.page.getByTestId("default-project-settings-tab").click();

		return new ProjectEditPage(this.page);
	}

	async goToPeriodicRefreshSettings(): Promise<PeriodicRefreshSettingsPage> {
		await this.page.getByTestId("periodic-refresh-settings-tab").click();

		return new PeriodicRefreshSettingsPage(this.page);
	}

	async goToDataRetentionSettings(): Promise<DataRetentionSettingsPage> {
		await this.page.getByTestId("data-retention-settings-tab").click();

		return new DataRetentionSettingsPage(this.page);
	}

	async goToOptionalFeatures(): Promise<OptionalFeaturesPage> {
		await this.page.getByTestId("optional-features-tab").click();

		return new OptionalFeaturesPage(this.page);
	}

	async goToLogs(): Promise<LogsPage> {
		await this.page.getByTestId("logs-tab").click();

		return new LogsPage(this.page);
	}
}
