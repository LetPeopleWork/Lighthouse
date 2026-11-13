import type { Locator, Page } from "@playwright/test";
import { WorkTrackingSystemCreateWizard } from "./WorkTrackingSystemCreateWizard";
import { WorkTrackingSystemEditPage } from "./WorkTrackingSystemEditPage";

/** System, Connections: the list of configured connections with Edit and Delete, and the button to connect a new one. */
export class SystemConnectionsPage {
	readonly page: Page;

	constructor(page: Page) {
		this.page = page;
	}

	get panel(): Locator {
		return this.page.getByTestId("connections-panel");
	}

	private connectionRow(connectionName: string): Locator {
		return this.panel
			.getByRole("row")
			.filter({ has: this.page.getByRole("link", { name: connectionName }) });
	}

	async addConnection(): Promise<WorkTrackingSystemCreateWizard> {
		await this.panel
			.getByRole("button", { name: "Connect Work Tracking System" })
			.click();

		return new WorkTrackingSystemCreateWizard(this.page);
	}

	async editConnection(
		connectionName: string,
	): Promise<WorkTrackingSystemEditPage> {
		await this.connectionRow(connectionName)
			.getByTestId("edit-connection-button")
			.click();

		return new WorkTrackingSystemEditPage(this.page);
	}

	async deleteConnection(connectionName: string): Promise<void> {
		await this.connectionRow(connectionName)
			.getByTestId("delete-connection-button")
			.click();
	}
}
