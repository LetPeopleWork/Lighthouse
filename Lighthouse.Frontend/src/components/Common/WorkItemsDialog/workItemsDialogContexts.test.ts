import { describe, expect, it } from "vitest";
import {
	type ColumnId,
	WORK_ITEMS_DIALOG_CONTEXTS,
	type WorkItemsDialogContextId,
} from "./workItemsDialogContexts";

// Which columns each context opens with is pinned through the dialog itself, where a reader sees
// it. These checks only hold the map to the rules every entry has to follow.

// Written as records so the compiler rejects a context or column the types gain or lose without
// this list following.
const contextIds = {
	closedItems: true,
	estimation: true,
	arrivals: true,
	inProgress: true,
	aging: true,
	blocked: true,
	stale: true,
	workDistribution: true,
	featureSize: true,
	featureChildren: true,
	deliveryTimeline: true,
	cumulativeStateTime: true,
	startedAndClosed: true,
} satisfies Record<WorkItemsDialogContextId, true>;

const everyContext = Object.keys(contextIds) as WorkItemsDialogContextId[];

type SingleColumnId = Exclude<ColumnId, `namedCycleTime:${number}`>;

const singleColumnIds = {
	startedDate: true,
	closedDate: true,
	cycleTime: true,
	workItemAge: true,
	ageOrCycleTime: true,
	parent: true,
	blockedSince: true,
	timeInState: true,
	estimate: true,
	size: true,
	forecast: true,
	ageBand: true,
	sleRisk: true,
	warnings: true,
	daysContributed: true,
} satisfies Record<SingleColumnId, true>;

const isKnownColumn = (column: string): boolean =>
	column in singleColumnIds || /^namedCycleTime:\d+$/.test(column);

describe("the rules every context of the Work Items dialog follows", () => {
	// @us-02 @slice-01b @example @contract-shape:pure-function
	it.skip("there is one context per row of the defaults table and no other", () => {
		expect(Object.keys(WORK_ITEMS_DIALOG_CONTEXTS).sort()).toEqual(
			[...everyContext].sort(),
		);
	});

	// @us-02 @slice-01b @example @contract-shape:pure-function
	it.skip.each(everyContext)(
		"%s sorts by one of its own default columns",
		(context) => {
			const defaults = WORK_ITEMS_DIALOG_CONTEXTS[context];

			expect(defaults.visible).toContain(defaults.sortBy);
		},
	);

	// @us-02 @slice-01b @example @contract-shape:pure-function
	it.skip.each(everyContext)(
		"%s names only columns the dialog knows, for its defaults and its sort",
		(context) => {
			const defaults = WORK_ITEMS_DIALOG_CONTEXTS[context];

			const unknown = [...defaults.visible, defaults.sortBy].filter(
				(column) => !isKnownColumn(column),
			);

			expect(unknown).toEqual([]);
		},
	);

	// @us-02 @slice-01b @example @contract-shape:pure-function
	it.skip("only Cumulative Time per State offers nothing beyond its defaults, because its rows carry no real dates or ages", () => {
		const withoutCatalogue = everyContext.filter(
			(context) => !WORK_ITEMS_DIALOG_CONTEXTS[context].catalogue,
		);

		expect(withoutCatalogue).toEqual(["cumulativeStateTime"]);
	});
});
