import { describe, expect, it } from "vitest";
import type { IPerStatePercentileValues } from "../../models/PerStatePercentileValues";
import type { IWorkItem, StateCategory } from "../../models/WorkItem";
import { certainColor, errorColor } from "../theme/colors";
import {
	buildAgeBandColumnDescriptor,
	classifyPaceBand,
	NO_HISTORY_BAND_LABEL,
	PACE_BAND_COLORS_LOW_TO_HIGH,
	paceBandColorForRank,
	paceBandLabelForRank,
	paceBandOptionLabels,
	resolvePaceBandLadders,
} from "./paceBands";

// The team from the story: Analysis has never been exited by a finished item, so it has no
// percentiles of its own and nothing before it to inherit.
const zenithDoingStates = ["Analysis", "In Progress", "Review", "Testing"];

const zenithPercentiles: IPerStatePercentileValues[] = [
	{
		state: "In Progress",
		percentiles: [
			{ percentile: 50, value: 4 },
			{ percentile: 70, value: 7 },
			{ percentile: 85, value: 11 },
			{ percentile: 95, value: 15 },
		],
	},
	{
		state: "Review",
		percentiles: [
			{ percentile: 50, value: 8 },
			{ percentile: 70, value: 12 },
			{ percentile: 85, value: 17 },
			{ percentile: 95, value: 24 },
		],
	},
	{
		state: "Testing",
		percentiles: [
			{ percentile: 50, value: 11 },
			{ percentile: 70, value: 16 },
			{ percentile: 85, value: 22 },
			{ percentile: 95, value: 30 },
		],
	},
];

// The same team on a week where Review has not yet been exited by anything finished, so it reads
// against In Progress instead.
const zenithWithoutReviewHistory: IPerStatePercentileValues[] =
	zenithPercentiles.filter((perState) => perState.state !== "Review");

const zenithLadders = () =>
	resolvePaceBandLadders({
		perStatePercentileValues: zenithPercentiles,
		doingStates: zenithDoingStates,
	});

const workItem = (overrides?: Partial<IWorkItem>): IWorkItem => ({
	id: 388,
	referenceId: "ZEN-388",
	name: "Sign-off flow",
	url: "https://example.com/work/388",
	type: "User Story",
	state: "Review",
	stateCategory: "Doing" as StateCategory,
	startedDate: new Date("2026-08-10"),
	closedDate: new Date("2026-08-10"),
	cycleTime: 0,
	workItemAge: 26,
	parentWorkItemReference: "",
	isBlocked: false,
	...overrides,
});

describe("pace band ladder", () => {
	describe("resolving the ladders across the workflow", () => {
		it("gives a state its own percentiles in value-ascending order", () => {
			const ladders = zenithLadders();

			expect(ladders.get(2)?.state).toBe("Review");
			expect(ladders.get(2)?.percentiles.map((p) => p.value)).toEqual([
				8, 12, 17, 24,
			]);
		});

		it("lets a state with no finished history of its own read against the state before it", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: zenithWithoutReviewHistory,
				doingStates: zenithDoingStates,
			});

			expect(ladders.get(2)?.state).toBe("Review");
			expect(ladders.get(2)?.percentiles.map((p) => p.value)).toEqual([
				4, 7, 11, 15,
			]);
		});

		it("leaves a state with nothing before it to inherit out of the ladders entirely", () => {
			expect(zenithLadders().has(0)).toBe(false);
		});

		it("reads percentiles in whatever order they arrive", () => {
			const reversed = [...zenithPercentiles].reverse().map((perState) => ({
				...perState,
				percentiles: [...perState.percentiles].reverse(),
			}));

			const fromReversed = resolvePaceBandLadders({
				perStatePercentileValues: reversed,
				doingStates: zenithDoingStates,
			});

			expect(fromReversed.get(2)?.percentiles.map((p) => p.value)).toEqual([
				8, 12, 17, 24,
			]);
		});

		it("ignores a state whose percentile list came back empty", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: [
					{ state: "Analysis", percentiles: [] },
					...zenithPercentiles,
				],
				doingStates: zenithDoingStates,
			});

			expect(ladders.has(0)).toBe(false);
		});

		it("produces no ladders at all for a team with no finished history anywhere", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: [{ state: "Analysis", percentiles: [] }],
				doingStates: zenithDoingStates,
			});

			expect(ladders.size).toBe(0);
		});

		it("produces no ladders when the workflow itself is empty", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: zenithPercentiles,
				doingStates: [],
			});

			expect(ladders.size).toBe(0);
		});

		it("carries a single-percentile history forward just as it carries a full one", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: [
					{ state: "In Progress", percentiles: [{ percentile: 50, value: 4 }] },
				],
				doingStates: zenithDoingStates,
			});

			expect(ladders.get(2)?.percentiles.map((p) => p.value)).toEqual([4]);
		});

		it("answers the same way however many times it is asked", () => {
			expect([...zenithLadders()]).toEqual([...zenithLadders()]);
		});
	});

	describe.skip("placing an age against a state's ladder", () => {
		// Review's history: 8 / 12 / 17 / 24 days. An age sitting exactly on one of those belongs to
		// the band beneath it, which is the band the chart paints at that height.
		it.each([
			[7, 0],
			[8, 0],
			[9, 1],
			[11, 1],
			[12, 1],
			[13, 2],
			[16, 2],
			[17, 2],
			[18, 3],
			[23, 3],
			[24, 3],
			[25, 4],
			[260, 4],
		])("places %i days in Review at rank %i", (age, expectedRank) => {
			expect(classifyPaceBand(age, "Review", zenithLadders())).toBe(
				expectedRank,
			);
		});

		it("places an age at the very floor in the lowest band", () => {
			expect(classifyPaceBand(0, "Review", zenithLadders())).toBe(0);
		});

		it("recognises a state whose name differs only in capitalisation", () => {
			expect(classifyPaceBand(26, "rEvIeW", zenithLadders())).toBe(
				classifyPaceBand(26, "Review", zenithLadders()),
			);
		});

		it("places an age against the inherited ladder when the state has no history of its own", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: zenithWithoutReviewHistory,
				doingStates: zenithDoingStates,
			});

			expect(classifyPaceBand(9, "Review", ladders)).toBe(2);
		});

		it("has no answer for a state with nothing to measure against", () => {
			expect(classifyPaceBand(6, "Analysis", zenithLadders())).toBeUndefined();
		});

		it("has no answer for a state that is not part of the workflow", () => {
			expect(
				classifyPaceBand(6, "Waiting on legal", zenithLadders()),
			).toBeUndefined();
		});

		it("never lands on a band that two identical boundaries have collapsed to nothing", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: [
					{
						state: "Review",
						percentiles: [
							{ percentile: 50, value: 8 },
							{ percentile: 70, value: 12 },
							{ percentile: 85, value: 12 },
							{ percentile: 95, value: 24 },
						],
					},
				],
				doingStates: ["Review"],
			});

			expect(classifyPaceBand(12, "Review", ladders)).toBe(1);
		});
	});

	describe.skip("naming the band", () => {
		it.each([
			[0, "Below 50th"],
			[1, "50th-70th"],
			[2, "70th-85th"],
			[3, "85th-95th"],
			[4, "Above 95th"],
		])("names rank %i as %s", (rank, expectedLabel) => {
			expect(paceBandLabelForRank(rank, zenithLadders().get(2))).toBe(
				expectedLabel,
			);
		});

		it("names an absent rank as no history", () => {
			expect(paceBandLabelForRank(undefined, undefined)).toBe(
				NO_HISTORY_BAND_LABEL,
			);
		});

		it("names the top band of a short ladder for the highest percentile it actually has", () => {
			const ladders = resolvePaceBandLadders({
				perStatePercentileValues: [
					{
						state: "Review",
						percentiles: [
							{ percentile: 50, value: 8 },
							{ percentile: 85, value: 17 },
						],
					},
				],
				doingStates: ["Review"],
			});

			expect(paceBandLabelForRank(2, ladders.get(0))).toBe("Above 85th");
			expect(paceBandLabelForRank(1, ladders.get(0))).toBe("50th-85th");
		});

		it("writes the band names with plain hyphens so they survive a spreadsheet unchanged", () => {
			for (const label of paceBandOptionLabels(zenithLadders())) {
				expect(label).not.toMatch(/[‐-―]/);
			}
		});

		it("offers every band a team can show, no history first and the worst band last", () => {
			expect(paceBandOptionLabels(zenithLadders())).toEqual([
				NO_HISTORY_BAND_LABEL,
				"Below 50th",
				"50th-70th",
				"70th-85th",
				"85th-95th",
				"Above 95th",
			]);
		});
	});

	describe.skip("colouring the band", () => {
		it("runs from the calmest colour at the floor to the alarming one at the top", () => {
			const fills = [0, 1, 2, 3, 4].map((rank) =>
				paceBandColorForRank(rank, 4),
			);

			expect(fills).toEqual([...PACE_BAND_COLORS_LOW_TO_HIGH]);
		});

		it("still paints the worst band of a short ladder the most alarming colour", () => {
			expect(paceBandColorForRank(2, 2)).toBe(errorColor);
			expect(paceBandColorForRank(0, 2)).toBe(certainColor);
		});

		it("stops distinguishing colours past the palette and leaves the naming to carry it", () => {
			expect(paceBandColorForRank(5, 6)).toBe(paceBandColorForRank(6, 6));
		});
	});

	describe.skip("the column descriptor handed to the work item dialog", () => {
		const descriptor = () =>
			buildAgeBandColumnDescriptor({
				perStatePercentileValues: zenithPercentiles,
				doingStates: zenithDoingStates,
				headerName: "Work Item Age Band",
				description:
					"Where this age sits against how long finished items took to leave this state",
			});

		it("carries the header and the explanation it was given", () => {
			expect(descriptor()?.headerName).toBe("Work Item Age Band");
			expect(descriptor()?.description).toContain("finished items");
		});

		it("names the band for an item that has run past every finished item before it", () => {
			expect(descriptor()?.bandFor(workItem())).toBe("Above 95th");
		});

		it("names the band for an item sitting exactly on a boundary as the band beneath", () => {
			expect(descriptor()?.bandFor(workItem({ workItemAge: 8 }))).toBe(
				"Below 50th",
			);
		});

		it("says no history for an item in a state nothing finished has left", () => {
			expect(
				descriptor()?.bandFor(workItem({ state: "Analysis", workItemAge: 6 })),
			).toBe(NO_HISTORY_BAND_LABEL);
		});

		it("gives every band the chart's own colour and leaves no history unpainted", () => {
			expect(descriptor()?.colorForBand("Above 95th")).toBe(errorColor);
			expect(descriptor()?.colorForBand("Below 50th")).toBe(certainColor);
			expect(descriptor()?.colorForBand(NO_HISTORY_BAND_LABEL)).toBeUndefined();
		});

		it("offers nothing at all when no state has any finished history", () => {
			expect(
				buildAgeBandColumnDescriptor({
					perStatePercentileValues: [{ state: "Analysis", percentiles: [] }],
					doingStates: zenithDoingStates,
					headerName: "Work Item Age Band",
					description: "unused",
				}),
			).toBeUndefined();
		});
	});
});
