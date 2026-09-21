import { describe, expect, it } from "vitest";
import type { IEntityReference } from "../../../../../../models/EntityReference";
import type { IFeature } from "../../../../../../models/Feature";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import { buildDeliveryTeamLanes } from "./deliveryTeamLanes";
import {
	buildDeliveryTimeline,
	type TimelinePercentile,
} from "./deliveryTimelineModel";

// Which Teams get a lane, what each one spans and what is said about the ones that get none —
// decided here, with no chart anywhere near it. The library paints to a canvas this environment
// does not have, so anything asserted through it passes against broken code.

const october = (day: number) => new Date(2026, 9, day);

const at = (probability: number, day: number) =>
	WhenForecast.new(probability, october(day));

const TERMS = { teamTerm: "Team", portfolioTerm: "Portfolio" };

/** What a Team outside this Portfolio is called, written out rather than asked of the code. */
const OUTSIDE_THIS_PORTFOLIO = "A Team from outside this Portfolio";

const forTeam = (
	teamId: number,
	starts: number[][],
	completions: number[][],
) => ({
	teamId,
	startPercentiles: starts.map(([probability, day]) => at(probability, day)),
	completionPercentiles: completions.map(([probability, day]) =>
		at(probability, day),
	),
});

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Coral Reef Restoration",
		startForecast: { source: "Forecast", percentiles: [at(70, 10), at(95, 9)] },
		forecasts: [at(70, 20), at(95, 26)],
		teamsWithoutForecast: [],
		teamForecasts: [],
		// Answered so a module that consults it can be caught doing so: every Team below has a
		// forecast row and no work at all, which is what the Feature table counts by.
		getTotalWorkForTeam: () => 0,
		...overrides,
	}) as IFeature;

const team = (id: number, name: string): IEntityReference => ({ id, name });

const lanesFor = (
	features: IFeature[],
	teams: IEntityReference[] = [],
	percentile: TimelinePercentile = 70,
) =>
	buildDeliveryTeamLanes(
		features,
		buildDeliveryTimeline(features, percentile),
		teams,
		percentile,
		TERMS,
	);

const ZENITH = team(5, "Zenith");
const GRAVITY = team(6, "Gravity");
const MERIDIAN = team(7, "Meridian");

const twoTeamsOneNameless = () =>
	feature({
		teamForecasts: [
			forTeam(5, [[70, 12]], [[70, 15]]),
			forTeam(404, [[70, 17]], [[70, 24]]),
		],
	});

describe("which Teams get a lane of their own", () => {
	it("gives a Feature two Teams contribute to one lane each, over that Team's own dates", () => {
		// Neither Team shares a date with the Feature, the two spans are disjoint, and no start
		// list overlaps a completion list — so reading the Feature's forecast, swapping the two
		// ends, or serving one Team's dates to both all fail here.
		const { lanes } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(lanes.map((lane) => [lane.teamName, lane.start, lane.end])).toEqual([
			["Gravity", october(17), october(24)],
			["Zenith", october(12), october(15)],
		]);
	});

	it("leaves a Feature one Team contributes to unsplit, beside one that does split", () => {
		// Asserted with a splitting Feature in the same call. On its own, "no lane for this one"
		// passes against a module that returns nothing at all.
		const { lanes, canShowTeams } = lanesFor(
			[
				feature({
					id: 1,
					name: "Kelp Forest",
					teamForecasts: [forTeam(5, [[70, 12]], [[70, 15]])],
				}),
				feature({
					id: 2,
					name: "Whale Migration",
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(lanes.map((lane) => lane.featureId)).toEqual([2, 2]);
		// And the control is offered, because there is something to show. Asserted here rather
		// than only in its absent cases, which a verdict hard-wired to "nothing to show" satisfies.
		expect(canShowTeams).toBe(true);
	});

	it("says nothing about Teams without a lane where every Team has one", () => {
		// An empty note list left on a Feature is not nothing: it is a Feature the chart believes
		// has something to say about a missing Team, and it turns the control on for a Delivery
		// where nothing is missing.
		const { unlanedTeams } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(unlanedTeams.size).toBe(0);
	});

	it("leaves a Feature that carries no per-Team forecast at all entirely alone", () => {
		// Every timeline fixture written before the per-Team forecasts existed is this shape, and
		// the field is still optional on the contract for that reason. Reading it as anything but
		// absent invents a contributing Team out of a missing field.
		const olderShape = feature();
		olderShape.teamForecasts = undefined;

		const { lanes, unlanedTeams, barTeams, legend, canShowTeams } = lanesFor(
			[olderShape],
			[ZENITH, GRAVITY],
		);

		expect(lanes).toEqual([]);
		expect(unlanedTeams.size).toBe(0);
		expect(barTeams.size).toBe(0);
		expect(legend).toEqual([]);
		expect(canShowTeams).toBe(false);
	});

	it("treats a Team the Portfolio lists without a name as one it cannot name", () => {
		// A blank name is not a name. Taken as one it is written along the lane as nothing at all,
		// and the colour helper drops a falsy key outright, so the lane loses its colour too.
		const { lanes } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, { id: 6, name: "" }],
		);

		const nameless = lanes.find((lane) => lane.teamId === 6);

		expect(nameless?.teamName).toBe(OUTSIDE_THIS_PORTFOLIO);
		expect(nameless?.color).toBeTruthy();
	});

	it("moves every lane when the probability moves, and reorders none of them", () => {
		const features = [
			feature({
				teamForecasts: [
					forTeam(
						5,
						[
							[70, 12],
							[95, 14],
						],
						[
							[70, 15],
							[95, 19],
						],
					),
					forTeam(
						6,
						[
							[70, 17],
							[95, 18],
						],
						[
							[70, 24],
							[95, 29],
						],
					),
				],
			}),
		];

		const atSeventy = lanesFor(features, [ZENITH, GRAVITY], 70).lanes;
		const atNinetyFive = lanesFor(features, [ZENITH, GRAVITY], 95).lanes;

		// Both halves. Every lane must have moved, or the percentile is being ignored; and the
		// sequence of Teams must be identical, or the lanes are ordered by date and a reader
		// loses the Team they were following the moment they move the control.
		expect(atNinetyFive.map((lane) => [lane.start, lane.end])).toEqual([
			[october(18), october(29)],
			[october(14), october(19)],
		]);
		expect(atNinetyFive.map((lane) => lane.teamId)).toEqual(
			atSeventy.map((lane) => lane.teamId),
		);
	});

	it("puts the unnamed Team last however early in the forecast it arrives", () => {
		// The same rule from the other side. With the unnamed Team arriving first, a comparator
		// that only ever answers "this one goes after" reads correctly on the fixture below and
		// wrongly here - the two orderings only disagree when the named Team is the one being
		// asked about.
		const { lanes } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(99, [[70, 11]], [[70, 13]]),
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(lanes.map((lane) => lane.teamName)).toEqual([
			"Gravity",
			"Zenith",
			OUTSIDE_THIS_PORTFOLIO,
		]);
	});

	it("reads the lanes in the Teams' own alphabetical order, with the unnamed Team last", () => {
		// The rows arrive in reverse alphabetical order, and the unnamed Team's fallback begins
		// with an "A" — so "keep the order given" and "sort by what is written on the lane" each
		// produce a different answer from this one.
		const { lanes } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
						forTeam(99, [[70, 11]], [[70, 13]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(lanes.map((lane) => lane.teamName)).toEqual([
			"Gravity",
			"Zenith",
			OUTSIDE_THIS_PORTFOLIO,
		]);
	});

	it("keeps a lane that begins before its Feature's bar on its own date", () => {
		// A Feature's own start is taken inside each simulated run while a lane is that Team's
		// marginal, so the earliest lane is not required to start where the bar does. Clamping it
		// into the bar is the plausible fix for a chart that looks wrong, and it would be a lie.
		const features = [
			feature({
				teamForecasts: [
					forTeam(5, [[70, 4]], [[70, 15]]),
					forTeam(6, [[70, 17]], [[70, 24]]),
				],
			}),
		];

		const { bars } = buildDeliveryTimeline(features, 70);
		const { lanes } = lanesFor(features, [ZENITH, GRAVITY]);

		expect(bars[0].start).toEqual(october(10));
		expect(lanes.find((lane) => lane.teamId === 5)?.start).toEqual(october(4));
	});

	it("starts a lane at a forecast even where its Feature's bar starts at an observed date", () => {
		// There is no per-Team observed start anywhere — not on the contract and not in the
		// domain — so a lane copying its Feature's observed start would be inventing one.
		const features = [
			feature({
				startForecast: {
					source: "Observed",
					observedDate: october(1),
					percentiles: [],
				},
				teamForecasts: [
					forTeam(5, [[70, 12]], [[70, 15]]),
					forTeam(6, [[70, 17]], [[70, 24]]),
				],
			}),
		];

		const { bars } = buildDeliveryTimeline(features, 70);
		const { lanes } = lanesFor(features, [ZENITH, GRAVITY]);

		expect(bars[0].startIsObserved).toBe(true);
		expect(bars[0].start).toEqual(october(1));
		expect(lanes.find((lane) => lane.teamId === 5)?.start).toEqual(october(12));
		expect(lanes[0]).not.toHaveProperty("startIsObserved");
	});

	it("gives a Team with no forecast at either end no lane, and names it on the bar instead", () => {
		// Both halves. A dateless lane is drawn at a position the data does not support rather
		// than left out, and a Team that simply vanishes is the silent disagreement this whole
		// criterion exists to prevent.
		const { lanes, unlanedTeams } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(7, [], []),
					],
				}),
			],
			[ZENITH, MERIDIAN],
		);

		expect(lanes.map((lane) => lane.teamId)).toEqual([5]);
		expect(unlanedTeams.get(1)?.map((unlaned) => unlaned.teamName)).toEqual([
			"Meridian",
		]);
		expect(unlanedTeams.get(1)?.[0].note.text).toContain("Meridian");
	});

	it("treats a Team that resolves at one end only, or at one probability only, the same way", () => {
		const startOnly = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(7, [[70, 12]], []),
					],
				}),
			],
			[ZENITH, MERIDIAN],
		);

		const endOnly = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(7, [], [[70, 15]]),
					],
				}),
			],
			[ZENITH, MERIDIAN],
		);

		// The third case is the one a `percentiles.length > 0` test would wave through: this Team
		// resolves at 70 and not at 95, and 95 is what the reader is looking at.
		const wrongProbability = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(
							5,
							[
								[70, 12],
								[95, 14],
							],
							[
								[70, 15],
								[95, 19],
							],
						),
						forTeam(7, [[70, 12]], [[70, 15]]),
					],
				}),
			],
			[ZENITH, MERIDIAN],
			95,
		);

		for (const result of [startOnly, endOnly, wrongProbability]) {
			expect(result.lanes.map((lane) => lane.teamId)).toEqual([5]);
			expect(result.unlanedTeams.get(1)?.map((one) => one.teamName)).toEqual([
				"Meridian",
			]);
		}
	});

	it("tells apart two un-laned Teams this Portfolio cannot name", () => {
		// Both are given the same fallback phrase, so the name identifies neither of them. Anything
		// downstream that keys on it keeps one and drops the other - which shows one Team where the
		// Feature has two, the exact disagreement naming them was meant to prevent.
		const { unlanedTeams } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(404, [], []),
						forTeam(405, [], []),
					],
				}),
			],
			[ZENITH],
		);

		const unlaned = unlanedTeams.get(1) ?? [];

		expect(unlaned.map((one) => one.teamId)).toEqual([404, 405]);
		// Paired with the thing that makes them indistinguishable, so this cannot be read as
		// passing because the names happened to differ.
		expect(new Set(unlaned.map((one) => one.teamName)).size).toBe(1);
	});

	it("says what it has to say about the un-laned Team as a note, not as an alarm", () => {
		// The Feature is forecasting correctly and this Team has nothing left to do. Amber here
		// spends the alarm on something that is not wrong, and teaches the reader to stop reading
		// the symbol at all.
		const { unlanedTeams } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(7, [], []),
					],
				}),
			],
			[ZENITH, MERIDIAN],
		);

		const notes = unlanedTeams.get(1) ?? [];

		expect(notes).toHaveLength(1);
		expect(notes[0].note.isWarning).toBe(false);
	});

	it("never shows fewer Teams than the Feature has", () => {
		const { lanes, unlanedTeams } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
						forTeam(7, [], []),
						forTeam(99, [[70, 11]], [[70, 13]]),
					],
				}),
			],
			[ZENITH, GRAVITY, MERIDIAN],
		);

		// The criterion's purpose rather than its mechanism: a dropped Team fails this, and so
		// does a Team handed both a lane and a note.
		const accountedFor = [
			...lanes.map((lane) => lane.teamName),
			...(unlanedTeams.get(1) ?? []).map((unlaned) => unlaned.teamName),
		];

		expect([...accountedFor].sort((a, b) => a.localeCompare(b))).toEqual([
			OUTSIDE_THIS_PORTFOLIO,
			"Gravity",
			"Meridian",
			"Zenith",
		]);
	});

	it("gives a Team this Portfolio cannot name a lane, in the reader's own words", () => {
		const { lanes } = lanesFor([twoTeamsOneNameless()], [ZENITH]);

		const stranger = lanes.find((lane) => lane.teamId === 404);

		// Pinned against the literal, and again with both terms renamed. A mutation run never
		// challenges copy, and a loose match would let the id or an empty string through.
		expect(stranger?.teamName).toBe(OUTSIDE_THIS_PORTFOLIO);
		expect(stranger?.start).toEqual(october(17));

		const features = [twoTeamsOneNameless()];
		const renamed = buildDeliveryTeamLanes(
			features,
			buildDeliveryTimeline(features, 70),
			[ZENITH],
			70,
			{ teamTerm: "Squad", portfolioTerm: "Programme" },
		);

		expect(renamed.lanes.find((lane) => lane.teamId === 404)?.teamName).toBe(
			"A Squad from outside this Programme",
		);
	});

	it("tells two Teams this Portfolio cannot name apart", () => {
		// Both Teams this Portfolio cannot name are given the same fallback phrase, so a colour map
		// keyed on the name would collapse them into one bucket and paint two different Teams
		// identically. (It would not drop them: the fallback is a real sentence, so it survives the
		// helper's own `keys.filter(Boolean)`.) Asserted as a difference between the four, never
		// against a colour value.
		const { lanes } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 13]], [[70, 16]]),
						forTeam(404, [[70, 17]], [[70, 24]]),
						forTeam(405, [[70, 18]], [[70, 25]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(lanes).toHaveLength(4);
		expect(new Set(lanes.map((lane) => lane.color)).size).toBe(4);
	});

	it("paints one Team one colour wherever it appears on the chart", () => {
		const { lanes } = lanesFor(
			[
				feature({
					id: 1,
					name: "Coral Reef",
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(7, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, GRAVITY, MERIDIAN],
		);

		const colourOn = (featureId: number, teamId: number) =>
			lanes.find(
				(lane) => lane.featureId === featureId && lane.teamId === teamId,
			)?.color;

		// Compared against the other lane's colour rather than against anything read back out of
		// the colour helper — the same reduction on both sides agrees with itself.
		expect(colourOn(1, 5)).toBe(colourOn(2, 5));
		expect(colourOn(1, 5)).not.toBe(colourOn(1, 6));
	});

	it("counts the Teams the forecast has, not the Teams the table shows", () => {
		// The Feature table lists the Portfolio's Teams filtered by total work; the forecast
		// carries one row per work row. They disagree here in both directions — Zenith and
		// Gravity have forecasts and no work at all, Meridian is listed and has no forecast row —
		// and the lanes follow the forecast.
		const { lanes } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
					getTotalWorkForTeam: () => 0,
				}),
			],
			[ZENITH, GRAVITY, MERIDIAN],
		);

		expect(lanes.map((lane) => lane.teamId).sort((a, b) => a - b)).toEqual([
			5, 6,
		]);
	});
});

describe("the one Team a Feature has to itself", () => {
	it("puts that Team on the Feature's own bar instead of giving it a row", () => {
		// With one Team the earliest and the latest across the Teams are that Team, so a row of
		// its own would be a second bar drawn exactly where the first one is. What the bar is
		// missing is which Team, not the span.
		const { lanes, barTeams } = lanesFor(
			[
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(5, [[70, 12]], [[70, 15]])],
				}),
			],
			[ZENITH],
		);

		expect(lanes).toEqual([]);
		expect(barTeams.get(2)?.teamName).toBe("Zenith");
		expect(barTeams.get(2)?.color).toBeTruthy();
	});

	it("leaves a Feature several Teams work on wearing its own colour", () => {
		// Dark means several Teams and coloured means this one Team, so a multi-Team Feature
		// taking one of its Teams' colours would say something false at a glance.
		const { barTeams } = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			[ZENITH, GRAVITY],
		);

		expect(barTeams.size).toBe(0);
	});

	it("says nothing on the bar of a Feature whose one Team it cannot name", () => {
		// A bar already carries its Feature's name. Adding a phrase to it that amounts to "a Team
		// we cannot name" spends the width without answering the question the reader asked.
		const { barTeams, canShowTeams } = lanesFor(
			[
				feature({
					id: 2,
					teamForecasts: [forTeam(404, [[70, 12]], [[70, 15]])],
				}),
			],
			[ZENITH],
		);

		expect(barTeams.size).toBe(0);
		expect(canShowTeams).toBe(false);
	});

	it("gives one Team one colour whether it has a row or a bar to itself", () => {
		// The trap this exists for: colouring the rows from one map and the single-Team bars from
		// a second one built over its own key set. Nothing about either map looks wrong on its
		// own, and the same Team then wears two colours on one screen.
		//
		// The shared Team is deliberately the one that does NOT sort first. The helper hands the
		// first key the same colour whatever else is in the set, so a shared Team sorting first
		// would come out identical from both maps by coincidence and this could not fail.
		const { lanes, barTeams } = lanesFor(
			[
				feature({
					id: 1,
					name: "Coral Reef",
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(6, [[70, 12]], [[70, 15]])],
				}),
			],
			[ZENITH, GRAVITY],
		);

		const gravitysRow = lanes.find((lane) => lane.teamId === 6)?.color;

		expect(barTeams.get(2)?.color).toBe(gravitysRow);
		// Paired with a Team that must differ, so a map answering one colour for everything fails.
		expect(lanes.find((lane) => lane.teamId === 5)?.color).not.toBe(
			gravitysRow,
		);
	});
});

describe("the key to the colours", () => {
	it("names every Team carrying a colour, once each, in the order the rows read", () => {
		const { legend } = lanesFor(
			[
				feature({
					id: 1,
					name: "Coral Reef",
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
						forTeam(99, [[70, 11]], [[70, 13]]),
					],
				}),
				feature({
					id: 2,
					name: "Kelp Forest",
					teamForecasts: [forTeam(5, [[70, 12]], [[70, 15]])],
				}),
				feature({
					id: 3,
					name: "Whale Migration",
					teamForecasts: [forTeam(7, [[70, 12]], [[70, 15]])],
				}),
			],
			[ZENITH, GRAVITY, MERIDIAN],
		);

		// Zenith has both a row and a bar to itself and is listed once; Meridian only ever wears a
		// bar and is listed all the same; the unnamed Team reads last, as its row does.
		expect(legend.map((team) => team.teamName)).toEqual([
			"Gravity",
			"Meridian",
			"Zenith",
			OUTSIDE_THIS_PORTFOLIO,
		]);
		expect(new Set(legend.map((team) => team.color)).size).toBe(4);
	});

	it("is empty where no Team carries a colour", () => {
		const { legend, canShowTeams } = lanesFor([feature()], [ZENITH]);

		expect(legend).toEqual([]);
		expect(canShowTeams).toBe(false);
	});
});

describe("a Team's colour, once the reader has learned it", () => {
	// Meridian resolves at 70 and not at 95, and its id sorts before the other two. That ordering
	// is the whole fixture: the colour helper assigns by position over its sorted keys, so a key
	// set that loses Meridian at 95 hands Zenith and Gravity the colour of their neighbour. A
	// fixture whose dropping Team sorted last could not fail.
	const MERIDIAN_EARLY = team(4, "Meridian");

	const threeTeams = () =>
		feature({
			teamForecasts: [
				forTeam(4, [[70, 11]], [[70, 13]]),
				forTeam(
					5,
					[
						[70, 12],
						[95, 14],
					],
					[
						[70, 15],
						[95, 19],
					],
				),
				forTeam(
					6,
					[
						[70, 17],
						[95, 18],
					],
					[
						[70, 24],
						[95, 29],
					],
				),
			],
		});

	it("does not change when the reader moves the probability", () => {
		const known = [MERIDIAN_EARLY, ZENITH, GRAVITY];

		const colourAt = (percentile: TimelinePercentile, teamId: number) =>
			lanesFor([threeTeams()], known, percentile).lanes.find(
				(lane) => lane.teamId === teamId,
			)?.color;

		// Meridian has dropped off the chart by 95, and the two that remain must not inherit each
		// other's colour. Paired with a difference, so a map answering one colour for everything
		// cannot satisfy the invariance half.
		expect(colourAt(95, 5)).toBe(colourAt(70, 5));
		expect(colourAt(95, 6)).toBe(colourAt(70, 6));
		expect(colourAt(70, 5)).not.toBe(colourAt(70, 6));
	});

	it("does not change between two Deliveries of the same Portfolio", () => {
		// The Portfolio's own Teams are in the key set whether or not this Delivery uses them, so
		// two Deliveries drawn for one Portfolio agree about which colour means which Team.
		const known = [MERIDIAN_EARLY, ZENITH, GRAVITY];

		const here = lanesFor(
			[
				feature({
					teamForecasts: [
						forTeam(5, [[70, 12]], [[70, 15]]),
						forTeam(6, [[70, 17]], [[70, 24]]),
					],
				}),
			],
			known,
		);

		const elsewhere = lanesFor(
			[
				feature({
					id: 9,
					name: "Whale Migration",
					teamForecasts: [
						forTeam(4, [[70, 11]], [[70, 13]]),
						forTeam(5, [[70, 12]], [[70, 15]]),
					],
				}),
			],
			known,
		);

		const zenithHere = here.lanes.find((lane) => lane.teamId === 5)?.color;

		expect(elsewhere.lanes.find((lane) => lane.teamId === 5)?.color).toBe(
			zenithHere,
		);
		expect(here.lanes.find((lane) => lane.teamId === 6)?.color).not.toBe(
			zenithHere,
		);
	});
});
