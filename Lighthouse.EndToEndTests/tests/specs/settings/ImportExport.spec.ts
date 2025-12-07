import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { TestConfig } from "../../../playwright.config";
import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"Should Export and Import Correctly",
	async ({ testData, overviewPage }) => {
		const settingsPage = await overviewPage.lighthousePage.goToSettings();
		const systemSettings = await settingsPage.goToSystemSettings();

		const workTrackingSystems = testData.connections;
		const teams = testData.teams;
		const portfolios = testData.portfolios;

		let exportedConfigFileName = "";
		await test.step("Export should export file", async () => {
			exportedConfigFileName = await systemSettings.exportConfiguration();

			expect(fs.existsSync(exportedConfigFileName)).toBeTruthy();
			const fileContent = fs.readFileSync(exportedConfigFileName, "utf8");

			for (const system of workTrackingSystems) {
				expect(fileContent).toContain(system.name);
			}

			for (const team of teams) {
				expect(fileContent).toContain(team.name);
			}

			for (const portfolio of portfolios) {
				expect(fileContent).toContain(portfolio.name);
			}
		});

		await test.step("Verify Import fails for Invalid File", async () => {
			const tempDir = os.tmpdir();
			const invalidJsonPath = path.join(
				tempDir,
				`invalid-import-${Date.now()}.json`,
			);
			fs.writeFileSync(
				invalidJsonPath,
				`
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            This is definitely not valid JSON!
            {
                "unclosed": "object"
                "missing": "comma"
            }
        `,
			);

			const importDialog = await systemSettings.importConfiguration();
			await importDialog.selectFile(invalidJsonPath);

			expect(await importDialog.hasError()).toBeTruthy();
			await importDialog.close();
		});

		await test.step("Import should create new configuration", async () => {
			const importDialog = await systemSettings.importConfiguration();
			await importDialog.selectFile(exportedConfigFileName);
			await importDialog.toggleClearConfiguration();

			for (const system of workTrackingSystems) {
				expect(await importDialog.getImportElementStatus(system.name)).toBe(
					"New",
				);
			}

			for (const team of teams) {
				expect(await importDialog.getImportElementStatus(team.name)).toBe(
					"New",
				);
			}

			for (const portfolio of portfolios) {
				expect(await importDialog.getImportElementStatus(portfolio.name)).toBe(
					"New",
				);
			}

			await importDialog.goToNextStep();

			await importDialog.addSecretParameter(
				"Personal Access Token",
				TestConfig.AzureDevOpsToken,
			);
			await importDialog.addSecretParameter("Api Token", TestConfig.JiraToken);

			await importDialog.validate();
			await expect(importDialog.nextButton).toBeEnabled();
			await importDialog.goToNextStep();

			await importDialog.import();

			await importDialog.waitForImportToFinish();
			expect(await importDialog.wasSuccess()).toBeTruthy();
			await importDialog.close();
		});

		await test.step("Import should update existing settings", async () => {
			const importDialog = await systemSettings.importConfiguration();
			await importDialog.selectFile(exportedConfigFileName);

			for (const system of workTrackingSystems) {
				expect(await importDialog.getImportElementStatus(system.name)).toBe(
					"Update",
				);
			}

			for (const team of teams) {
				expect(await importDialog.getImportElementStatus(team.name)).toBe(
					"Update",
				);
			}

			for (const portfolio of portfolios) {
				expect(await importDialog.getImportElementStatus(portfolio.name)).toBe(
					"Update",
				);
			}

			await importDialog.goToNextStep();

			await importDialog.import();
			await importDialog.waitForImportToFinish();

			expect(await importDialog.wasSuccess()).toBeTruthy();

			await importDialog.close();
		});
	},
);
