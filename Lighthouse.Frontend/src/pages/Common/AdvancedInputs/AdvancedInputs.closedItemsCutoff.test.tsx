import { fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import {
	RenamedTerms,
	renderWithTerms,
	theInfoIconsIn,
} from "../../../tests/FieldTipsTestKit";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import AdvancedInputsComponent from "./AdvancedInputs";

/**
 * Sam, config-admin, opens a Team's (or a Portfolio's) settings and reads, right under Closed Items Cutoff
 * (days), what the number limits: finished items older than it are not kept, which limits metrics, forecasts
 * and how far back Reports can reach; 0 keeps them all; raising it brings older ones back with the next
 * update. The line names the tenant's own word for what it keeps, and it is always visible: no icon to find.
 */

const CUTOFF = "Closed Items Cutoff (days)";

const whatTheCutoffLimits = (finishedItems: string) =>
	`Finished ${finishedItems} older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0 keeps them all. Raising it brings older ones back with the next update.`;

type Owner = "Team" | "Portfolio";

const givenSamOpensTheSettingsOf = async (
	owner: Owner,
	{
		terms = {},
		settings = createMockTeamSettings() as IBaseSettings | null,
	} = {},
) => {
	await renderWithTerms(
		<AdvancedInputsComponent
			settings={settings}
			onSettingsChange={vi.fn()}
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

const theCutoff = () => screen.getByLabelText(CUTOFF);

describe("Story 6348 slice 01 (pending): the Closed Items Cutoff says what it limits", () => {
	// @driving_port @contract-shape:pure-function
	it.skip("on a Team's settings, the line under the cutoff says what it limits, of its Work Items", async () => {
		await givenSamOpensTheSettingsOf("Team");

		expect(theCutoff()).toHaveAccessibleDescription(
			whatTheCutoffLimits("Work Items"),
		);
	});

	// @driving_port @contract-shape:pure-function
	it.skip("on a Portfolio's settings, the same line speaks of its Features", async () => {
		await givenSamOpensTheSettingsOf("Portfolio");

		expect(theCutoff()).toHaveAccessibleDescription(
			whatTheCutoffLimits("Features"),
		);
	});

	// @edge @contract-shape:pure-function
	it.skip("a tenant that calls its Work Items Tickets reads Tickets on a Team, never Work Items", async () => {
		await givenSamOpensTheSettingsOf("Team", { terms: RenamedTerms });

		expect(theCutoff()).toHaveAccessibleDescription(
			whatTheCutoffLimits("Tickets"),
		);
		expect(screen.queryByText(/Work Items/)).not.toBeInTheDocument();
	});

	// @edge @contract-shape:pure-function
	it.skip("a tenant that calls its Features Bets reads Bets on a Portfolio, never Features", async () => {
		await givenSamOpensTheSettingsOf("Portfolio", { terms: RenamedTerms });

		expect(theCutoff()).toHaveAccessibleDescription(
			whatTheCutoffLimits("Bets"),
		);
		expect(screen.queryByText(/Features/)).not.toBeInTheDocument();
	});

	// @edge @contract-shape:pure-function
	it.skip("before the settings arrive, the line already says what the cutoff limits", async () => {
		await givenSamOpensTheSettingsOf("Team", { settings: null });

		expect(theCutoff()).toHaveAccessibleDescription(
			whatTheCutoffLimits("Work Items"),
		);
	});

	// @error @contract-shape:bounded-change
	it.skip("names Reports in general, and the old retain-and-filter wording is gone", async () => {
		await givenSamOpensTheSettingsOf("Team");

		expect(screen.getByText(/how far back Reports can reach/)).toBeVisible();
		expect(screen.queryByText(/Then & Now/)).not.toBeInTheDocument();
		expect(
			screen.queryByText(/Number of days to retain closed\/done items/),
		).not.toBeInTheDocument();
	});
});

describe("Story 6348 slice 01: what stays as it is", () => {
	// @error @active-guard @contract-shape:unbounded-preservation
	it("the explanation sits in the open, so no info icon hides it", async () => {
		await givenSamOpensTheSettingsOf("Team");

		const theCutoffsPlace = theCutoff().closest(".MuiGrid-root");
		expect(theCutoffsPlace).not.toBeNull();
		expect(theInfoIconsIn(theCutoffsPlace as HTMLElement)).toHaveLength(0);
		expect(
			screen.queryByRole("button", { name: /^About Closed Items Cutoff/ }),
		).not.toBeInTheDocument();
	});
});
