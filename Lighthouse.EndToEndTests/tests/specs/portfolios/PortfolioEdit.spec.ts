import { TestConfig } from "../../../playwright.config";
import {
	expect,
	test,
	testWithData,
	testWithUpdatedTeams,
} from "../../fixutres/LighthouseFixture";
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

			let sizeEstimateField =
				await portfolioEditPage.getSelectedSizeEstimateField();
			expect(sizeEstimateField).toBe("​");

			const potentialSizeEstimateFields =
				await portfolioEditPage.getPotentialSizeEstimateFields();
			expect(potentialSizeEstimateFields).toContain("None");
			expect(potentialSizeEstimateFields).toContain("Area Path");

			await portfolioEditPage.selectSizeEstimateField("Area Path");
			sizeEstimateField =
				await portfolioEditPage.getSelectedSizeEstimateField();
			expect(sizeEstimateField).toBe("Area Path");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.selectSizeEstimateField("None");
			sizeEstimateField =
				await portfolioEditPage.getSelectedSizeEstimateField();
			expect(sizeEstimateField).toBe("​");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.addSizeOverrideState("New");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.removeSizeOverrideState("New");
			await expect(portfolioEditPage.validateButton).toBeEnabled();
		});

		await test.step("Ownership Setting Configuration should not be mandatory", async () => {
			await portfolioEditPage.toggleOwnershipSettings();

			let featureOwnerField =
				await portfolioEditPage.getSelectedFeatureOwnerField();
			expect(featureOwnerField).toBe("​");

			const potentialFeatureOwnerFields =
				await portfolioEditPage.getPotentialFeatureOwnerFields();
			expect(potentialFeatureOwnerFields).toContain("None");
			expect(potentialFeatureOwnerFields).toContain("Area Path");

			await portfolioEditPage.selectFeatureOwnerField("Area Path");
			featureOwnerField =
				await portfolioEditPage.getSelectedFeatureOwnerField();
			expect(featureOwnerField).toBe("Area Path");
			await expect(portfolioEditPage.validateButton).toBeEnabled();

			await portfolioEditPage.selectFeatureOwnerField("None");
			featureOwnerField =
				await portfolioEditPage.getSelectedFeatureOwnerField();
			expect(featureOwnerField).toBe("​");
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

		await test.step("All Available Teams and None can be selected as Owner", async () => {
			const availableOptions =
				await portfolioEditPage.getPotentialOwningTeams();

			expect(availableOptions).toContain("None");
			expect(availableOptions).toContain(team1.name);
			expect(availableOptions).toContain(team2.name);
			expect(availableOptions).toContain(team3.name);
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
					[],
					teamConfiguration.portfolioConfiguration.validWorkItemTypes,
				);

				// Expect Validation to be disabled because mandatory config is still missing
				await expect(newPortfolioPage.validateButton).toBeDisabled();
			});

			await test.step("Add State Configuration", async () => {
				await newPortfolioPage.resetStates(
					{
						toDo: [],
						doing: [],
						done: [],
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

const wizardConfigurations = [
	{
		name: "Jira",
		displayName: "Jira",
		involvedTeams: newTeamConfigurations[0].involvedTeams,
		workTrackingSystemOptions:
			newTeamConfigurations[0].workTrackingSystemOptions,
		portfolioConfiguration: newTeamConfigurations[0].portfolioConfiguration,
		boardName: "Epics",
		dataRetrievalKey: "JQL Query",
		expectedQuery:
			"project = LIGHTHOUSE AND type = Epic AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)",
		expectedWorkItemTypes: ["Epic"],
		expectedToDoStates: ["Ideas", "Evaluation", "Next"],
		expectedDoingStates: ["Ready to Release", "Ongoing"],
		expectedDoneStates: ["Done"],
	},
	{
		name: "AzureDevOps",
		displayName: "Azure DevOps",
		invovolvedTeams: newTeamConfigurations[1].involvedTeams,
		workTrackingSystemOptions:
			newTeamConfigurations[1].workTrackingSystemOptions,
		portfolioConfiguration: newTeamConfigurations[1].portfolioConfiguration,
		boardName: "Lighthouse - Epics",
		dataRetrievalKey: "WIQL Query",
		expectedQuery: String.raw`[System.AreaPath] = "Lighthouse" OR [System.AreaPath] = "Lighthouse\Something Below"`,
		expectedWorkItemTypes: ["Epic"],
		expectedToDoStates: ["New"],
		expectedDoingStates: ["Planned", "Active", "Resolved"],
		expectedDoneStates: ["Closed"],
	},
];

for (const wizardConfiguration of wizardConfigurations) {
	testWithData(
		`should allow to create a new Portfolio through a ${wizardConfiguration.displayName} Wizard`,
		async ({ testData, overviewPage }) => {
			test.fail(
				testData.portfolios.length < 1,
				"Expected to have portfolio initiatilized to prevent tutorial page from being displayed",
			);

			let newPortfolioPage = await overviewPage.addNewPortfolio();

			await test.step("Add Valid Configuration for new portfolio", async () => {
				await newPortfolioPage.setName(
					`My New ${wizardConfiguration.name} Portfolio`,
				);
			});

			await test.step("Add Work Tracking System", async () => {
				const newWorkTrackingSystemConnectionName = generateRandomName();

				const newWorkTrackingSystemDialog =
					await newPortfolioPage.addNewWorkTrackingSystem();

				await newWorkTrackingSystemDialog.selectWorkTrackingSystem(
					wizardConfiguration.name,
				);

				for (const option of wizardConfiguration.workTrackingSystemOptions) {
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
			});

			await test.step(`Use ${wizardConfiguration.displayName} Wizard to Select Board`, async () => {
				const boardWizard = await newPortfolioPage.openBoardWizard(
					wizardConfiguration.displayName,
				);

				expect(await boardWizard.confirmButton.isEnabled()).toBeFalsy();

				await boardWizard.selectBoardByName(wizardConfiguration.boardName);

				await expect(boardWizard.boardInformationPanel).toBeVisible();
				expect(await boardWizard.confirmButton.isEnabled()).toBeTruthy();

				newPortfolioPage = await boardWizard.confirm();
			});

			await test.step("Validate Settings", async () => {
				expect(newPortfolioPage.validateButton).toBeEnabled();
				expect(newPortfolioPage.saveButton).toBeDisabled();

				await newPortfolioPage.validate();

				await expect(newPortfolioPage.validateButton).toBeEnabled();
				await expect(newPortfolioPage.saveButton).toBeEnabled();

				expect(
					await newPortfolioPage.getDataRetrievalValue(
						wizardConfiguration.dataRetrievalKey,
					),
				).toBe(wizardConfiguration.expectedQuery);

				const workItemTypes = await newPortfolioPage.getWorkItemTypes();
				expect(workItemTypes).toEqual(
					wizardConfiguration.expectedWorkItemTypes,
				);

				const toDoStates = await newPortfolioPage.getToDoStates();
				expect(toDoStates).toEqual(wizardConfiguration.expectedToDoStates);

				const doingStates = await newPortfolioPage.getDoingStates();
				expect(doingStates).toEqual(wizardConfiguration.expectedDoingStates);

				const doneStates = await newPortfolioPage.getDoneStates();
				expect(doneStates).toEqual(wizardConfiguration.expectedDoneStates);
			});
		},
	);
}
