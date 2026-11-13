import type { Page } from "@playwright/test";
import { RbacSettingsPage } from "../auth/rbac/RbacSettingsPage";
import { DemoDataPage } from "../teams/DemoDataPage";
import { ApiKeysSettingsPage } from "./ApiKeys/ApiKeysSettingsPage";
import { DatabaseManagementPage } from "./DatabaseManagement/DatabaseManagementPage";
import { EncryptionSettingsPage } from "./Encryption/EncryptionSettingsPage";
import { SystemInfoPage } from "./SystemInfo/SystemInfoPage";
import { SystemConfigurationPage } from "./SystemSettings/SystemConfigurationPage";

export type SettingsArea = "organization" | "access" | "system";

export class SettingsPage {
	page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	/** Opens an area from the header menu. */
	async openArea(area: SettingsArea): Promise<void> {
		await this.page.getByTestId("settings-menu").click();
		await this.page.getByTestId(`settings-area-${area}`).click();
	}

	/** Opens an area, then one of its tabs by test id. */
	private async openTab(area: SettingsArea, tabTestId: string): Promise<void> {
		await this.openArea(area);
		await this.page.getByTestId(tabTestId).click();
	}

	async goToDemoData(): Promise<DemoDataPage> {
		await this.openTab("system", "demo-data-tab");

		return new DemoDataPage(this.page);
	}

	async goToSystemConfiguration(): Promise<SystemConfigurationPage> {
		await this.openTab("organization", "configuration-tab");

		return new SystemConfigurationPage(this.page);
	}

	async goToSystemInfo(): Promise<SystemInfoPage> {
		await this.openTab("system", "system-info-tab");

		return new SystemInfoPage(this.page);
	}

	async goToDatabaseManagement(): Promise<DatabaseManagementPage> {
		await this.openTab("system", "database-tab");

		return new DatabaseManagementPage(this.page);
	}

	async goToEncryption(): Promise<EncryptionSettingsPage> {
		await this.openTab("system", "encryption-tab");

		const encryptionPage = new EncryptionSettingsPage(this.page);
		await encryptionPage.keyRing.waitFor();

		return encryptionPage;
	}

	async goToApiKeys(): Promise<ApiKeysSettingsPage> {
		await this.openTab("access", "api-keys-tab");

		return new ApiKeysSettingsPage(this.page);
	}

	async goToRbacSettings(): Promise<RbacSettingsPage> {
		await this.openTab("access", "rbac-tab");

		return new RbacSettingsPage(this.page);
	}
}
