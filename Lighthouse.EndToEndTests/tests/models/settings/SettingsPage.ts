import type { Page } from "@playwright/test";
import { ProjectEditPage } from "../projects/ProjectEditPage";
import { TeamEditPage } from "../teams/TeamEditPage";
import { LogsPage } from "./Logs/LogsPage";
import { SystemSettingsPage } from "./SystemSettings/SystemSettingsPage";
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

	async goToSystemSettings(): Promise<SystemSettingsPage> {
		await this.page.getByTestId("system-settings-tab").click();

		return new SystemSettingsPage(this.page);
	}

	async goToLogs(): Promise<LogsPage> {
		await this.page.getByTestId("logs-tab").click();

		return new LogsPage(this.page);
	}
}
