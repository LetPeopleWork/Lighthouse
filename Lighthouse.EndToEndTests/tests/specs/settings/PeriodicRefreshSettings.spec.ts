import { expect, test } from '../../fixutres/LighthouseFixture';
import { PeriodicRefreshSettingType } from '../../models/settings/PeriodicRefreshSettings/PeriodicRefreshSettingsPage';

const settings: PeriodicRefreshSettingType[] = [
    'Throughput',
    'Feature',
    'Forecast'
];

settings.forEach((settingName) => {
    test(`Should persist ${settingName} Refresh Settings on Save`, async ({ overviewPage }) => {
        const settingsPage = await overviewPage.lighthousePage.goToSettings();
        let periodicRefreshSettingsPage = await settingsPage.goToPeriodicRefreshSettings();

        await test.step("Save New Settings", async () => {
            await periodicRefreshSettingsPage.setInterval(13, settingName);
            await periodicRefreshSettingsPage.setRefreshAfter(37, settingName);
            await periodicRefreshSettingsPage.setStartDelay(1886, settingName);

            await periodicRefreshSettingsPage.updateSettings(settingName);
        });

        await test.step("Verify Changes were saved", async () => {
            await overviewPage.lightHousePage.goToOverview();
            const settingsPage = await overviewPage.lighthousePage.goToSettings();
            periodicRefreshSettingsPage = await settingsPage.goToPeriodicRefreshSettings();

            const interval = await periodicRefreshSettingsPage.getInterval(settingName);
            expect(interval).toBe(13);

            const refreshAfter = await periodicRefreshSettingsPage.getRefreshAfter(settingName);
            expect(refreshAfter).toBe(37);

            const startDelay = await periodicRefreshSettingsPage.getStartDelay(settingName);
            expect(startDelay).toBe(1886);
        });
    });
});