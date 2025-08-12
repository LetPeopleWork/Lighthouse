import { TestConfig } from "../../../playwright.config";
import {
	expect,
	test,
	testWithData,
	testWithUpdatedTeams,
} from "../../fixutres/LighthouseFixture";
import { deleteWorkTrackingSystemConnectionByName } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";

const newProjectConfigurations = [
	{ name: "Azure DevOps", index: 0, involvedTeams: [1] },
	{ name: "Jira", index: 2, involvedTeams: [2] },
];

for (const { name, involvedTeams, index } of newProjectConfigurations) {
	testWithUpdatedTeams(involvedTeams)(
		`should allow save after validate when editing existing ${name} project`,
		async ({ testData, overviewPage }) => {
			const project = testData.projects[index];

			const projectsPage = await overviewPage.lightHousePage.goToProjects();
			const projectEditPage = await projectsPage.editProject(project);

			await expect(projectEditPage.validateButton).toBeEnabled();
			await expect(projectEditPage.saveButton).toBeDisabled();

			await projectEditPage.validate();

			await expect(projectEditPage.validateButton).toBeEnabled();
			await expect(projectEditPage.saveButton).toBeEnabled();
		},
	);
}

testWithData(
	"should disable validate button if not all mandatory fields are set",
	async ({ testData, overviewPage }) => {
		const project = testData.projects[0];
		const team = testData.teams[0];

		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		const projectEditPage = await projectsPage.editProject(project);

		await expect(projectEditPage.validateButton).toBeEnabled();

		await test.step("Project Name should be mandatory", async () => {
			const oldName = project.name;
			await projectEditPage.setName("");
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.setName(oldName);
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Work Item Query should be mandatory", async () => {
			await projectEditPage.setWorkItemQuery("");
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.setWorkItemQuery(
				'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"',
			);
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Work Item Types should be mandatory and more than 1", async () => {
			await projectEditPage.removeWorkItemType("Epic");
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.addWorkItemType("Epic");
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Involved Teams should be mandatory and more than 1", async () => {
			await projectEditPage.deselectTeam(team.name);
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.selectTeam(team.name);
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Each state category should have at least one state", async () => {
			await projectEditPage.removeState("New");
			await projectEditPage.removeState("Planned");
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.addState("Backlog", "To Do");
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.removeState("Active");
			await projectEditPage.removeState("Resolved");
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.addState("In Progress", "Doing");
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.removeState("Closed");
			await expect(projectEditPage.validateButton).toBeDisabled();

			await projectEditPage.addState("Done", "Done");
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Unparented Work Item Query should not be mandatory", async () => {
			await projectEditPage.toggleUnparentedWorkItemConfiguration();
			await projectEditPage.setUnparentedWorkItemQuery(
				'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"',
			);
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.setUnparentedWorkItemQuery("");
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Default Feature Size Configuration should not be mandatory", async () => {
			await projectEditPage.toggleDefaultFeatureSizeConfiguration();

			await projectEditPage.setSizeEstimateField("CUSTOMFIELD_1337");
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.setSizeEstimateField("");
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.addSizeOverrideState("New");
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.removeSizeOverrideState("New");
			await expect(projectEditPage.validateButton).toBeEnabled();
		});

		await test.step("Ownership Setting Configuration should not be mandatory", async () => {
			await projectEditPage.toggleOwnershipSettings();

			await projectEditPage.setFeatureOwnerField("System.AreaPath");
			await expect(projectEditPage.validateButton).toBeEnabled();

			await projectEditPage.setFeatureOwnerField("");
			await expect(projectEditPage.validateButton).toBeEnabled();
		});
	},
);

testWithData(
	"should handle Owning Team correctly",
	async ({ testData, overviewPage }) => {
		const project = testData.projects[0];
		const [team1, team2, team3] = testData.teams;

		const projectsPage = await overviewPage.lightHousePage.goToProjects();
		const projectEditPage = await projectsPage.editProject(project);

		await test.step("No Team is selected as Owner by Default", async () => {
			await projectEditPage.toggleOwnershipSettings();

			const owningTeamValue = await projectEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe("​");
		});

		await test.step("Only Involved Team and None can be selected as Owner", async () => {
			const availableOptions = await projectEditPage.getPotentialOwningTeams();

			expect(availableOptions).toContain("None");
			expect(availableOptions).toContain(team1.name);
			expect(availableOptions).not.toContain(team2.name);
			expect(availableOptions).not.toContain(team3.name);
		});

		await test.step("Including more teams should allow to pick them as owners", async () => {
			await projectEditPage.selectTeam(team2.name);
			await projectEditPage.selectTeam(team3.name);

			const availableOptions = await projectEditPage.getPotentialOwningTeams();
			expect(availableOptions).toContain("None");
			expect(availableOptions).toContain(team1.name);
			expect(availableOptions).toContain(team2.name);
			expect(availableOptions).toContain(team3.name);
		});

		await test.step("Removing Owning Team from Involved Teams will reset Owning Team", async () => {
			await projectEditPage.selectOwningTeam(team1.name);

			let owningTeamValue = await projectEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe(team1.name);

			await projectEditPage.deselectTeam(team1.name);
			owningTeamValue = await projectEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe("​");
		});
	},
);

const newTeamConfigurations = [
	{
		name: "Jira",
		workTrackingSystemIndex: 1,
		involvedTeams: [2],
		projectConfiguration: {
			validWorkItemTypes: ["Epic"],
			invalidWorkItemTypes: ["Feature"],
			validStates: { toDo: ["To Do"], doing: ["In Progress"], done: ["Done"] },
			invalidStates: { toDo: ["New"], doing: ["Active"], done: ["Closed"] },
			validQuery: 'project = "LGHTHSDMO" AND fixVersion = "Oberon Initiative"',
			invalidQuery: 'project = "LGHTHSDMO" AND labels = "Lagunitas"',
			involvedTeams: [2],
		},
		workTrackingSystemOptions: [
			{ field: "Jira Url", value: "https://letpeoplework.atlassian.net" },
			{ field: "Username", value: "atlassian.pushchair@huser-berta.com" },
			{ field: "Api Token", value: TestConfig.JiraToken },
		],
	},
	{
		name: "AzureDevOps",
		workTrackingSystemIndex: 0,
		involvedTeams: [1],
		projectConfiguration: {
			validWorkItemTypes: ["Epic"],
			invalidWorkItemTypes: ["Feature"],
			validStates: { toDo: ["New"], doing: ["Active"], done: ["Closed"] },
			invalidStates: {
				toDo: ["To Do"],
				doing: ["In Progress"],
				done: ["Done"],
			},
			validQuery:
				'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"',
			invalidQuery:
				'[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"',
			involvedTeams: [1],
		},
		workTrackingSystemOptions: [
			{
				field: "Azure DevOps Url",
				value: "https://dev.azure.com/letpeoplework",
			},
			{ field: "Personal Access Token", value: TestConfig.AzureDevOpsToken },
		],
	},
];

for (const teamConfiguration of newTeamConfigurations) {
	testWithUpdatedTeams(teamConfiguration.involvedTeams)(
		`should allow to create a project team for ${teamConfiguration.name}`,
		async ({ testData, overviewPage }) => {
			let projectPage = await overviewPage.lightHousePage.goToProjects();
			const newProjectPage = await projectPage.addNewProject();

			const newProject = {
				id: 0,
				name: `My New ${teamConfiguration.name} Project`,
			};

			await test.step("Add general configuration", async () => {
				await newProjectPage.setName(newProject.name);
				await newProjectPage.setWorkItemQuery(
					teamConfiguration.projectConfiguration.validQuery,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newProjectPage.validateButton).toBeDisabled();
			});

			await test.step("Add Work Item Type Configuration", async () => {
				await newProjectPage.resetWorkItemTypes(
					["Epic"],
					teamConfiguration.projectConfiguration.validWorkItemTypes,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newProjectPage.validateButton).toBeDisabled();
			});

			await test.step("Add Involved Teams Configuration", async () => {
				for (const teamIndex of teamConfiguration.projectConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newProjectPage.selectTeam(team.name);
				}

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newProjectPage.validateButton).toBeDisabled();
			});

			await test.step("Add State Configuration", async () => {
				await newProjectPage.resetStates(
					{
						toDo: ["New", "Proposed", "To Do"],
						doing: ["Active", "Resolved", "In Progress", "Committed"],
						done: ["Done", "Closed"],
					},
					teamConfiguration.projectConfiguration.validStates,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newProjectPage.validateButton).toBeDisabled();
			});

			await test.step("Add Tags", async () => {
				await newProjectPage.addTag("Important");
				await newProjectPage.addTag(teamConfiguration.name);
			});

			await test.step("Select Work Tracking System", async () => {
				const workTrackingSystem =
					testData.connections[teamConfiguration.workTrackingSystemIndex];
				await newProjectPage.selectWorkTrackingSystem(workTrackingSystem.name);

				// Now we have all default configuration set
				await expect(newProjectPage.validateButton).toBeEnabled();
			});

			await test.step("Validate Settings", async () => {
				await newProjectPage.validate();
				await expect(newProjectPage.validateButton).toBeEnabled();
				await expect(newProjectPage.saveButton).toBeEnabled();
			});

			await test.step("Invalidate Work Item Query", async () => {
				await newProjectPage.setWorkItemQuery(
					teamConfiguration.projectConfiguration.invalidQuery,
				);
				await newProjectPage.validate();
				await expect(newProjectPage.validateButton).toBeEnabled();
				await expect(newProjectPage.saveButton).toBeDisabled();

				await newProjectPage.setWorkItemQuery(
					teamConfiguration.projectConfiguration.validQuery,
				);
			});

			await test.step("Invalidate Work Item Types", async () => {
				await newProjectPage.resetWorkItemTypes(
					teamConfiguration.projectConfiguration.validWorkItemTypes,
					teamConfiguration.projectConfiguration.invalidWorkItemTypes,
				);
				await newProjectPage.validate();
				await expect(newProjectPage.validateButton).toBeEnabled();
				await expect(newProjectPage.saveButton).toBeDisabled();

				await newProjectPage.resetWorkItemTypes(
					teamConfiguration.projectConfiguration.invalidWorkItemTypes,
					teamConfiguration.projectConfiguration.validWorkItemTypes,
				);
			});

			await test.step("Invalidate Involved Teams", async () => {
				for (const teamIndex of teamConfiguration.projectConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newProjectPage.deselectTeam(team.name);
				}

				await expect(newProjectPage.validateButton).toBeDisabled();
				await expect(newProjectPage.saveButton).toBeDisabled();

				for (const teamIndex of teamConfiguration.projectConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newProjectPage.selectTeam(team.name);
				}
			});

			await test.step("Invalidate States", async () => {
				await newProjectPage.resetStates(
					teamConfiguration.projectConfiguration.validStates,
					teamConfiguration.projectConfiguration.invalidStates,
				);
				await newProjectPage.validate();
				await expect(newProjectPage.validateButton).toBeEnabled();
				await expect(newProjectPage.saveButton).toBeDisabled();

				await newProjectPage.resetStates(
					teamConfiguration.projectConfiguration.invalidStates,
					teamConfiguration.projectConfiguration.validStates,
				);
			});

			await test.step("Create New Project", async () => {
				await newProjectPage.validate();
				await expect(newProjectPage.saveButton).toBeEnabled();
				const projectInfoPage = await newProjectPage.save();

				await expect(projectInfoPage.refreshFeatureButton).toBeDisabled();
				newProject.id = projectInfoPage.projectId;

				projectPage = await overviewPage.lightHousePage.goToProjects();
				await projectPage.search(newProject.name);
				const projectLink = await projectPage.getProjectLink(newProject);
				await expect(projectLink).toBeVisible();
			});
		},
	);
}

for (const teamConfiguration of newTeamConfigurations) {
	testWithUpdatedTeams(teamConfiguration.involvedTeams)(
		`should allow to create a new project with a new Work Tracking System ${teamConfiguration.name}`,
		async ({ testData, overviewPage, request }) => {
			test.fail(
				testData.projects.length < 1,
				"Expected to have projects initiatilized to prevent tutorial page from being displayed",
			);

			const projectsPage = await overviewPage.lightHousePage.goToProjects();
			let newProjectPage = await projectsPage.addNewProject();

			await test.step("Add Valid Configuration for new project", async () => {
				await newProjectPage.setName(
					`My New ${teamConfiguration.name} Project`,
				);
				await newProjectPage.setWorkItemQuery(
					teamConfiguration.projectConfiguration.validQuery,
				);

				for (const teamIndex of teamConfiguration.projectConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newProjectPage.selectTeam(team.name);
				}

				await newProjectPage.resetStates(
					{
						toDo: ["New", "Proposed", "To Do"],
						doing: ["Active", "Resolved", "In Progress", "Committed"],
						done: ["Done", "Closed"],
					},
					teamConfiguration.projectConfiguration.validStates,
				);
			});

			const newWorkTrackingSystemConnectionName = generateRandomName();
			await test.step("Add new Work Tracking System", async () => {
				let newWorkTrackingSystemDialog =
					await newProjectPage.addNewWorkTrackingSystem();

				newProjectPage = await newWorkTrackingSystemDialog.cancel();

				// No New Work Tracking System
				await expect(newProjectPage.validateButton).toBeDisabled();

				newWorkTrackingSystemDialog =
					await newProjectPage.addNewWorkTrackingSystem();
				await newWorkTrackingSystemDialog.selectWorkTrackingSystem(
					teamConfiguration.name,
				);

				for (const option of teamConfiguration.workTrackingSystemOptions) {
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

				newProjectPage = await newWorkTrackingSystemDialog.create();

				await expect(newProjectPage.validateButton).toBeEnabled();
				await expect(newProjectPage.saveButton).toBeDisabled();

				await newProjectPage.validate();
				await expect(newProjectPage.validateButton).toBeEnabled();
				await expect(newProjectPage.saveButton).toBeEnabled();
			});

			await deleteWorkTrackingSystemConnectionByName(
				request,
				newWorkTrackingSystemConnectionName,
			);
		},
	);
}
