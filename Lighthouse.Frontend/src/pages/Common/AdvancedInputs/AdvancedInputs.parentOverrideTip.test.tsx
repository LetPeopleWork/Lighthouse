import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	expectLearnMoreToOpen,
	openTheTip,
	PORTFOLIO_PARENT_OVERRIDE_DOCS,
	RenamedTerms,
	renderWithTerms,
	TEAM_PARENT_OVERRIDE_DOCS,
	theAboutButtonOf,
} from "../../../tests/FieldTipsTestKit";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

/**
 * Sam, config-admin, is about to pick a Parent Override Field on a Team's or a Portfolio's settings. Beside the
 * field sits an info button, "About Parent Override Field". Opening it shows that the field reads the link or
 * field on the item that points to its parent, with the tracker's own example, that None keeps the tracker's own
 * parent, and a "Learn More" link opening the matching docs section in a new tab.
 */

const PARENT_OVERRIDE = "Parent Override Field";
const IS_CAUSED_BY = /is caused by/;
const THE_PARENTS_ID = /parent['’]s id/i;

type Owner = "Team" | "Portfolio";

const givenSamOpensTheSettingsOf = async (
	owner: Owner,
	tracker: WorkTrackingSystemType | undefined,
	terms: Record<string, string> = {},
	connection: IWorkTrackingSystemConnection | null = null,
) => {
	const settings: IBaseSettings = {
		...createMockTeamSettings(),
		workTrackingSystem: tracker,
	};
	await renderWithTerms(
		<AdvancedInputsComponent
			settings={settings}
			onSettingsChange={vi.fn()}
			connection={connection}
			parentOverrideDefault={
				owner === "Team"
					? "parentOverrideForTeams"
					: "parentOverrideForPortfolios"
			}
		/>,
		terms,
	);
	fireEvent.click(screen.getByLabelText("toggle"));
};

const whenSamOpensTheTip = () =>
	openTheTip(userEvent.setup(), theAboutButtonOf(PARENT_OVERRIDE));

describe("Story 6352: Parent Override Field says which way round it reads", () => {
	it("on a Jira Team, the tip names the phrase read towards the parent, such as is caused by, and what None does", async () => {
		await givenSamOpensTheSettingsOf("Team", "Jira");

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(IS_CAUSED_BY);
		expect(tip).toHaveTextContent(/None/);
	});

	it("on a Jira Portfolio, the tip gives the same example", async () => {
		await givenSamOpensTheSettingsOf("Portfolio", "Jira");

		expect(await whenSamOpensTheTip()).toHaveTextContent(IS_CAUSED_BY);
	});

	it("on an Azure DevOps Team, the tip asks for the field that holds the parent's id, not a Jira phrase", async () => {
		await givenSamOpensTheSettingsOf("Team", "AzureDevOps");

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(THE_PARENTS_ID);
		expect(tip).not.toHaveTextContent(IS_CAUSED_BY);
	});

	it("once an Azure DevOps connection has loaded, the tip follows it, though the settings still say Jira", async () => {
		await givenSamOpensTheSettingsOf(
			"Team",
			"Jira",
			{},
			new WorkTrackingSystemConnection({
				id: 2,
				name: "Payments Azure DevOps",
				workTrackingSystem: "AzureDevOps",
				options: [],
			}),
		);

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(THE_PARENTS_ID);
		expect(tip).not.toHaveTextContent(IS_CAUSED_BY);
	});

	it.each(["Linear", "Csv", "ServiceNow", undefined] as const)(
		"on a %s Team the tip stays tracker-neutral, without an example",
		async (tracker) => {
			await givenSamOpensTheSettingsOf("Team", tracker);

			const tip = await whenSamOpensTheTip();
			expect(tip).toHaveTextContent(/parent/i);
			expect(tip).not.toHaveTextContent(IS_CAUSED_BY);
			expect(tip).not.toHaveTextContent(THE_PARENTS_ID);
		},
	);

	it("without an example, the tip reads as two sentences: which end the field reads, then what None does", async () => {
		await givenSamOpensTheSettingsOf("Team", "Linear");

		expect(await whenSamOpensTheTip()).toHaveTextContent(
			"Read on each Work Item, this names the link or field that points to its parent. With None, the parent the tracker itself records is used.",
		);
	});

	it("on a Team the tip speaks of the tenant's Ticket, never of a Work Item", async () => {
		await givenSamOpensTheSettingsOf("Team", "Jira", RenamedTerms);

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(/Ticket/);
		expect(tip).not.toHaveTextContent(/Work Item/);
	});

	it("on a Portfolio the tip speaks of the tenant's Bet, never of a Feature", async () => {
		await givenSamOpensTheSettingsOf("Portfolio", "Jira", RenamedTerms);

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(/Bet/);
		expect(tip).not.toHaveTextContent(/Feature/);
	});

	it("on a Team, Learn More opens the Team's Parent Override docs in a new tab", async () => {
		await givenSamOpensTheSettingsOf("Team", "Jira");

		expectLearnMoreToOpen(
			await whenSamOpensTheTip(),
			TEAM_PARENT_OVERRIDE_DOCS,
		);
	});

	it("on a Portfolio, Learn More opens the Portfolio's Parent Override docs in a new tab", async () => {
		await givenSamOpensTheSettingsOf("Portfolio", "Jira");

		expectLearnMoreToOpen(
			await whenSamOpensTheTip(),
			PORTFOLIO_PARENT_OVERRIDE_DOCS,
		);
	});
});
