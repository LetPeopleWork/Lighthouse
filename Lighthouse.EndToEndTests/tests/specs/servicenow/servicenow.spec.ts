import { TestConfig } from "../../../playwright.config";
import { expect, test } from "../../fixutres/LighthouseFixture";
import { generateRandomName } from "../../helpers/names";
import { takeDialogScreenshot } from "../../helpers/screenshots";
import { TeamDetailPage } from "../../models/teams/TeamDetailPage";

// Shared with the connecting account on the PDI the backend integration tests use. Boards are
// shared, not roled (ADR-126), so this name is what the admin account can actually see.
const BOARD_NAME = "Incidents Kanban";

test("should be able to handle a team defined by a ServiceNow Visual Task Board", async ({
	overviewPage,
}) => {
	const workTrackingSystem = {
		name: generateRandomName(),
	};

	await test.step("Create ServiceNow Work Tracking System Connection", async () => {
		await overviewPage.lightHousePage.goToOverview();
		const workTrackingSystemCreationWizard = await overviewPage.addConnection();

		await workTrackingSystemCreationWizard.selectWorkTrackingSystemType(
			"ServiceNow",
		);

		await workTrackingSystemCreationWizard.setWorkTrackingSystemOption(
			"ServiceNow Instance URL",
			TestConfig.ServiceNowInstanceUrl,
		);
		await workTrackingSystemCreationWizard.setWorkTrackingSystemOption(
			"Username",
			TestConfig.ServiceNowUsername,
		);
		await workTrackingSystemCreationWizard.setWorkTrackingSystemOption(
			"Password",
			TestConfig.ServiceNowPassword,
		);

		await workTrackingSystemCreationWizard.goToNextStep();

		await workTrackingSystemCreationWizard.setConnectionName(
			workTrackingSystem.name,
		);

		await workTrackingSystemCreationWizard.create();
	});

	await test.step("Create ServiceNow Team from a Visual Task Board", async () => {
		const newTeam = { name: generateRandomName() };

		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Choose Connection", async () => {
			await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);
		});

		await test.step("@screenshot Select Visual Task Board in Wizard", async () => {
			const boardWizard = await newTeamPage.selectWizard("ServiceNow");
			await boardWizard.selectByName(BOARD_NAME);

			await expect(boardWizard.boardInformationPanel).toBeVisible();
			expect(await boardWizard.confirmButton.isEnabled()).toBeTruthy();

			await takeDialogScreenshot(
				newTeamPage.page.getByRole("dialog"),
				"concepts/servicenow_wizard.png",
				5,
				1000,
			);

			newTeamPage = await boardWizard.confirm();
		});

		await test.step("Add Name", async () => {
			// The board pre-fills query, work item types and states, so only the name is left.
			await newTeamPage.setName(newTeam.name);

			await expect(newTeamPage.createButton).toBeEnabled();
		});

		await test.step("Create New Team", async () => {
			const teamInfoPage = await newTeamPage.create(
				(page) => new TeamDetailPage(page),
			);

			await expect(teamInfoPage.updateTeamDataButton).toBeEnabled();

			const teamsPage = await overviewPage.lightHousePage.goToOverview();
			await teamsPage.search(newTeam.name);
			const teamLink = await teamsPage.getTeamLink(newTeam.name);
			await expect(teamLink).toBeVisible();
		});
	});
});
