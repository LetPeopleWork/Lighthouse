import type { Page } from "@playwright/test";
import { RbacSettingsPage } from "../auth/rbac/RbacSettingsPage";
import { DemoDataPage } from "../teams/DemoDataPage";
import { ApiKeysSettingsPage } from "./ApiKeys/ApiKeysSettingsPage";
import { DatabaseManagementPage } from "./DatabaseManagement/DatabaseManagementPage";
import { EncryptionSettingsPage } from "./Encryption/EncryptionSettingsPage";
import { CalendarPage } from "./Organization/CalendarPage";
import { RefreshAndOrderingPage } from "./Organization/RefreshAndOrderingPage";
import { TerminologyPage } from "./Organization/TerminologyPage";
import { SystemInfoPage } from "./SystemInfo/SystemInfoPage";
import { SystemConnectionsPage } from "./WorkTrackingSystems/SystemConnectionsPage";

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

	/** The configured connections, first tab of System and for System Admins only. */
	async goToConnections(): Promise<SystemConnectionsPage> {
		await this.openTab("system", "connections-tab");

		return new SystemConnectionsPage(this.page);
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
