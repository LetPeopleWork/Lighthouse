import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";
import { expectDateToBeRecent } from "../../helpers/dates";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithPortfolio = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

testWithPortfolio(
	"should properly handle deliveries within a portfolio",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioDetailPage = await overviewPage.goToPortfolio(
			portfolio.name,
		);
		await test.step("Refresh Features", async () => {
			await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();

			await portfolioDetailPage.refreshFeatures();
			await expect(portfolioDetailPage.refreshFeatureButton).toBeDisabled();

			await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled({
				timeout: 90_000,
			});

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
			// Story #5587 (ADR-113): the delivery number is the joint across every feature, so it is
			// never ABOVE any of its rows rather than equal to one of them — equality is permitted but
			// is no longer the rule (D5). One point of slack absorbs the header and the row chips
			// rounding independently.
			expect(details.likelihood).not.toBeNull();
			expect(details.likelihood as number).toBeLessThanOrEqual(
				Math.min(...featureLikelihoods) + 1,
			);
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

		await test.step("Archive Delivery and bring it back", async () => {
			const delivery = deliveriesPage.getDeliveryByName(deliveryName);

			// Asking and then thinking better of it must leave the Delivery where it was.
			const cancelled = await delivery.archive();
			await cancelled.cancel();
			await expect(
				deliveriesPage.getDeliveryByName(deliveryName).container,
			).toBeVisible();

			const confirmed = await deliveriesPage
				.getDeliveryByName(deliveryName)
				.archive();
			await confirmed.archive();

			await expect(
				deliveriesPage.getDeliveryByName(deliveryName).container,
			).toHaveCount(0);

			await deliveriesPage.openArchivedSection();

			const archived = deliveriesPage.getArchivedDeliveryByName(deliveryName);
			await expect(archived.container).toBeVisible();
			expect(await archived.getArchivedOn()).toContain("Archived:");

			await archived.unarchive();

			await expect(
				deliveriesPage.getDeliveryByName(deliveryName).container,
			).toBeVisible();
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
