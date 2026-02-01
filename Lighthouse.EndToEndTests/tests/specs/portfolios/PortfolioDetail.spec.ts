import {
	expect,
	test,
	testWithUpdatedTeams,
} from "../../fixutres/LighthouseFixture";
import { expectDateToBeRecent } from "../../helpers/dates";

const testData = [
	{
		name: "Azure DevOps",
		index: 1,
		involvedTeams: [1],
		expectedFeatures: [
			{
				name: "Instant status monitoring for real-time insights",
				inProgress: false,
				defaultSize: false,
				involvedTeams: [1],
			},
			{
				name: "Intuitive content filtering",
				inProgress: false,
				defaultSize: true,
				involvedTeams: [1],
			},
		],
	},
	{
		name: "Jira",
		index: 2,
		involvedTeams: [2],
		expectedFeatures: [
			{
				name: "Majestic Moments",
				inProgress: false,
				defaultSize: true,
				involvedTeams: [2],
			},
			{
				name: "Astral Affinitiy",
				inProgress: true,
				defaultSize: false,
				involvedTeams: [2],
			},
		],
	},
];

for (const { index, name, involvedTeams, expectedFeatures } of testData) {
	testWithUpdatedTeams(involvedTeams)(
		`should show correct Features for ${name} portfolio on refresh`,
		async ({ testData, overviewPage }) => {
			const portfolio = testData.portfolios[index];

			const portfolioDetailPage = await overviewPage.goToPortfolio(portfolio);

			const involvedTeams: { [key: string]: string[] } = {};

			await test.step("Refresh Features", async () => {
				await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

				await portfolioDetailPage.refreshFeatures();
				await expect(portfolioDetailPage.refreshFeatureButton).toBeDisabled();

				// Wait for update to be done
				await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

				const lastUpdatedDate = await portfolioDetailPage.getLastUpdatedDate();
				expectDateToBeRecent(lastUpdatedDate);
			});

			await test.step("Expected Features were loaded", async () => {
				for (const feature of expectedFeatures) {
					const featureLink = portfolioDetailPage.getFeatureLink(feature.name);
					await expect(featureLink).toBeVisible();

					if (feature.inProgress) {
						const inProgressIcon = portfolioDetailPage.getFeatureInProgressIcon(
							feature.name,
						);
						await expect(inProgressIcon).toBeVisible();
					}

					if (feature.defaultSize) {
						const defaultSizeIcon = portfolioDetailPage.getFeatureIsDefaultSize(
							feature.name,
						);
						await expect(defaultSizeIcon).toBeVisible();
					}

					for (const involvedTeamIndex of feature.involvedTeams) {
						const team = testData.teams[involvedTeamIndex];

						if (!involvedTeams[team.name]) {
							involvedTeams[team.name] = [];
						}

						involvedTeams[team.name].push(feature.name);

						const teamLink = portfolioDetailPage.getTeamLinkForFeature(
							team.name,
							involvedTeams[team.name].length - 1,
						);
						await expect(teamLink).toBeVisible();
					}
				}
			});

			await test.step("Expect Team Detail to List Features", async () => {
				for (const [team, features] of Object.entries(involvedTeams)) {
					const teamsOverviewPage =
						await overviewPage.lightHousePage.goToOverview();

					const teamDetailPage = await teamsOverviewPage.goToTeam(team);

					for (const feature of features) {
						const featureLink = teamDetailPage.getFeatureLink(feature);
						await expect(featureLink).toBeVisible();
					}
				}
			});
		},
	);
}

testWithUpdatedTeams([0])(
	"should properly handle deliveries within a portfolio",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioDetailPage = await overviewPage.goToPortfolio(portfolio);
		await test.step("Refresh Features", async () => {
			await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

			await portfolioDetailPage.refreshFeatures();
			await expect(portfolioDetailPage.refreshFeatureButton).toBeDisabled();

			// Wait for update to be done
			await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

			const lastUpdatedDate = await portfolioDetailPage.getLastUpdatedDate();
			expectDateToBeRecent(lastUpdatedDate);
		});

		let deliveriesPage = await portfolioDetailPage.goToDeliveries();
		const deliveryName = "Next Release";

		await test.step("Create new Delivery", async () => {
			const addDeliveryDialog = await deliveriesPage.addDelivery();

			await expect(addDeliveryDialog.saveButton).toBeDisabled();
			expect(
				await addDeliveryDialog.hasDeliveryNameValidationError(),
			).toBeTruthy();

			await addDeliveryDialog.setDeliveryName(deliveryName);

			await expect(addDeliveryDialog.saveButton).toBeDisabled();
			expect(
				await addDeliveryDialog.hasDeliveryNameValidationError(),
			).toBeFalsy();
			expect(
				await addDeliveryDialog.hasDeliveryDateValidationError(),
			).toBeTruthy();

			const oneWeekFromNow = new Date();
			oneWeekFromNow.setDate(oneWeekFromNow.getDate() + 7);
			const yyyy = oneWeekFromNow.getFullYear();
			const mm = String(oneWeekFromNow.getMonth() + 1).padStart(2, "0");
			const dd = String(oneWeekFromNow.getDate()).padStart(2, "0");
			const futureDate = `${yyyy}-${mm}-${dd}`;

			await addDeliveryDialog.setDeliveryDate(futureDate);

			await expect(addDeliveryDialog.saveButton).toBeDisabled();
			expect(
				await addDeliveryDialog.hasDeliveryDateValidationError(),
			).toBeFalsy();
			expect(
				await addDeliveryDialog.hasAtLeastOneFeatureValidationError(),
			).toBeTruthy();

			await addDeliveryDialog.selectFeatureByIndex(0);
			await addDeliveryDialog.selectFeatureByIndex(1);

			deliveriesPage = await addDeliveryDialog.save();
		});

		const delivery = deliveriesPage.getDeliveryByName(deliveryName);
		await test.step("Verify Delivery is shown in Portfolio Detail", async () => {
			const details = await delivery.getDetails();
			expect(details.name).toBe(deliveryName);
			expect(details.scope).toBe(2);

			await delivery.toggleDetails();

			const featureLikelihoods = await delivery.getFeatureLikelihoods();
			expect(featureLikelihoods.length).toBe(2);
			expect(featureLikelihoods).toContain(details.likelihood);
		});

		await test.step("Rule Based Delivery Modification", async () => {
			const modifyDialog = await delivery.modifyDelivery();

			await expect(modifyDialog.saveButton).toBeEnabled();

			await modifyDialog.switchToRuleBased();

			await expect(modifyDialog.saveButton).toBeDisabled();
			expect(
				await modifyDialog.hasRulesMustBeValidatedValidationError(),
			).toBeTruthy();

			await modifyDialog.addRule();
			await modifyDialog.setRuleField(0, "Type");
			await modifyDialog.setRuleOperator(0, "Equals");
			await modifyDialog.setRuleValue(0, "Epic");

			await modifyDialog.validateRules();

			await expect(modifyDialog.saveButton).toBeEnabled();
			deliveriesPage = await modifyDialog.save();
		});

		await test.step("Delete Delivery from Portfolio Detail", async () => {
			let delivery = deliveriesPage.getDeliveryByName(deliveryName);
			let deleteDialog = await delivery.delete();

			await deleteDialog.cancel();

			delivery = deliveriesPage.getDeliveryByName(deliveryName);
			expect(await delivery.getName()).toBe(deliveryName);

			deleteDialog = await delivery.delete();
			await deleteDialog.delete();

			await expect(
				deliveriesPage.page.getByRole("button", { name: deliveryName }),
			).toHaveCount(0);
		});
	},
);
