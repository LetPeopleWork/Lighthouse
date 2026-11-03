import { describe, expect, it } from "vitest";
import type {
	IRefinementRow,
	IRefinementView,
	RefinementStage,
} from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { describeHeading, describeReadyByVotes } from "./refinementHeading";

const TERMS: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEM]: "Work Item",
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.REFINEMENT]: "Refinement",
};
const getTerm = (key: string) => TERMS[key] ?? key;

const rows = (count: number, stage?: RefinementStage): IRefinementRow[] =>
	Array.from(
		{ length: count },
		(_, index) => ({ referenceId: `GR-${index}`, stage }) as IRefinementRow,
	);

const refinementOf = (
	overrides: Partial<IRefinementView>,
	workItems: IRefinementRow[] = [],
): IRefinementView =>
	({ refinementConfigured: true, workItems, ...overrides }) as IRefinementView;

describe("describeHeading", () => {
	it.each([
		[0, "0 Work Items in Refinement"],
		[1, "1 Work Item in Refinement"],
		[2, "2 Work Items in Refinement"],
		[143, "143 Work Items in Refinement"],
		[300, "300 Work Items in Refinement"],
	])("counts %i Work Items of a Team without stage rules", (count, heading) => {
		expect(describeHeading(refinementOf({}, rows(count)), getTerm)).toBe(
			heading,
		);
	});

	it("uses the words the Team renamed", () => {
		const renamed = (key: string) =>
			({
				[TERMINOLOGY_KEYS.WORK_ITEMS]: "Stories",
				[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
			})[key] ?? key;

		expect(describeHeading(refinementOf({}, rows(3)), renamed)).toBe(
			"3 Stories in Grooming",
		);
	});

	it("adds how many are ready by votes", () => {
		expect(
			describeHeading(refinementOf({ readyByVotesCount: 4 }, rows(5)), getTerm),
		).toBe("5 Work Items in Refinement · 4 ready by votes");
	});

	it("reads a Team with stage rules by stage, however many it has", () => {
		const workItems = [
			...rows(300, "Ready"),
			...rows(2, "BeingRefined"),
			...rows(1, "Waiting"),
		];

		expect(
			describeHeading(
				refinementOf(
					{ stagesConfigured: true, readyByVotesCount: 2 },
					workItems,
				),
				getTerm,
			),
		).toBe("300 Ready · 2 Being refined · 1 Waiting");
	});
});

describe("describeReadyByVotes", () => {
	it("says nothing when the count is not known", () => {
		expect(describeReadyByVotes(refinementOf({}))).toBe("");
	});

	it.each([
		[0, " · 0 ready by votes"],
		[7, " · 7 ready by votes"],
	])("says %i ready by votes", (count, said) => {
		expect(
			describeReadyByVotes(refinementOf({ readyByVotesCount: count })),
		).toBe(said);
	});
});
