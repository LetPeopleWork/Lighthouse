import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import {
	Atlas,
	aFeatureServiceAnswering,
	aList,
	aRow,
	Borealis,
	Platform2027,
	renderAFeatureList,
	stubTheScreenSizeTheGridAsksFor,
	TeamBorealis,
} from "../../../tests/FeatureListTestKit";
import TeamFeatureList from "./TeamFeatureList";

const PAYMENT_PROVIDER_SWITCH = /Payment provider switch/;
const SEARCH_REVAMP = /Search revamp/;
const LEGACY_CLEANUP = /Legacy cleanup/;

beforeEach(() => {
	stubTheScreenSizeTheGridAsksFor();
	localStorage.clear();
});

describe("Story 6408 (slice 04): a Team's Features list draws each row from the one answer", () => {
	const borealisWorks = aList(
		[
			aRow(21, "NP-1", "Payment provider switch", { parent: "ST-1" }),
			aRow(12, "NW-2", "Search revamp"),
			aRow(13, "NW-3", "Legacy cleanup"),
		],
		{
			parents: [Platform2027],
			activeWork: [
				{ teamId: Borealis.id, featureReferenceIds: ["NP-1"] },
				{ teamId: Atlas.id, featureReferenceIds: ["NW-2"] },
			],
		},
	);

	it("a row names the Team only when this Team has it in progress, not when another Team does or nobody does", async () => {
		await renderAFeatureList(<TeamFeatureList team={TeamBorealis} />, {
			featureService: aFeatureServiceAnswering(Promise.resolve(borealisWorks)),
		});

		const atWork = await screen.findByRole("row", {
			name: PAYMENT_PROVIDER_SWITCH,
		});
		const worksOnlyElsewhere = screen.getByRole("row", { name: SEARCH_REVAMP });
		const worksNowhere = screen.getByRole("row", { name: LEGACY_CLEANUP });

		expect(within(atWork).getByTestId("active-work-indicator")).toBeVisible();
		expect(
			within(worksOnlyElsewhere).getByTestId("no-active-work"),
		).toBeInTheDocument();
		expect(
			within(worksNowhere).getByTestId("no-active-work"),
		).toBeInTheDocument();

		await userEvent.hover(within(atWork).getByTestId("active-work-indicator"));

		const tooltip = await screen.findByRole("tooltip");
		expect(
			within(tooltip).getByRole("link", { name: Borealis.name }),
		).toBeInTheDocument();
	});

	it("a row names its parent from the same answer", async () => {
		await renderAFeatureList(<TeamFeatureList team={TeamBorealis} />, {
			featureService: aFeatureServiceAnswering(Promise.resolve(borealisWorks)),
		});

		const row = await screen.findByRole("row", {
			name: PAYMENT_PROVIDER_SWITCH,
		});

		expect(within(row).getByText(/Platform 2027/)).toBeInTheDocument();
	});

	it("when Hide Completed hides every Feature the list says how many there are and how to see them", async () => {
		await renderAFeatureList(<TeamFeatureList team={TeamBorealis} />, {
			featureService: aFeatureServiceAnswering(
				Promise.resolve(
					aList([
						aRow(31, "NP-9", "Finished work", { completed: true }),
						aRow(32, "NP-10", "More finished work", { completed: true }),
					]),
				),
			),
		});

		expect(
			await screen.findByText(
				"All 2 Features are completed. Turn off Hide Completed Features to see them.",
			),
		).toBeInTheDocument();
	});
});
