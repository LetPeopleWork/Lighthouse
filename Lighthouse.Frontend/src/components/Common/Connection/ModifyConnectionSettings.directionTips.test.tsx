import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import {
	type ConnectionDefaultName,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	DEPENDENCY_FIELD_DOCS,
	expectLearnMoreToOpen,
	openTheTip,
	PORTFOLIO_PARENT_OVERRIDE_DOCS,
	RenamedTerms,
	renderWithTerms,
	TEAM_PARENT_OVERRIDE_DOCS,
	theAboutButtonOf,
	theAboutButtonsIn,
	theInfoIconsIn,
} from "../../../tests/FieldTipsTestKit";
import ModifyConnectionSettings from "./ModifyConnectionSettings";

/**
 * Priya, System Admin, sets the defaults every Team and Portfolio on "Jira DC – Payments" follows. Beside the
 * Parent Override Field and the Dependency Field rows sits the same info button the Team and Portfolio settings
 * show, with the tracker's own example and a "Learn More" to the docs; the other default rows carry none.
 */

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
	}),
}));

const JiraDcPayments = "Jira DC – Payments";
const PARENT_OVERRIDE = "Parent Override Field";
const DEPENDENCY = "Dependency Field";
const IS_CAUSED_BY = /is caused by/;
const IS_BLOCKED_BY = /is blocked by/;
const PREDECESSOR = /Predecessor/;

const EveryDefault: ConnectionDefaultName[] = [
	"parentOverrideForTeams",
	"estimationForTeams",
	"parentOverrideForPortfolios",
	"dependencyFieldForPortfolios",
	"sizeEstimateFieldForPortfolios",
	"featureOwnerFieldForPortfolios",
	"estimationForPortfolios",
];

const WhatEachTrackerOffers: Partial<
	Record<WorkTrackingSystemType, ConnectionDefaultName[]>
> = {
	Jira: EveryDefault,
	AzureDevOps: EveryDefault,
	Csv: [
		"estimationForTeams",
		"featureOwnerFieldForPortfolios",
		"estimationForPortfolios",
	],
};

const aConnectionOn = (tracker: WorkTrackingSystemType) =>
	new WorkTrackingSystemConnection({
		id: 7,
		name: JiraDcPayments,
		workTrackingSystem: tracker,
		authenticationMethodKey: "none",
		availableAuthenticationMethods: [
			{ key: "none", displayName: "None", options: [] },
		],
		options: [],
		additionalFieldDefinitions: [
			{ id: 11, displayName: "Caused by", reference: "customfield_10500" },
		],
		usedBy: { teams: 0, portfolios: 0 },
		defaults: {},
		supportedDefaults: WhatEachTrackerOffers[tracker] ?? [],
	});

const givenPriyaOpensAConnectionOn = async (
	tracker: WorkTrackingSystemType,
	terms: Record<string, string> = {},
) => {
	const connection = aConnectionOn(tracker);
	await renderWithTerms(
		<MemoryRouter>
			<ModifyConnectionSettings
				title="Update Connection"
				getSupportedSystems={vi.fn().mockResolvedValue([connection])}
				getConnectionSettings={vi.fn().mockResolvedValue(connection)}
				saveConnectionSettings={vi.fn()}
				validateConnectionSettings={vi.fn()}
			/>
		</MemoryRouter>,
		terms,
	);
	await screen.findByDisplayValue(JiraDcPayments);
};

const theDefaultsSection = () =>
	screen.getByRole("region", { name: /^Defaults for / });

const theGroup = (name: string) =>
	within(theDefaultsSection()).getByRole("group", { name });

const whenPriyaOpensTheTipOf = (field: string, group: string) =>
	openTheTip(userEvent.setup(), theAboutButtonOf(field, theGroup(group)));

describe("Story 6352: the connection's defaults say which way round they read", () => {
	// @driving_port @contract-shape:pure-function
	it("on a Jira connection, only the Parent Override and Dependency rows carry an About button", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expect(theAboutButtonsIn(theGroup("Teams"))).toEqual([
			`About ${PARENT_OVERRIDE}`,
		]);
		expect(theAboutButtonsIn(theGroup("Portfolios"))).toEqual([
			`About ${PARENT_OVERRIDE}`,
			`About ${DEPENDENCY}`,
		]);
	});

	// @edge @contract-shape:unbounded-preservation
	it("the Estimation, Size Estimate and Feature Owner rows carry no info icon", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expect(theInfoIconsIn(theGroup("Teams"))).toHaveLength(1);
		expect(theInfoIconsIn(theGroup("Portfolios"))).toHaveLength(2);
		for (const row of [
			"Estimation Field",
			"Size Estimate Field",
			"Feature Owner Field",
		]) {
			expect(
				within(theDefaultsSection()).queryByRole("button", {
					name: `About ${row}`,
				}),
			).not.toBeInTheDocument();
		}
	});

	// @contract-shape:pure-function
	it("on a Jira connection, the Teams' Parent Override tip gives Jira's example", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expect(
			await whenPriyaOpensTheTipOf(PARENT_OVERRIDE, "Teams"),
		).toHaveTextContent(IS_CAUSED_BY);
	});

	// @contract-shape:pure-function
	it("on a Jira connection, the Dependency tip asks for is blocked by", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expect(
			await whenPriyaOpensTheTipOf(DEPENDENCY, "Portfolios"),
		).toHaveTextContent(IS_BLOCKED_BY);
	});

	// @contract-shape:pure-function
	it("on an Azure DevOps connection, the Dependency tip names Predecessor and no Jira phrase", async () => {
		await givenPriyaOpensAConnectionOn("AzureDevOps");

		const tip = await whenPriyaOpensTheTipOf(DEPENDENCY, "Portfolios");
		expect(tip).toHaveTextContent(PREDECESSOR);
		expect(tip).not.toHaveTextContent(IS_BLOCKED_BY);
	});

	// @edge @contract-shape:pure-function
	it("a tenant's own words reach the tips: the Dependency tip speaks of a Bet, never of a Feature", async () => {
		await givenPriyaOpensAConnectionOn("Jira", RenamedTerms);

		const tip = await whenPriyaOpensTheTipOf(DEPENDENCY, "Programs");
		expect(tip).toHaveTextContent(/Bet/);
		expect(tip).not.toHaveTextContent(/Feature/);
	});

	// @contract-shape:pure-function
	it("the Teams' Parent Override tip leads to the Team docs", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expectLearnMoreToOpen(
			await whenPriyaOpensTheTipOf(PARENT_OVERRIDE, "Teams"),
			TEAM_PARENT_OVERRIDE_DOCS,
		);
	});

	// @contract-shape:pure-function
	it("the Portfolios' Parent Override tip leads to the Portfolio docs", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expectLearnMoreToOpen(
			await whenPriyaOpensTheTipOf(PARENT_OVERRIDE, "Portfolios"),
			PORTFOLIO_PARENT_OVERRIDE_DOCS,
		);
	});

	// @contract-shape:pure-function
	it("the Dependency tip leads to the Dependency Field docs", async () => {
		await givenPriyaOpensAConnectionOn("Jira");

		expectLearnMoreToOpen(
			await whenPriyaOpensTheTipOf(DEPENDENCY, "Portfolios"),
			DEPENDENCY_FIELD_DOCS,
		);
	});
});

describe("Story 6352: defaults that read no link", () => {
	// @error @active-guard @contract-shape:unbounded-preservation
	it("a CSV connection's defaults, Estimation and Feature Owner, carry no info button", async () => {
		await givenPriyaOpensAConnectionOn("Csv");

		expect(theInfoIconsIn(theDefaultsSection())).toHaveLength(0);
		expect(theAboutButtonsIn(theDefaultsSection())).toEqual([]);
	});
});
