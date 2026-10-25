import { act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import type { IFeatureList } from "../../../models/FeatureList";
import {
	aFeatureServiceAnswering,
	aList,
	aRow,
	aTeamMetricsService,
	Borealis,
	RenamedTerms,
	renderAFeatureList,
	stubTheScreenSizeTheGridAsksFor,
	TeamBorealis,
	TeamWithNothingYet,
	theListStatus,
} from "../../../tests/FeatureListTestKit";
import { deferred } from "../../../tests/HeldMetricsService";
import TeamFeatureList from "./TeamFeatureList";

const WhatBorealisWorksOn = aList(
	[
		aRow(21, "NP-1", "Payment provider switch"),
		aRow(12, "NW-2", "Search revamp"),
	],
	{
		activeWork: [{ teamId: Borealis.id, featureReferenceIds: ["NP-1"] }],
	},
);

const PAYMENT_PROVIDER_SWITCH = /Payment provider switch/;

const NO_FEATURES_YET = "This Team has no Features yet.";

const COULD_NOT_LOAD = "The Features couldn't be loaded.";

const theListKeyOf = (teamId: number) => ["feature-list", "team", teamId];

const aDroppedConnection = () =>
	Promise.reject<IFeatureList>(new Error("the network dropped"));

beforeEach(() => {
	stubTheScreenSizeTheGridAsksFor();
	localStorage.clear();
});

describe("Story 6408 (slice 04): Aisha opens Team Borealis's Features and one answer fills the list", () => {
	it("the list asks once, for the Team's list, and reads nothing else", async () => {
		const featureService = aFeatureServiceAnswering(
			Promise.resolve(WhatBorealisWorksOn),
		);
		const teamMetricsService = aTeamMetricsService();

		const { container } = await renderAFeatureList(
			<TeamFeatureList team={TeamBorealis} />,
			{ featureService, teamMetricsService },
		);

		expect(
			await screen.findByText(PAYMENT_PROVIDER_SWITCH),
		).toBeInTheDocument();
		expect(screen.getByText(/Search revamp/)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("ready");
		expect(featureService.getFeatureList).toHaveBeenCalledTimes(1);
		expect(featureService.getFeatureList).toHaveBeenCalledWith({
			kind: "team",
			id: TeamBorealis.id,
		});
		expect(featureService.getFeaturesByIds).not.toHaveBeenCalled();
		expect(teamMetricsService.getFeaturesInProgress).not.toHaveBeenCalled();
	});

	it("a Team that works on no Features yet says so, as an answer that has arrived", async () => {
		const { container } = await renderAFeatureList(
			<TeamFeatureList team={TeamWithNothingYet} />,
			{
				featureService: aFeatureServiceAnswering(Promise.resolve(aList([]))),
			},
		);

		expect(await screen.findByText(NO_FEATURES_YET)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("ready");
	});

	it("when the list cannot be loaded it says so, and Retry asks again", async () => {
		const featureService = aFeatureServiceAnswering(
			aDroppedConnection(),
			Promise.resolve(WhatBorealisWorksOn),
		);
		const { container } = await renderAFeatureList(
			<TeamFeatureList team={TeamBorealis} />,
			{ featureService },
		);

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("error");

		await userEvent.click(screen.getByRole("button", { name: "Retry" }));

		expect(
			await screen.findByText(PAYMENT_PROVIDER_SWITCH),
		).toBeInTheDocument();
	});

	it("the messages use the instance's own words for Features and Teams", async () => {
		await renderAFeatureList(<TeamFeatureList team={TeamWithNothingYet} />, {
			featureService: aFeatureServiceAnswering(Promise.resolve(aList([]))),
			terms: RenamedTerms,
		});

		expect(
			await screen.findByText("This Squad has no Bets yet."),
		).toBeInTheDocument();
	});
});

describe("Story 6408 (slice 04): after an update the Team's list never shows an old answer as current", () => {
	it("a refresh that fails takes the could-not-load look rather than showing the old rows as current", async () => {
		const { container, queryClient } = await renderAFeatureList(
			<TeamFeatureList team={TeamBorealis} />,
			{
				featureService: aFeatureServiceAnswering(
					Promise.resolve(WhatBorealisWorksOn),
					aDroppedConnection(),
				),
			},
		);
		await screen.findByText(PAYMENT_PROVIDER_SWITCH);

		await act(async () => {
			await queryClient
				.invalidateQueries({ queryKey: theListKeyOf(TeamBorealis.id) })
				.catch(() => undefined);
		});

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("error");
		expect(
			screen.queryByRole("row", { name: PAYMENT_PROVIDER_SWITCH }),
		).not.toBeInTheDocument();
	});

	it("an answer for the Team the reader has already left never replaces the one they are on", async () => {
		const borealisAnswer = deferred<IFeatureList>();
		const nothingYetAnswer = deferred<IFeatureList>();
		const { rerender } = await renderAFeatureList(
			<TeamFeatureList team={TeamBorealis} />,
			{
				featureService: aFeatureServiceAnswering(
					borealisAnswer.promise,
					nothingYetAnswer.promise,
				),
			},
		);

		rerender(<TeamFeatureList team={TeamWithNothingYet} />);
		await act(async () => nothingYetAnswer.resolve(aList([])));
		await act(async () => borealisAnswer.resolve(WhatBorealisWorksOn));

		expect(await screen.findByText(NO_FEATURES_YET)).toBeInTheDocument();
		expect(screen.queryByText(PAYMENT_PROVIDER_SWITCH)).not.toBeInTheDocument();
	});
});
