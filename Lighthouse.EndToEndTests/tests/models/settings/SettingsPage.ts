import type { Page } from "@playwright/test";
import { DemoDataPage } from "../teams/DemoDataPage";
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

	async goToDemoData(): Promise<DemoDataPage> {
		await this.page.getByTestId("demo-data-tab").click();

		return new DemoDataPage(this.page);
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
