import type { Locator, Page } from "@playwright/test";
import { LighthousePage } from "../app/LighthousePage";
import { OverviewPage } from "../overview/OverviewPage";
import { LoginPage } from "./LoginPage";

export class BlockedPage {
	readonly page: Page;
	readonly container: Locator;
	readonly fileInput: Locator;
	readonly uploadButton: Locator;
	readonly uploadError: Locator;
	readonly logoutButton: Locator;

	constructor(page: Page) {
		this.page = page;
		this.container = page.getByTestId("blocked-page");
		this.fileInput = page.getByTestId("license-file-input");
		this.uploadButton = page.getByTestId("upload-license-button");
		this.uploadError = page.getByTestId("upload-error");
		this.logoutButton = page.getByTestId("blocked-logout-button");
	}

	async uploadLicense(filePath: string): Promise<OverviewPage> {
		await this.fileInput.setInputFiles(filePath);

		return new OverviewPage(this.page, new LighthousePage(this.page));
	}

	async clickLogout(): Promise<LoginPage> {
		await this.logoutButton.click();
		return new LoginPage(this.page);
	}
}
