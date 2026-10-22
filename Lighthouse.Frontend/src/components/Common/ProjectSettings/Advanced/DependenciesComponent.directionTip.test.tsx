import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { WorkTrackingSystemType } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	DEPENDENCY_FIELD_DOCS,
	expectLearnMoreToOpen,
	openTheTip,
	RenamedTerms,
	renderWithTerms,
	theAboutButtonOf,
	theOpenTip,
} from "../../../../tests/FieldTipsTestKit";
import { createMockProjectSettings } from "../../../../tests/TestDataProvider";
import DependenciesComponent from "./DependenciesComponent";

/**
 * Sam, config-admin, is about to name a Dependency Field on a Portfolio's settings. Beside the field sits an info
 * button, "About Dependency Field". Opening it, by mouse or keyboard, shows that the field is read on the item that
 * waits, the tracker's own example of that end of the link, that None reads the tracker's own links while a chosen
 * field replaces them, and a "Learn More" link to the Dependency Field docs. Choosing a field saves exactly as before.
 */

const DEPENDENCY = "Dependency Field";
const IS_BLOCKED_BY = /is blocked by/;
const NOT_BLOCKS = /not .blocks./;
const PREDECESSOR = /Predecessor/;
const THE_REVERSE_END = /Reverse/;

interface HeldSettings {
	current: IPortfolioSettings;
}

/** Holds the Portfolio's settings and applies every change, as the edit page does, so a test reads what is left. */
const PortfolioSettingsInState = ({ held }: { held: HeldSettings }) => {
	const [settings, setSettings] = useState(held.current);
	held.current = settings;
	return (
		<DependenciesComponent
			projectSettings={settings}
			onProjectSettingsChange={(key, value) =>
				setSettings((previous) => ({ ...previous, [key]: value }))
			}
			additionalFieldDefinitions={[
				{ id: 1, displayName: "Waits On", reference: "Custom.WaitsOn" },
			]}
		/>
	);
};

const givenSamOpensAPortfolioOn = async (
	tracker: WorkTrackingSystemType | undefined,
	terms: Record<string, string> = {},
) => {
	const held: HeldSettings = {
		current: { ...createMockProjectSettings(), workTrackingSystem: tracker },
	};
	await renderWithTerms(<PortfolioSettingsInState held={held} />, terms);
	fireEvent.click(screen.getByLabelText("toggle"));
	return held;
};

const theDependencyButton = () => theAboutButtonOf(DEPENDENCY);

const whenSamOpensTheTip = () =>
	openTheTip(userEvent.setup(), theDependencyButton());

describe("Story 6352 slice 02: Dependency Field says which end of a link it reads", () => {
	// @driving_port @contract-shape:pure-function
	it("on Jira, the tip asks for the phrase on the waiting end, is blocked by, not blocks", async () => {
		await givenSamOpensAPortfolioOn("Jira");

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(/wait/i);
		expect(tip).toHaveTextContent(IS_BLOCKED_BY);
		expect(tip).toHaveTextContent(NOT_BLOCKS);
	});

	// @contract-shape:pure-function
	it("on Jira, the tip says None reads Jira's own links and a chosen field replaces them", async () => {
		await givenSamOpensAPortfolioOn("Jira");

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(/None/);
		expect(tip).toHaveTextContent(/replace/i);
		expect(tip).toHaveTextContent(/rather than/);
	});

	// @contract-shape:pure-function
	it("on Azure DevOps, the tip names the Reverse end, Predecessor, and no Jira phrase", async () => {
		await givenSamOpensAPortfolioOn("AzureDevOps");

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(PREDECESSOR);
		expect(tip).toHaveTextContent(THE_REVERSE_END);
		expect(tip).not.toHaveTextContent(IS_BLOCKED_BY);
	});

	// @edge @contract-shape:pure-function
	it.each(["Linear", "Csv", "ServiceNow", undefined] as const)(
		"on a %s Portfolio the tip stays tracker-neutral, without an example",
		async (tracker) => {
			await givenSamOpensAPortfolioOn(tracker);

			const tip = await whenSamOpensTheTip();
			expect(tip).toHaveTextContent(/wait/i);
			expect(tip).not.toHaveTextContent(IS_BLOCKED_BY);
			expect(tip).not.toHaveTextContent(PREDECESSOR);
		},
	);

	// @edge @contract-shape:pure-function
	it("the tip speaks of the tenant's Bet that waits, never of a Feature", async () => {
		await givenSamOpensAPortfolioOn("Jira", RenamedTerms);

		const tip = await whenSamOpensTheTip();
		expect(tip).toHaveTextContent(/Bet/);
		expect(tip).not.toHaveTextContent(/Feature/);
	});

	// @contract-shape:pure-function
	it("Learn More opens the Dependency Field docs in a new tab", async () => {
		await givenSamOpensAPortfolioOn("Jira");

		expectLearnMoreToOpen(await whenSamOpensTheTip(), DEPENDENCY_FIELD_DOCS);
	});

	// @edge @contract-shape:pure-function
	it("with the keyboard alone, Enter opens the tip and Tab reaches Learn More", async () => {
		const user = userEvent.setup();
		await givenSamOpensAPortfolioOn("Jira");

		theDependencyButton().focus();
		await user.keyboard("{Enter}");
		const tip = await theOpenTip();
		await user.tab();

		expect(within(tip).getByRole("link", { name: "Learn More" })).toHaveFocus();
	});

	// @edge @contract-shape:bounded-change
	it("Escape closes the tip, and the button says it is closed again", async () => {
		const user = userEvent.setup();
		await givenSamOpensAPortfolioOn("Jira");

		const button = theDependencyButton();
		const tip = await openTheTip(user, button);
		expect(button).toHaveAttribute("aria-expanded", "true");
		await user.keyboard("{Escape}");

		await waitFor(() => expect(tip).not.toBeInTheDocument());
		expect(theDependencyButton()).toHaveAttribute("aria-expanded", "false");
	});
});

describe("Story 6352 slice 02: what stays as it is", () => {
	// @error @active-guard @contract-shape:bounded-change
	it("choosing a Dependency Field saves exactly what it saved before", async () => {
		const held = await givenSamOpensAPortfolioOn("Jira");
		const before = held.current;

		fireEvent.mouseDown(screen.getByRole("combobox", { name: DEPENDENCY }));
		fireEvent.click(
			within(screen.getByRole("listbox")).getByRole("option", {
				name: "Waits On",
			}),
		);

		expect(held.current).toEqual({
			...before,
			dependencyFieldSource: "Own",
			dependencyOverrideAdditionalFieldDefinitionId: 1,
		});
	});
});
