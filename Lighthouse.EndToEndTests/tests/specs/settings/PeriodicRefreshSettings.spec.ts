import { expect, test } from "../../fixutres/LighthouseFixture";
import type { PeriodicRefreshSettingType } from "../../models/settings/SystemSettings/SystemSettingsPage";

const settings: PeriodicRefreshSettingType[] = ["Team", "Feature"];

for (const settingName of settings) {
	test(`Should persist ${settingName} Refresh Settings on Save`, async ({
		overviewPage,
	}) => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		let systemSettings = await settingsPage.goToSystemSettings();

		await test.step("Save New Settings", async () => {
			await systemSettings.setInterval(13, settingName);
			await systemSettings.setRefreshAfter(37, settingName);
			await systemSettings.setStartDelay(1886, settingName);

			await systemSettings.updateSettings(settingName);
		});

		await test.step("Verify Changes were saved", async () => {
			await overviewPage.lightHousePage.goToOverview();
			const settingsPage = await overviewPage.lighthousePage.goToSettings();
			systemSettings = await settingsPage.goToSystemSettings();

			const interval = await systemSettings.getInterval(settingName);
			expect(interval).toBe(13);

			const refreshAfter = await systemSettings.getRefreshAfter(settingName);
			expect(refreshAfter).toBe(37);

			const startDelay = await systemSettings.getStartDelay(settingName);
			expect(startDelay).toBe(1886);
		});
	});
}
