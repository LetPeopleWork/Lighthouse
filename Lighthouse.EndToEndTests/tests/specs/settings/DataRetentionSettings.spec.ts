import { expect, test } from "../../fixutres/LighthouseFixture";

test("Should persist Data Retention Settings Settings on Save", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lighthousePage.goToSettings();
	let systemSettings = await settingsPage.goToSystemSettings();

	await test.step("Save New Settings", async () => {
		await systemSettings.setMaximumDataRetentionTime(1886);
		await systemSettings.updateDataRetentionSettings();
	});

	await test.step("Verify Changes were saved", async () => {
		await overviewPage.lightHousePage.goToOverview();
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		systemSettings = await settingsPage.goToSystemSettings();

		const maxDays = await systemSettings.getMaximumDataRetentionTime();
		expect(maxDays).toBe(1886);
	});
});
