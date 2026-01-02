import { TestConfig } from "../../../playwright.config";
import {
	expect,
	test,
	testWithData,
	testWithUpdatedTeams,
} from "../../fixutres/LighthouseFixture";
import { deleteWorkTrackingSystemConnectionByName } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";

const newPortfolioConfigurations = [
	{ name: "Azure DevOps", index: 0, involvedTeams: [1] },
	{ name: "Jira", index: 2, involvedTeams: [2] },
];

for (const { name, involvedTeams, index } of newPortfolioConfigurations) {
	testWithUpdatedTeams(involvedTeams)(
		`should allow save after validate when editing existing ${name} portfolio`,
		async ({ testData, overviewPage }) => {
			const portfolio = testData.portfolios[index];

			const portfolioEditPage = await overviewPage.editPortfolio(portfolio);

			await expect(portfolioEditPage.validateButton).toBeEnabled();
			await expect(portfolioEditPage.saveButton).toBeDisabled();

			await portfolioEditPage.validate();

			await expect(portfolioEditPage.validateButton).toBeEnabled();
			await expect(portfolioEditPage.saveButton).toBeEnabled();
		},
	);
}

testWithData(
	"should disable validate button if not all mandatory fields are set",
	async ({ testData, overviewPage }) => {
		const portfolio = testData.portfolios[0];
		const team = testData.teams[0];

		const portfolioEditPage = await overviewPage.editPortfolio(portfolio);

		await expect(portfolioEditPage.validateButton).toBeEnabled();

		await test.step("portfolio Name should be mandatory", async () => {
			const oldName = portfolio.name;
			await portfolioEditPage.setName("");
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.setName(oldName);
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Work Item Query should be mandatory", async () => {
			await portfolioEditPage.setDataRetrievalValue("", "WIQL Query");
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.setDataRetrievalValue(
				'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"',
				"WIQL Query",
			);
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Work Item Types should be mandatory and more than 1", async () => {
			await portfolioEditPage.removeWorkItemType("Epic");
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.addWorkItemType("Epic");
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Involved Teams should be mandatory and more than 1", async () => {
			await portfolioEditPage.deselectTeam(team.name);
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.selectTeam(team.name);
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Each state category should have at least one state", async () => {
			await portfolioEditPage.removeState("New");
			await portfolioEditPage.removeState("Planned");
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.addState("Backlog", "To Do");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.removeState("Active");
			await portfolioEditPage.removeState("Resolved");
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.addState("In Progress", "Doing");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.removeState("Closed");
			await expect(portfolioEditPage.validateButton).toBeDisabled();

			await portfolioEditPage.addState("Done", "Done");
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Default Feature Size Configuration should not be mandatory", async () => {
			await portfolioEditPage.toggleDefaultFeatureSizeConfiguration();

			await portfolioEditPage.setSizeEstimateField("CUSTOMFIELD_1337");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.setSizeEstimateField("");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.addSizeOverrideState("New");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.removeSizeOverrideState("New");
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Ownership Setting Configuration should not be mandatory", async () => {
			await portfolioEditPage.toggleOwnershipSettings();

			await portfolioEditPage.setFeatureOwnerField("System.AreaPath");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.setFeatureOwnerField("");
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});
	},
);

testWithData(
	"should handle Owning Team correctly",
	async ({ testData, overviewPage }) => {
		const portfolio = testData.portfolios[0];
		const [team1, team2, team3] = testData.teams;

		const portfolioEditPage = await overviewPage.editPortfolio(portfolio);

		await test.step("No Team is selected as Owner by Default", async () => {
			await portfolioEditPage.toggleOwnershipSettings();

			const owningTeamValue = await portfolioEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe("​");
		});

		await test.step("Only Involved Team and None can be selected as Owner", async () => {
			const availableOptions =
				await portfolioEditPage.getPotentialOwningTeams();

			expect(availableOptions).toContain("None");
			expect(availableOptions).toContain(team1.name);
			expect(availableOptions).not.toContain(team2.name);
			expect(availableOptions).not.toContain(team3.name);
		});

		await test.step("Including more teams should allow to pick them as owners", async () => {
			await portfolioEditPage.selectTeam(team2.name);
			await portfolioEditPage.selectTeam(team3.name);

			const availableOptions =
				await portfolioEditPage.getPotentialOwningTeams();
			expect(availableOptions).toContain("None");
			expect(availableOptions).toContain(team1.name);
			expect(availableOptions).toContain(team2.name);
			expect(availableOptions).toContain(team3.name);
		});

		await test.step("Removing Owning Team from Involved Teams will reset Owning Team", async () => {
			await portfolioEditPage.selectOwningTeam(team1.name);

			let owningTeamValue = await portfolioEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe(team1.name);

			await portfolioEditPage.deselectTeam(team1.name);
			owningTeamValue = await portfolioEditPage.getSelectedOwningTeam();
			expect(owningTeamValue).toBe("​");
		});
	},
);

const newTeamConfigurations = [
	{
		name: "Jira",
		workTrackingSystemIndex: 1,
		dataRetrievalKey: "JQL Query",
		involvedTeams: [2],
		portfolioConfiguration: {
			validWorkItemTypes: ["Epic"],
			invalidWorkItemTypes: ["Feature"],
			validStates: { toDo: ["To Do"], doing: ["In Progress"], done: ["Done"] },
			invalidStates: { toDo: ["New"], doing: ["Active"], done: ["Closed"] },
			validQuery: 'project = "LGHTHSDMO" AND fixVersion = "Oberon Initiative"',
			invalidQuery: 'project = "LGHTHSDMO" AND labels = "Lagunitas"',
			involvedTeams: [2],
		},
		workTrackingSystemOptions: [
			{ field: "Jira URL", value: "https://letpeoplework.atlassian.net" },
			{
				field: "Username (Email)",
				value: "atlassian.pushchair@huser-berta.com",
			},
			{ field: "API Token", value: TestConfig.JiraToken },
		],
	},
	{
		name: "AzureDevOps",
		workTrackingSystemIndex: 0,
		dataRetrievalKey: "WIQL Query",
		involvedTeams: [1],
		portfolioConfiguration: {
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
			invalidQuery: String.raw`[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\Binary Blazers"`,
			involvedTeams: [1],
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

for (const teamConfiguration of newTeamConfigurations) {
	testWithUpdatedTeams(teamConfiguration.involvedTeams)(
		`should allow to create a portfolio team for ${teamConfiguration.name}`,
		async ({ testData, overviewPage }) => {
			const newPortfolioPage = await overviewPage.addNewPortfolio();

			const newPortfolio = {
				id: 0,
				name: `My New ${teamConfiguration.name} Portfolio`,
			};

			await test.step("Add general configuration", async () => {
				await newPortfolioPage.setName(newPortfolio.name);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newPortfolioPage.validateButton).toBeDisabled();
			});

			await test.step("Add Work Item Type Configuration", async () => {
				await newPortfolioPage.resetWorkItemTypes(
					["Epic"],
					teamConfiguration.portfolioConfiguration.validWorkItemTypes,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newPortfolioPage.validateButton).toBeDisabled();
			});

			await test.step("Add Involved Teams Configuration", async () => {
				for (const teamIndex of teamConfiguration.portfolioConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newPortfolioPage.selectTeam(team.name);
				}

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newPortfolioPage.validateButton).toBeDisabled();
			});

			await test.step("Add State Configuration", async () => {
				await newPortfolioPage.resetStates(
					{
						toDo: ["New", "Proposed", "To Do"],
						doing: ["Active", "Resolved", "In Progress", "Committed"],
						done: ["Done", "Closed"],
					},
					teamConfiguration.portfolioConfiguration.validStates,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newPortfolioPage.validateButton).toBeDisabled();
			});

			await test.step("Add Tags", async () => {
				await newPortfolioPage.addTag("Important");
				await newPortfolioPage.addTag(teamConfiguration.name);
			});

			await test.step("Select Work Tracking System", async () => {
				const workTrackingSystem =
					testData.connections[teamConfiguration.workTrackingSystemIndex];
				await newPortfolioPage.selectWorkTrackingSystem(
					workTrackingSystem.name,
				);

				await newPortfolioPage.setDataRetrievalValue(
					teamConfiguration.portfolioConfiguration.validQuery,
					teamConfiguration.dataRetrievalKey,
				);

				// Now we have all default configuration set
				await expect(newPortfolioPage.validateButton).toBeEnabled();
			});

			await test.step("Validate Settings", async () => {
				await newPortfolioPage.validate();
				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeEnabled();
			});

			await test.step("Invalidate Work Item Query", async () => {
				await newPortfolioPage.setDataRetrievalValue(
					teamConfiguration.portfolioConfiguration.invalidQuery,
					teamConfiguration.dataRetrievalKey,
				);
				await newPortfolioPage.validate();
				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeDisabled();

				await newPortfolioPage.setDataRetrievalValue(
					teamConfiguration.portfolioConfiguration.validQuery,
					teamConfiguration.dataRetrievalKey,
				);
			});

			await test.step("Invalidate Work Item Types", async () => {
				await newPortfolioPage.resetWorkItemTypes(
					teamConfiguration.portfolioConfiguration.validWorkItemTypes,
					teamConfiguration.portfolioConfiguration.invalidWorkItemTypes,
				);
				await newPortfolioPage.validate();
				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeDisabled();

				await newPortfolioPage.resetWorkItemTypes(
					teamConfiguration.portfolioConfiguration.invalidWorkItemTypes,
					teamConfiguration.portfolioConfiguration.validWorkItemTypes,
				);
			});

			await test.step("Invalidate Involved Teams", async () => {
				for (const teamIndex of teamConfiguration.portfolioConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newPortfolioPage.deselectTeam(team.name);
				}

				await expect(newPortfolioPage.validateButton).toBeDisabled();
				await expect(newPortfolioPage.saveButton).toBeDisabled();

				for (const teamIndex of teamConfiguration.portfolioConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newPortfolioPage.selectTeam(team.name);
				}
			});

			await test.step("Invalidate States", async () => {
				await newPortfolioPage.resetStates(
					teamConfiguration.portfolioConfiguration.validStates,
					teamConfiguration.portfolioConfiguration.invalidStates,
				);
				await newPortfolioPage.validate();
				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeDisabled();

				await newPortfolioPage.resetStates(
					teamConfiguration.portfolioConfiguration.invalidStates,
					teamConfiguration.portfolioConfiguration.validStates,
				);
			});

			await test.step("Create New Portfolio", async () => {
				await newPortfolioPage.validate();
				await expect(newPortfolioPage.saveButton).toBeEnabled();
				const portfolioInfoPage = await newPortfolioPage.save();

				await expect(portfolioInfoPage.refreshFeatureButton).toBeDisabled();
				newPortfolio.id = portfolioInfoPage.portfolioId;

				await overviewPage.lightHousePage.goToOverview();
				await overviewPage.search(newPortfolio.name);
				const portfolioLink = await overviewPage.getPortfolioLink(newPortfolio);
				await expect(portfolioLink).toBeVisible();
			});
		},
	);
}

for (const teamConfiguration of newTeamConfigurations) {
	testWithUpdatedTeams(teamConfiguration.involvedTeams)(
		`should allow to create a new portfolio with a new Work Tracking System ${teamConfiguration.name}`,
		async ({ testData, overviewPage, request }) => {
			test.fail(
				testData.portfolios.length < 1,
				"Expected to have portfolio initiatilized to prevent tutorial page from being displayed",
			);

			let newPortfolioPage = await overviewPage.addNewPortfolio();

			await test.step("Add Valid Configuration for new portfolio", async () => {
				await newPortfolioPage.setName(
					`My New ${teamConfiguration.name} Portfolio`,
				);

				for (const teamIndex of teamConfiguration.portfolioConfiguration
					.involvedTeams) {
					const team = testData.teams[teamIndex];

					await newPortfolioPage.selectTeam(team.name);
				}

				await newPortfolioPage.resetStates(
					{
						toDo: ["New", "Proposed", "To Do"],
						doing: ["Active", "Resolved", "In Progress", "Committed"],
						done: ["Done", "Closed"],
					},
					teamConfiguration.portfolioConfiguration.validStates,
				);
			});

			const newWorkTrackingSystemConnectionName = generateRandomName();
			await test.step("Add new Work Tracking System", async () => {
				let newWorkTrackingSystemDialog =
					await newPortfolioPage.addNewWorkTrackingSystem();

				newPortfolioPage = await newWorkTrackingSystemDialog.cancel();

				// No New Work Tracking System
				await expect(newPortfolioPage.validateButton).toBeDisabled();

				newWorkTrackingSystemDialog =
					await newPortfolioPage.addNewWorkTrackingSystem();
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

				newPortfolioPage = await newWorkTrackingSystemDialog.create();

				await newPortfolioPage.setDataRetrievalValue(
					teamConfiguration.portfolioConfiguration.validQuery,
					teamConfiguration.dataRetrievalKey,
				);

				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeDisabled();

				await newPortfolioPage.validate();
				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeEnabled();
			});

			await deleteWorkTrackingSystemConnectionByName(
				request,
				newWorkTrackingSystemConnectionName,
			);
		},
	);
}
