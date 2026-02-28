import type { Page } from "@playwright/test";
import { DemoDataPage } from "../teams/DemoDataPage";
import { LogsPage } from "./Logs/LogsPage";
import { SystemConfigurationPage } from "./SystemSettings/SystemConfigurationPage";

export class SettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	async goToDemoData(): Promise<DemoDataPage> {
		await this.page.getByTestId("demo-data-tab").click();

		return new DemoDataPage(this.page);
	}

	async goToSystemConfiguration(): Promise<SystemConfigurationPage> {
		await this.page.getByTestId("configuration-tab").click();

		return new SystemConfigurationPage(this.page);
	}

	async goToLogs(): Promise<LogsPage> {
		await this.page.getByTestId("logs-tab").click();

		return new LogsPage(this.page);
	}
}
