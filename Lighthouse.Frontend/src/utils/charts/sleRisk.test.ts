import { describe, expect, test } from "vitest";
import type { ISleRisk } from "../../models/Metrics/SleRisk";
import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";
import {
	buildSleRiskColumnDescriptor,
	sleRiskAtRiskSummary,
	sleRiskColumnDescription,
	sleRiskColumnHeaderName,
	sleRiskEvidenceDisclosure,
	sleRiskSortValue,
} from "./sleRisk";

const zenithItem = (referenceId: string): IWorkItem => ({
	id: Number.parseInt(referenceId.replace("ZEN-", ""), 10),
	name: `Item ${referenceId}`,
	state: "In Progress",
	stateCategory: "Doing",
	type: "User Story",
	referenceId,
	url: `https://example.com/work/${referenceId}`,
	startedDate: new Date("2026-09-01"),
	closedDate: new Date("2026-09-01"),
	cycleTime: 0,
	workItemAge: 9,
	parentWorkItemReference: "",
	isBlocked: false,
});

const answer = (
	referenceId: string,
	risk: number,
	finishedItemsStillOpenAtThisAge = 4,
	finishedItemsThatWentOnToMiss: number | null = 3,
): ISleRisk => ({
	referenceId,
	risk,
	finishedItemsStillOpenAtThisAge,
	finishedItemsThatWentOnToMiss,
});

const descriptorFor = (answers: ISleRisk[]) =>
	buildSleRiskColumnDescriptor({
		answers,
		headerName: "SLE Risk",
		description: "how likely this one is to miss",
		workItemTerm: "Work Item",
		workItemsTerm: "Work Items",
		sleTerm: "SLE",
		sleRangeInDays: 10,
	});

const evidence = (
	finishedItemsStillOpenAtThisAge: number,
	finishedItemsThatWentOnToMiss: number | null,
	workItemTerm = "Work Item",
	workItemsTerm = "Work Items",
) => ({
	finishedItemsStillOpenAtThisAge,
	finishedItemsThatWentOnToMiss,
	sleRangeInDays: 10,
	workItemTerm,
	workItemsTerm,
	sleTerm: "SLE",
});

describe("the risk column's wording", () => {
	test("names the target with whatever the team calls it", () => {
		expect(sleRiskColumnHeaderName("Service Level Expectation")).toBe(
			"Service Level Expectation Risk",
		);
	});

	test("says what the number is a share of, in the team's own words", () => {
		expect(sleRiskColumnDescription("Ticket", "Goal")).toBe(
			"Of every Ticket still open at this age across the team's configured history, the share that went on to miss the Goal",
		);
	});

	test("names the evidence, so nobody reads the number as following the range picker", () => {
		// The wording is the whole of the disclosure. The evidence is the team's configured history
		// and the answer is about today, so a reader who moves the range and sees nothing change has
		// been told why before they file it as a bug.
		expect(sleRiskColumnDescription("Work Item", "SLE")).toContain(
			"the team's configured history",
		);
	});
});

describe("building the risk column", () => {
	test("offers nothing at all when no item was answered for", () => {
		// A team with no published target is the only way this arrives empty, and there is then no
		// promise for anything to be at risk of breaking.
		expect(descriptorFor([])).toBeUndefined();
	});

	test("offers nothing when there is no target to name, whatever the answers say", () => {
		// The two inputs agree in practice - the endpoint returns nothing for a team without a
		// target - so this pins the half that would otherwise go untested and draw "the 0 day SLE".
		const withoutTarget = buildSleRiskColumnDescriptor({
			answers: [answer("ZEN-412", 86)],
			headerName: "SLE Risk",
			description: "how likely this one is to miss",
			workItemTerm: "Work Item",
			workItemsTerm: "Work Items",
			sleTerm: "SLE",
			sleRangeInDays: undefined,
		});

		expect(withoutTarget).toBeUndefined();
	});

	test("treats a zero range as no target rather than as a target of zero", () => {
		// A range of zero IS how the absence is stored on the team, so it reaches here as a number
		// rather than as nothing. Letting it through would draw a column promising "the 0 day SLE".
		const withZeroRange = buildSleRiskColumnDescriptor({
			answers: [answer("ZEN-412", 86)],
			headerName: "SLE Risk",
			description: "how likely this one is to miss",
			workItemTerm: "Work Item",
			workItemsTerm: "Work Items",
			sleTerm: "SLE",
			sleRangeInDays: 0,
		});

		expect(withZeroRange).toBeUndefined();
	});

	test("carries the percentage as the column's value, so the export reads as the column does", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-412"))).toBe("86%");
	});

	test("renders a risk of zero as a number, not as a blank", () => {
		// Zero is a real answer: the item is inside its target and the team has finished nothing that
		// ran this long. It is also falsy, so a label written as a truthiness check type-checks and
		// silently blanks every cell on a thin history - which is the whole population this slice
		// stopped hiding behind a sentinel.
		const descriptor = descriptorFor([answer("ZEN-412", 0)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-412"))).toBe("0%");
		expect(descriptor?.riskFor(zenithItem("ZEN-412"))).toBe(0);
	});

	test("says nothing at all about an item the answer never mentioned", () => {
		// An item the endpoint did not list - it entered the board after the answer was computed, or
		// it is no longer in flight. Inventing a risk for it would be worse than an empty cell.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-999"))).toBe("");
	});

	test("leaves an unmentioned item out of the ordering as well", () => {
		// An empty label parses to nothing and sorts to the bottom in both directions, which is where
		// a row making no claim belongs.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-999"))).toBeUndefined();
		expect(sleRiskSortValue("")).toBeUndefined();
	});

	test("orders a zero below every answered item rather than alongside the unmentioned ones", () => {
		// The distinction the empty string has to keep: no claim sorts nowhere, a claim of zero sorts
		// at the bottom of the claims.
		expect(sleRiskSortValue("0%")).toBe(0);
	});

	test("has no risk to order by for an item the answer never mentioned", () => {
		// The lookup misses, and a miss must read as "no answer" rather than throw on the way to
		// drawing the column.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-999"))).toBeUndefined();
	});

	test("keeps the risk itself available for ordering, not only its spelling", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-412"))).toBe(86);
	});
});

describe("painting the risk", () => {
	const paintedBy = (risk: number) =>
		descriptorFor([answer("ZEN-412", 86)])?.colorForRisk(risk);

	test.each([
		[0, 0],
		[24, 0],
		[25, 1],
		[49, 1],
		[50, 2],
		[74, 2],
		[75, 3],
		[99, 3],
		[100, 4],
	])(
		"paints %i in the same colour the chart paints its band %i",
		(risk, band) => {
			expect(paintedBy(risk)).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[band]);
		},
	);

	test("leaves an item with no answer unpainted", () => {
		// Not the calmest colour. An absence of evidence must not be dressed as good news.
		expect(
			descriptorFor([answer("ZEN-412", 86)])?.colorForRisk(undefined),
		).toBeUndefined();
	});
});

describe("reading the number back out of a rendered label", () => {
	test("recovers the risk from the percentage the column carries", () => {
		expect(sleRiskSortValue("86%")).toBe(86);
	});

	test("has no number for the empty label an unmentioned row renders", () => {
		expect(sleRiskSortValue("")).toBeUndefined();
	});

	test("has no number for a label that was never a percentage", () => {
		expect(sleRiskSortValue("not a number")).toBeUndefined();
	});
});

describe("counting what is at risk", () => {
	test("counts the items seventy percent or worse", () => {
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 86),
			answer("ZEN-2", 70),
			answer("ZEN-3", 69),
			answer("ZEN-4", 12),
		]);

		// Seventy is the line and sits on the at-risk side of it. Sixty-nine is chosen over a round
		// number so the test fails if the comparison is ever written as strictly greater.
		expect(summary.count).toBe(2);
	});

	test("leaves an item that is merely more likely than not out of the count", () => {
		// The line used to be fifty, which counted anything past a coin flip. On a healthy board that
		// is most of the work in progress, and a count that is never small is one nobody reads.
		const summary = sleRiskAtRiskSummary([answer("ZEN-7", 55)]);

		expect(summary.count).toBe(0);
	});

	test("counts an item that has already outlasted its target", () => {
		// Past the target every item that ever ran this long had already missed, so the number is
		// 100 by definition rather than by evidence - and it is the clearest trouble on the board.
		const summary = sleRiskAtRiskSummary([answer("ZEN-5", 100)]);

		expect(summary.count).toBe(1);
	});

	test("does not count an item whose history gives it a zero", () => {
		// A thin history now answers 0 rather than staying silent. Zero is below the threshold and
		// must be treated as the number it is, not as a missing one.
		const summary = sleRiskAtRiskSummary([answer("ZEN-6", 0)]);

		expect(summary.count).toBe(0);
	});

	test("takes its colour from the worst item it counted", () => {
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 75),
			answer("ZEN-2", 99),
		]);

		expect(summary.color).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[3]);
	});

	test("reads an item past its target as the worst band there is", () => {
		// Certainty is the top of the ladder, and an item already past the target is there.
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 75),
			answer("ZEN-5", 100),
		]);

		expect(summary.color).toBe(
			PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1],
		);
	});

	test("has no colour to give when it counted nothing", () => {
		const summary = sleRiskAtRiskSummary([answer("ZEN-3", 12)]);

		expect(summary.count).toBe(0);
		expect(summary.color).toBeUndefined();
	});

	test("counts nothing for a team that published no target", () => {
		// An empty answer is how that arrives, and it is the same shape as having no items.
		expect(sleRiskAtRiskSummary([]).count).toBe(0);
	});
});

describe("what a risk rests on", () => {
	test("states how much of that work missed, so nobody multiplies the percentage by the count", () => {
		// The reason this story exists. A reader given "9" and "67%" has to work out that six of the
		// nine missed, and the arithmetic they do in their head is the arithmetic the sentence owes
		// them.
		expect(sleRiskEvidenceDisclosure(evidence(9, 6))).toBe(
			"9 Work Items the team finished were still open at this age. 6 of them missed the 10 day SLE.",
		);
		expect(sleRiskEvidenceDisclosure(evidence(9, 6, "Ticket", "Tickets"))).toBe(
			"9 Tickets the team finished were still open at this age. 6 of them missed the 10 day SLE.",
		);
	});

	test("names the target with whatever the team calls it, and however long they set it", () => {
		expect(
			sleRiskEvidenceDisclosure({
				...evidence(9, 6),
				sleRangeInDays: 21,
				sleTerm: "Goal",
			}),
		).toContain("21 day Goal");

		// A one-day target reads "1 day", never "1 days".
		expect(
			sleRiskEvidenceDisclosure({ ...evidence(9, 6), sleRangeInDays: 1 }),
		).toContain("the 1 day SLE.");
	});

	test("counts a single miss among several, and the smallest plural", () => {
		// One is the value the singular arm tests for, so a plural row carrying exactly one miss is
		// where a branch written as equality rather than as presence would go wrong. Two is the
		// smallest N that takes the plural arm at all.
		expect(sleRiskEvidenceDisclosure(evidence(9, 1))).toBe(
			"9 Work Items the team finished were still open at this age. 1 of them missed the 10 day SLE.",
		);
		expect(sleRiskEvidenceDisclosure(evidence(2, 2))).toBe(
			"2 Work Items the team finished were still open at this age. 2 of them missed the 10 day SLE.",
		);
	});

	test("does not tell one item it met a target it missed", () => {
		// The singular arm asks whether there was a miss, not whether there was exactly one. Nothing
		// the backend produces can hand it a two - both counts are taken from one list at one age,
		// so the misses are a subset of the total - but a version written as equality would answer
		// "It did not miss" to a two, which is the opposite of the truth rather than a blank.
		expect(sleRiskEvidenceDisclosure(evidence(1, 2))).toContain("It missed");
	});

	test("spells out a clean history rather than leaving a bare zero", () => {
		// "0 of them missed" reads as a placeholder for a number nobody worked out. This team has
		// evidence and all of it met the target, which is worth saying in words.
		expect(sleRiskEvidenceDisclosure(evidence(9, 0))).toBe(
			"9 Work Items the team finished were still open at this age. None of them missed the 10 day SLE.",
		);
	});

	test("speaks of one finished item in the singular", () => {
		expect(sleRiskEvidenceDisclosure(evidence(1, 1))).toBe(
			"1 Work Item the team finished was still open at this age. It missed the 10 day SLE.",
		);
		expect(sleRiskEvidenceDisclosure(evidence(1, 0))).toBe(
			"1 Work Item the team finished was still open at this age. It did not miss the 10 day SLE.",
		);
	});

	test("says a thin history said nothing rather than saying nothing", () => {
		// This is what the withdrawn "beyond history" sentinel used to carry, returned as evidence
		// beside the answer instead of as a substitute for it.
		expect(sleRiskEvidenceDisclosure(evidence(0, 0))).toBe(
			"No Work Item the team finished was ever still open this long.",
		);
	});

	test("explains a certain item by the promise it broke, not by a history it never read", () => {
		// The trap this branch exists for. An item past its target reads 100% because being past the
		// target settles it, and the count beside it is often zero because nothing the team finished
		// ever ran that long. Stating a share there would invite a reader to divide it out and
		// believe the emptiness produced the hundred. The backend withholds the second count on
		// exactly that branch, and the absence is what this sentence reads.
		expect(sleRiskEvidenceDisclosure(evidence(0, null))).toBe(
			"Already past the 10 day SLE.",
		);
		expect(sleRiskEvidenceDisclosure(evidence(18, null))).toBe(
			"Already past the 10 day SLE.",
		);
	});

	// The invariance test below compares two disclosures against each other, which is satisfied by
	// two of anything — including two undefineds. This is what stops it being vacuous: the real
	// descriptor hands back the real sentence for a row the answers do mention.
	test("hands a row's own counts to the sentence", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86, 7, 6)]);

		expect(descriptor?.disclosureFor(zenithItem("ZEN-412"))).toBe(
			"7 Work Items the team finished were still open at this age. 6 of them missed the 10 day SLE.",
		);
	});

	// The sentence may now differ between a certain item and a safe one, but only because the
	// backend hands it different evidence — never because it looked at the percentage. Two rows
	// carrying the same counts and opposite risks must still read identically.
	//
	// Asserted as invariance rather than by banning words like "computed" or "based on". A negative
	// assertion over an open set of words goes stale the moment the wording is edited, and passes
	// for a sentence that implies the derivation without using any of them. This one fails the
	// instant a risk-aware branch appears, which is the only way the implication can get in.
	test("reads the evidence and never the risk", () => {
		const certain = descriptorFor([answer("ZEN-412", 100, 9, 9)]);
		const unremarkable = descriptorFor([answer("ZEN-412", 33, 9, 9)]);

		expect(certain?.disclosureFor(zenithItem("ZEN-412"))).toBe(
			unremarkable?.disclosureFor(zenithItem("ZEN-412")),
		);
	});

	test("offers nothing to disclose for a row the answer never mentioned", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.disclosureFor(zenithItem("ZEN-999"))).toBeUndefined();
	});
});
