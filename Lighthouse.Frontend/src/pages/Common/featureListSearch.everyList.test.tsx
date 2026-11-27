import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../models/Delivery";
import type { IFeature } from "../../models/Feature";
import {
	aFeatureServiceAnswering,
	aList,
	aRow,
	Atlas,
	Borealis,
	NorthwindPlatform,
	renderAFeatureList,
	stubTheScreenSizeTheGridAsksFor,
	TeamBorealis,
} from "../../tests/FeatureListTestKit";
import { featureColumns } from "../Portfolios/Detail/Components/DeliveryGrid/deliveryFeatureRow";
import WorkItemsTab from "../Portfolios/Detail/Components/DeliveryGrid/WorkItemsTab";
import PortfolioFeatureList from "../Portfolios/Detail/PortfolioFeatureList";
import TeamFeatureList from "../Teams/Detail/TeamFeatureList";

/**
 * A person finds a Feature by typing part of its reference ID into the same search box above every full
 * Feature list: the Portfolio Features tab, the Team Features tab and the Delivery Work Items tab.
 */

const CHECKOUT = "PROJ-12: Checkout";
const PAYMENTS = "PROJ-20: Payments";

const twoFeatures = (): IFeature[] => [
	aRow(11, "PROJ-12", "Checkout"),
	aRow(12, "PROJ-20", "Payments"),
];

const theFeatureService = (features: IFeature[]) =>
	aFeatureServiceAnswering(Promise.resolve(aList(features)));

const theBox = () => screen.findByRole("textbox", { name: "Search Features" });

const featuresShown = () =>
	[...document.querySelectorAll('[role="gridcell"][data-field="name"]')].map(
		(cell) => cell.textContent ?? "",
	);

const whenTheyType = async (text: string) => {
	await userEvent.type(await theBox(), text);
};

const thenOnlyTheseAreListed = async (names: string[]) => {
	await waitFor(() =>
		expect(
			featuresShown(),
			`the list must show exactly ${JSON.stringify(names)} for what was typed; this list must offer the same search box as every other Feature list and match what each column shows`,
		).toEqual(names),
	);
};

const givenTheDeliveryWorkItemsTab = async (features: IFeature[]) => {
	const delivery = new Delivery();
	delivery.id = 7;
	delivery.name = "Q3 Launch";
	delivery.featureLikelihoods = [];
	await renderAFeatureList(
		<WorkItemsTab
			isLoadingFeatures={false}
			features={features}
			columns={featureColumns({
				featureTerm: "Feature",
				delivery,
				teams: [Atlas, Borealis],
				onShowDetails: vi.fn(),
			})}
			deliveryId={delivery.id}
			featuresTerm="Features"
			deliveryTerm="Delivery"
			exportFileName="q3-launch"
			exportTable={vi.fn()}
		/>,
		{ featureService: theFeatureService([]) },
	);
	await screen.findByText(new RegExp(CHECKOUT));
};

const givenTheList = async (ui: ReactElement, features: IFeature[]) => {
	await renderAFeatureList(ui, { featureService: theFeatureService(features) });
	await screen.findByText(new RegExp(CHECKOUT));
};

beforeEach(() => {
	stubTheScreenSizeTheGridAsksFor();
	localStorage.clear();
});

describe("Feature list search on every full list", () => {
	it("finds a Feature by its reference ID on the Portfolio Features tab", async () => {
		await givenTheList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			twoFeatures(),
		);

		await whenTheyType("proj-12");

		await thenOnlyTheseAreListed([CHECKOUT]);
	});

	it("finds a Feature by its reference ID on the Team Features tab", async () => {
		await givenTheList(<TeamFeatureList team={TeamBorealis} />, twoFeatures());

		await whenTheyType("proj-12");

		await thenOnlyTheseAreListed([CHECKOUT]);
	});

	it("finds a Feature by the Portfolio it belongs to on the Team Features tab", async () => {
		const inPlatform = aRow(11, "PROJ-12", "Checkout");
		inPlatform.projects = [{ id: 2, name: "Northwind Platform" }];
		const inMobile = aRow(12, "PROJ-20", "Payments");
		inMobile.projects = [{ id: 5, name: "Northwind Mobile" }];
		await givenTheList(<TeamFeatureList team={TeamBorealis} />, [
			inPlatform,
			inMobile,
		]);

		await whenTheyType("mobile");

		await thenOnlyTheseAreListed([PAYMENTS]);
	});

	it("finds a Feature by its reference ID on the Delivery Work Items tab", async () => {
		await givenTheDeliveryWorkItemsTab(twoFeatures());

		await whenTheyType("proj-12");

		await thenOnlyTheseAreListed([CHECKOUT]);
	});

	it("finds the Features a Team works on by typing the Team name on the Delivery Work Items tab", async () => {
		const atlasWork = aRow(11, "PROJ-12", "Checkout");
		const borealisWork = aRow(12, "PROJ-20", "Payments");
		Object.assign(atlasWork, { totalWork: { 1: 8 }, remainingWork: { 1: 5 } });
		Object.assign(borealisWork, {
			totalWork: { 2: 8 },
			remainingWork: { 2: 5 },
		});
		await givenTheDeliveryWorkItemsTab([atlasWork, borealisWork]);

		await whenTheyType("atlas");

		await thenOnlyTheseAreListed([CHECKOUT]);
	});
});
