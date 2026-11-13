import type { Page } from "@playwright/test";
import { RbacSettingsPage } from "../auth/rbac/RbacSettingsPage";
import { DemoDataPage } from "../teams/DemoDataPage";
import { ApiKeysSettingsPage } from "./ApiKeys/ApiKeysSettingsPage";
import { DatabaseManagementPage } from "./DatabaseManagement/DatabaseManagementPage";
import { EncryptionSettingsPage } from "./Encryption/EncryptionSettingsPage";
import { SystemInfoPage } from "./SystemInfo/SystemInfoPage";
import { CalendarPage } from "./SystemSettings/CalendarPage";
import { RefreshAndOrderingPage } from "./SystemSettings/RefreshAndOrderingPage";
import { TerminologyPage } from "./SystemSettings/TerminologyPage";

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

	async goToTerminology(): Promise<TerminologyPage> {
		await this.openTab("organization", "terminology-tab");

		return new TerminologyPage(this.page);
	}

	async goToCalendar(): Promise<CalendarPage> {
		await this.openTab("organization", "calendar-tab");

		return new CalendarPage(this.page);
	}

	async goToRefreshAndOrdering(): Promise<RefreshAndOrderingPage> {
		await this.openTab("organization", "refresh-ordering-tab");

		return new RefreshAndOrderingPage(this.page);
	}

	async goToSystemInfo(): Promise<SystemInfoPage> {
		await this.openTab("system", "system-info-tab");

		return new SystemInfoPage(this.page);
	}

	/** The log level and the log viewer, which sit on their own tab beside System Info. */
	async goToLogs(): Promise<SystemInfoPage> {
		await this.openTab("system", "logs-tab");

		return new SystemInfoPage(this.page);
	}

	/** The license status with its import and clear actions, last tab of System. */
	async goToLicense(): Promise<Page> {
		await this.openTab("system", "license-tab");

		return this.page;
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
