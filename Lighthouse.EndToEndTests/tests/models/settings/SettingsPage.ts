import type { Page } from "@playwright/test";
import { DemoDataPage } from "../teams/DemoDataPage";
import { DatabaseManagementPage } from "./DatabaseManagement/DatabaseManagementPage";
import { SystemInfoPage } from "./SystemInfo/SystemInfoPage";
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

	async goToSystemInfo(): Promise<SystemInfoPage> {
		await this.page.getByTestId("system-info-tab").click();

		return new SystemInfoPage(this.page);
	}

	async goToDatabaseManagement(): Promise<DatabaseManagementPage> {
		await this.page.getByTestId("database-tab").click();

		return new DatabaseManagementPage(this.page);
	}
}
