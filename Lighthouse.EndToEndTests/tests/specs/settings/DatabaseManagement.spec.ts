import path from "node:path";
import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";

test(`Should be able to restore and create a new backup`, async ({
	overviewPage,
}) => {
	await test.step("Restore backup from fixture", async () => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		const databaseManagement = await settingsPage.goToDatabaseManagement();

		const databaseProvider = await databaseManagement.getProvider();
		const fileName = `Lighthouse_Backup_2026-03-24_${databaseProvider}.zip`;
		const backupFilePath = path.join(
			process.cwd(),
			"tests",
			"fixtures",
			fileName,
		);

		await databaseManagement.restoreBackup(
			backupFilePath,
			TestConfig.BackupPassword,
		);
	});

	await test.step("Make sure backup was restored successfully", async () => {
		const mainPage = await overviewPage.lighthousePage.goToOverview();

		const teamLink = await mainPage.getTeamLink("Lighthouse");
		await expect(teamLink).toBeVisible();

		const portfolioLink = await mainPage.getPortfolioLink("Lighthouse");
		await expect(portfolioLink).toBeVisible();
	});

	const newBackupSecretPhrase = "Random 11! Test";
	let newBackupFilePath = "";

	await test.step("Create new backup", async () => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		const databaseManagement = await settingsPage.goToDatabaseManagement();

		newBackupFilePath = await databaseManagement.createBackup(
			newBackupSecretPhrase,
		);
	});

	await test.step("Clear database", async () => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		const databaseManagement = await settingsPage.goToDatabaseManagement();

		await databaseManagement.clearDatabase();
	});

	await test.step("Make sure database was cleared successfully", async () => {
		const mainPage = await overviewPage.lighthousePage.goToOverview();

		await expect(
			mainPage.page
				.getByRole("alert")
				.filter({ hasText: "No Portfolios found." }),
		).toBeVisible();
		await expect(
			mainPage.page.getByRole("alert").filter({ hasText: "No Teams found." }),
		).toBeVisible();
	});

	await test.step("Load created backup and check that it is correct", async () => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		const databaseManagement = await settingsPage.goToDatabaseManagement();

		await databaseManagement.restoreBackup(
			newBackupFilePath,
			newBackupSecretPhrase,
		);
	});

	await test.step("Make sure loaded backup was restored successfully", async () => {
		const mainPage = await overviewPage.lighthousePage.goToOverview();

		const teamLink = await mainPage.getTeamLink("Lighthouse");
		await expect(teamLink).toBeVisible();

		const portfolioLink = await mainPage.getPortfolioLink("Lighthouse");
		await expect(portfolioLink).toBeVisible();
	});
});
