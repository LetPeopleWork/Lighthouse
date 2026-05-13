import type { Page } from "@playwright/test";
import { RbacSettingsPage } from "../auth/rbac/RbacSettingsPage";
import { DemoDataPage } from "../teams/DemoDataPage";
import { ApiKeysSettingsPage } from "./ApiKeys/ApiKeysSettingsPage";
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

	async goToApiKeys(): Promise<ApiKeysSettingsPage> {
		await this.page.getByTestId("api-keys-tab").click();

		return new ApiKeysSettingsPage(this.page);
	}

	async goToRbacSettings(): Promise<RbacSettingsPage> {
		await this.page.getByTestId("rbac-tab").click();

		return new RbacSettingsPage(this.page);
	}
}
