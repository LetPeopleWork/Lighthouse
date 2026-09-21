import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../../../../models/Feature";
import type { IWorkItem } from "../../../../../../models/WorkItem";
import {
	anythingToWarnAbout,
	barMarksFor,
	warningsColumnFor,
} from "./deliveryBarMarks";
import type { UnlanedTeam } from "./deliveryTeamLanes";

// What a bar has to say, on its own. Reached only through the tab it used to live in, most of these
// answers were decided before anything rendered and could only be checked by inference from what
// the chart ended up drawing.

const TERMS = {
	workItemsTerm: "Work Items",
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
};

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Deep Sea Mapping Initiative",
		stateCategory: "Doing",
		isUsingDefaultFeatureSize: false,
		dependsOn: [],
		getRemainingWorkForFeature: () => 0,
		...overrides,
	}) as IFeature;

const NO_TEAM_NOTES: ReadonlyMap<number, UnlanedTeam[]> = new Map();
const NO_DEPENDENCY_NOTES: ReadonlyMap<number, string[]> = new Map();

const unlaned = (teamId: number, teamName: string): UnlanedTeam =>
	({
		teamId,
		teamName,
		note: { text: `${teamName} has no dates to draw.`, isWarning: false },
	}) as UnlanedTeam;

const marksFor = (
	features: IFeature[],
	{
		dependencyNotes = NO_DEPENDENCY_NOTES,
		teamsWithoutALane = NO_TEAM_NOTES,
		showWarnings = false,
	} = {},
) =>
	barMarksFor(
		features,
		dependencyNotes,
		TERMS,
		teamsWithoutALane,
		showWarnings,
	);

describe("what a bar has to say", () => {
	it("says nothing at all about a Feature with nothing against it", () => {
		// An entry with an empty list is drawn as a symbol with nothing behind it. Paired with the
		// tests below, which is what stops this passing against a function returning nothing ever.
		expect(marksFor([feature()], { showWarnings: true }).has(1)).toBe(false);
	});

	it("carries the Feature's own warnings only when the warnings are being read", () => {
		// Both halves. A function that always includes them passes the first; one that never does
		// passes the second.
		const marked = [feature({ isUsingDefaultFeatureSize: true })];

		expect(marksFor(marked, { showWarnings: true }).get(1)?.notes).toHaveLength(
			1,
		);
		expect(marksFor(marked, { showWarnings: false }).has(1)).toBe(false);
	});

	it("marks a Feature's own warnings as worth an alarm, and the chart's own notes as not", () => {
		// The symbol a bar draws is chosen from this, so a sound dependency raising an alarm would
		// devalue the alarm everywhere it is right.
		const both = marksFor([feature({ isUsingDefaultFeatureSize: true })], {
			dependencyNotes: new Map([[1, ["Hull Fabrication sits below it."]]]),
			showWarnings: true,
		});

		expect(both.get(1)?.notes.map((note) => note.isWarning)).toEqual([
			true,
			false,
		]);
	});

	it("names a Team that got no row whether or not the warnings are being read", () => {
		// The promise the split makes is that it never shows fewer Teams than the Feature has, and
		// that promise belongs to the Teams rather than to the warnings. Carrying it inside the
		// warnings once meant a reader looking at the Teams was told about one and not the other.
		const teams = new Map([[1, [unlaned(7, "Meridian")]]]);

		for (const showWarnings of [true, false]) {
			const mark = marksFor([feature()], {
				teamsWithoutALane: teams,
				showWarnings,
			}).get(1);

			expect(mark?.namesOnTheBar).toEqual([{ teamId: 7, name: "Meridian" }]);
		}
	});

	it("tells two Teams apart when this Portfolio can name neither of them", () => {
		// Every Team a Portfolio cannot name gets the same sentence. Keyed on the sentence, one of
		// the two disappears - and the bar then says less than the Feature has.
		const sameWords = "A Team from outside this Portfolio";
		const teams = new Map([
			[
				1,
				[
					{
						teamId: 404,
						teamName: sameWords,
						note: { text: sameWords, isWarning: false },
					} as UnlanedTeam,
					{
						teamId: 405,
						teamName: sameWords,
						note: { text: sameWords, isWarning: false },
					} as UnlanedTeam,
				],
			],
		]);

		const mark = marksFor([feature()], { teamsWithoutALane: teams }).get(1);

		expect(mark?.notes.map((note) => note.subject)).toEqual([
			"team:404",
			"team:405",
		]);
		expect(mark?.namesOnTheBar).toHaveLength(2);
	});

	it("keeps each Feature's marks under its own id", () => {
		// Keyed by Feature rather than by position, which is right until the board is re-ordered.
		const marks = marksFor(
			[
				feature({ id: 11 }),
				feature({ id: 22, isUsingDefaultFeatureSize: true }),
			],
			{ showWarnings: true },
		);

		expect(marks.has(11)).toBe(false);
		expect(marks.get(22)?.notes).toHaveLength(1);
	});
});

describe("whether this Delivery has anything to warn about at all", () => {
	it("counts a Feature's own warning", () => {
		expect(
			anythingToWarnAbout(
				[feature({ isUsingDefaultFeatureSize: true })],
				TERMS,
			),
		).toBe(true);
	});

	it("counts a dependency there is nothing wrong with", () => {
		// A sound dependency raises no warning sentence and still puts a note on the bar, so a
		// gate counting only the sentences would withhold a control that does something.
		expect(
			anythingToWarnAbout(
				// biome-ignore lint/suspicious/noExplicitAny: the shape under test is one field.
				[feature({ dependsOn: [{} as any] })],
				TERMS,
			),
		).toBe(true);
	});

	it("counts nothing on a Delivery with neither", () => {
		// Paired with both rows above, or each of them passes against a function answering true.
		expect(anythingToWarnAbout([feature()], TERMS)).toBe(false);
	});
});

describe("the warnings column the bar's dialog is given", () => {
	it("answers for the Feature the row belongs to", () => {
		const column = warningsColumnFor(
			[feature({ id: 7, isUsingDefaultFeatureSize: true })],
			TERMS,
		);

		expect(column.warningsFor({ id: 7 } as IWorkItem)).toHaveLength(1);
	});

	it("answers nothing, rather than throwing, for a row it does not know", () => {
		// The dialog is shared with fifteen other screens, so it can be handed an item this chart
		// has never heard of.
		const column = warningsColumnFor([feature({ id: 7 })], TERMS);

		expect(column.warningsFor({ id: 404 } as IWorkItem)).toEqual([]);
	});
});
