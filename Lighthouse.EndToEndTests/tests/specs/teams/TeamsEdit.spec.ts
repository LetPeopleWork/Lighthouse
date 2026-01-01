import { TestConfig } from "../../../playwright.config";
import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

import { deleteWorkTrackingSystemConnectionByName } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";

const teamConfigurations = [
	{ name: "Azure DevOps", index: 0 },
	{ name: "Jira", index: 2 },
];

for (const { name, index } of teamConfigurations) {
	testWithData(
		`should allow save after validate when editing existing ${name} team`,
		async ({ testData, overviewPage }) => {
			const team = testData.teams[index];

			const teamEditPage = await overviewPage.editTeam(team.name);

			await expect(teamEditPage.validateButton).toBeEnabled();
			await expect(teamEditPage.saveButton).toBeDisabled();

			await teamEditPage.validate();

			await expect(teamEditPage.validateButton).toBeEnabled();
			await expect(teamEditPage.saveButton).toBeEnabled();
		},
	);
}

testWithData(
	"should disable validate button if not all mandatory fields are set",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamEditPage = await overviewPage.editTeam(team.name);

		await expect(teamEditPage.validateButton).toBeEnabled();

		await test.step("Team Name should be mandatory", async () => {
			const oldName = team.name;
			await teamEditPage.setName("");
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.setName(oldName);
			await expect(teamEditPage.validateButton).toBeEnabled();
		});

		await test.step("Throughput History should be mandatory and greater than 0", async () => {
			await teamEditPage.setThroughputHistory(0);
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.setThroughputHistory(-1);
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.setThroughputHistory(1);
			await expect(teamEditPage.validateButton).toBeEnabled();
		});

		await test.step("Work Item Query should be mandatory", async () => {
			await teamEditPage.setWorkItemQuery("");
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.setWorkItemQuery(
				`[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers" AND ([System.State] <> "Closed"  OR [System.Parent] <> "" OR [System.ChangedDate] >= "${historicalDateString}")`,
			);
			await expect(teamEditPage.validateButton).toBeEnabled();
		});

		await test.step("Work Item Types should be mandatory and more than 1", async () => {
			await teamEditPage.removeWorkItemType("Bug");
			await expect(teamEditPage.validateButton).toBeEnabled();

			await teamEditPage.removeWorkItemType("User Story");
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.addWorkItemType("Feature");
			await expect(teamEditPage.validateButton).toBeEnabled();
		});

		await test.step("Each state category should have at least one state", async () => {
			await teamEditPage.removeState("New");
			await teamEditPage.removeState("Planned");
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.addState("Backlog", "To Do");
			await expect(teamEditPage.validateButton).toBeEnabled();

			await teamEditPage.removeState("Active");
			await teamEditPage.removeState("Resolved");
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.addState("In Progress", "Doing");
			await expect(teamEditPage.validateButton).toBeEnabled();

			await teamEditPage.removeState("Closed");
			await expect(teamEditPage.validateButton).toBeDisabled();

			await teamEditPage.addState("Done", "Done");
			await expect(teamEditPage.validateButton).toBeEnabled();
		});

		await test.step("Advanced Configuration should not be mandatory", async () => {
			await teamEditPage.toggleAdvancedConfiguration();

			await teamEditPage.setParentOverrideField("CUSTOMFIELD");
			await expect(teamEditPage.validateButton).toBeEnabled();
			await teamEditPage.setParentOverrideField("");
			await expect(teamEditPage.validateButton).toBeEnabled();
		});
	},
);

const historicalDate = new Date(Date.now() - 5 * 24 * 60 * 60 * 1000);
const historicalDateString = historicalDate.toISOString().slice(0, 10);

const newTeamConfigurations = [
	{
		name: "Jira",
		workTrackingSystemIndex: 1,
		teamConfiguration: {
			validWorkItemTypes: ["Story", "Bug"],
			invalidWorkItemTypes: ["User Story", "Bugs"],
			validStates: { toDo: ["To Do"], doing: ["In Progress"], done: ["Done"] },
			invalidStates: { toDo: ["New"], doing: ["Active"], done: ["Closed"] },
			validQuery: 'project = "LGHTHSDMO" AND labels = "Lagunitas"',
			invalidQuery: 'project = "LGHTHSDMO" AND tags = "Lagunitas"',
		},
		workTrackingSystemOptions: [
			{ field: "Jira URL", value: "https://letpeoplework.atlassian.net" },
			{ field: "Username (Email)", value: "atlassian.pushchair@huser-berta.com" },
			{ field: "API Token", value: TestConfig.JiraToken },
		],
	},
	{
		name: "AzureDevOps",
		workTrackingSystemIndex: 0,
		teamConfiguration: {
			validWorkItemTypes: ["User Story", "Bug"],
			invalidWorkItemTypes: ["Story"],
			validStates: { toDo: ["New"], doing: ["Active"], done: ["Closed"] },
			invalidStates: {
				toDo: ["To Do"],
				doing: ["In Progress"],
				done: ["Done"],
			},
			validQuery: `[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers" AND ([System.State] <> "Closed"  OR [System.Parent] <> "" OR [System.ChangedDate] >= "${historicalDateString}")`,
			invalidQuery:
				'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Lighthouse Demo\\Binary Blazers"',
		},
		workTrackingSystemOptions: [
			{
				field: "Organization URL",
				value: "https://dev.azure.com/letpeoplework",
			},
			{ field: "Personal Access Token", value: TestConfig.AzureDevOpsToken },
		],
	},
];

for (const {
	name,
	workTrackingSystemIndex,
	teamConfiguration,
} of newTeamConfigurations) {
	testWithData(
		`should allow to create a new team for ${name}`,
		async ({ testData, overviewPage }) => {
			const newTeamPage = await overviewPage.addNewTeam();

			const newTeam = { id: 0, name: `My New ${name} team` };

			await test.step("Add general configuration", async () => {
				await newTeamPage.setName(newTeam.name);
				await newTeamPage.setThroughputHistory(20);
				await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newTeamPage.validateButton).toBeDisabled();
			});

			await test.step("Add Work Item Type Configuration", async () => {
				await newTeamPage.resetWorkItemTypes(
					["User Story", "Bug"],
					teamConfiguration.validWorkItemTypes,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newTeamPage.validateButton).toBeDisabled();
			});

			await test.step("Add State Configuration", async () => {
				await newTeamPage.resetStates(
					{
						toDo: ["New", "Proposed", "To Do"],
						doing: ["Active", "Resolved", "In Progress", "Committed"],
						done: ["Done", "Closed"],
					},
					teamConfiguration.validStates,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newTeamPage.validateButton).toBeDisabled();
			});

			await test.step("Add Tags", async () => {
				await newTeamPage.addTag("Urgent");
				await newTeamPage.addTag(name);
			});

			await test.step("Select Work Tracking System", async () => {
				const workTrackingSystem =
					testData.connections[workTrackingSystemIndex];
				await newTeamPage.selectWorkTrackingSystem(workTrackingSystem.name);
				await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);

				// Now we have all default configuration set
				await expect(newTeamPage.validateButton).toBeEnabled();
			});

			await test.step("Validate Settings", async () => {
				await newTeamPage.validate();
				await expect(newTeamPage.validateButton).toBeEnabled();
				await expect(newTeamPage.saveButton).toBeEnabled();
			});

			await test.step("Invalidate Work Item Query", async () => {
				await newTeamPage.setWorkItemQuery(teamConfiguration.invalidQuery);
				await newTeamPage.validate();
				await expect(newTeamPage.validateButton).toBeEnabled();
				await expect(newTeamPage.saveButton).toBeDisabled();

				await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);
			});

			await test.step("Invalidate Work Item Types", async () => {
				await newTeamPage.resetWorkItemTypes(
					teamConfiguration.validWorkItemTypes,
					teamConfiguration.invalidWorkItemTypes,
				);
				await newTeamPage.validate();
				await expect(newTeamPage.validateButton).toBeEnabled();
				await expect(newTeamPage.saveButton).toBeDisabled();

				await newTeamPage.resetWorkItemTypes(
					teamConfiguration.invalidWorkItemTypes,
					teamConfiguration.validWorkItemTypes,
				);
			});

			await test.step("Invalidate States", async () => {
				await newTeamPage.resetStates(
					teamConfiguration.validStates,
					teamConfiguration.invalidStates,
				);
				await newTeamPage.validate();
				await expect(newTeamPage.validateButton).toBeEnabled();
				await expect(newTeamPage.saveButton).toBeDisabled();

				await newTeamPage.resetStates(
					teamConfiguration.invalidStates,
					teamConfiguration.validStates,
				);
			});

			await test.step("Create New Team", async () => {
				await newTeamPage.validate();
				await expect(newTeamPage.saveButton).toBeEnabled();
				const teamInfoPage = await newTeamPage.save();

				await expect(teamInfoPage.updateTeamDataButton).toBeDisabled();
				newTeam.id = teamInfoPage.teamId;

				overviewPage.lightHousePage.goToOverview();

				await overviewPage.search(newTeam.name);
				const teamLink = await overviewPage.getTeamLink(newTeam.name);
				await expect(teamLink).toBeVisible();
			});
		},
	);
}

for (const {
	name: workTrackingSystemName,
	teamConfiguration,
	workTrackingSystemOptions,
} of newTeamConfigurations) {
	test(`should allow to create a new team with a new Work Tracking System ${workTrackingSystemName}`, async ({
		overviewPage,
		request,
	}) => {
		let newTeamPage = await overviewPage.lightHousePage.createNewTeam();

		await test.step("Add Valid Configuration for new team", async () => {
			await newTeamPage.setName(`My New ${workTrackingSystemName} team`);
			await newTeamPage.setWorkItemQuery(teamConfiguration.validQuery);
			await newTeamPage.resetWorkItemTypes(
				["User Story", "Bug"],
				teamConfiguration.validWorkItemTypes,
			);
			await newTeamPage.resetStates(
				{
					toDo: ["New", "Proposed", "To Do"],
					doing: ["Active", "Resolved", "In Progress", "Committed"],
					done: ["Done", "Closed"],
				},
				teamConfiguration.validStates,
			);
		});

		const newWorkTrackingSystemConnectionName = generateRandomName();
		await test.step("Add new Work Tracking System", async () => {
			let newWorkTrackingSystemDialog =
				await newTeamPage.addNewWorkTrackingSystem();

			newTeamPage = await newWorkTrackingSystemDialog.cancel();

			// No New Work Tracking System
			await expect(newTeamPage.validateButton).toBeDisabled();

			newWorkTrackingSystemDialog =
				await newTeamPage.addNewWorkTrackingSystem();
			await newWorkTrackingSystemDialog.selectWorkTrackingSystem(
				workTrackingSystemName,
			);

			for (const option of workTrackingSystemOptions) {
				await newWorkTrackingSystemDialog.setWorkTrackingSystemOption(
					option.field,
					option.value,
				);
			}

			await newWorkTrackingSystemDialog.setConnectionName(
				newWorkTrackingSystemConnectionName,
			);

			await newWorkTrackingSystemDialog.validate();
			await expect(newWorkTrackingSystemDialog.createButton).toBeEnabled();

			newTeamPage = await newWorkTrackingSystemDialog.create();

			await expect(newTeamPage.validateButton).toBeEnabled();
			await expect(newTeamPage.saveButton).toBeDisabled();

			await newTeamPage.validate();
			await expect(newTeamPage.validateButton).toBeEnabled();
			await expect(newTeamPage.saveButton).toBeEnabled();
		});

		await deleteWorkTrackingSystemConnectionByName(
			request,
			newWorkTrackingSystemConnectionName,
		);
	});
}
