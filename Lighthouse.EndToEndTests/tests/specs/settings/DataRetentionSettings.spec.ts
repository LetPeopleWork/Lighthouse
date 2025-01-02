import { expect, test } from "../../fixutres/LighthouseFixture";

test("Should persist Data Retention Settings Settings on Save", async ({
	overviewPage,
}) => {
	const settingsPage = await overviewPage.lighthousePage.goToSettings();
	let dataRetentionSettingsPage =
		await settingsPage.goToDataRetentionSettings();

	await test.step("Save New Settings", async () => {
		await dataRetentionSettingsPage.setMaximumDataRetentionTime(1886);
		await dataRetentionSettingsPage.updateDataRetentionSettings();
	});

	await test.step("Verify Changes were saved", async () => {
		await overviewPage.lightHousePage.goToOverview();
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		dataRetentionSettingsPage = await settingsPage.goToDataRetentionSettings();

		const maxDays =
			await dataRetentionSettingsPage.getMaximumDataRetentionTime();
		expect(maxDays).toBe(1886);
	});
});
