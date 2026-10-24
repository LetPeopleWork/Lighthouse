import { describe, expect, it } from "vitest";
import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import {
	type DialogQueryState,
	dialogQueryStatusOf,
} from "./useDeliveryFormQueries";

const AN_ANSWER = ["Checkout redesign"];

const cases: [string, DialogQueryState, WidgetStatus][] = [
	[
		"a query not sent yet, with nothing and no failure, is loading",
		{ data: undefined, isError: false, isFetching: false },
		"loading",
	],
	[
		"a first request on its way is loading",
		{ data: undefined, isError: false, isFetching: true },
		"loading",
	],
	[
		"an answer that has arrived is ready",
		{ data: AN_ANSWER, isError: false, isFetching: false },
		"ready",
	],
	[
		"an answer being asked for again is loading, never shown as current",
		{ data: AN_ANSWER, isError: false, isFetching: true },
		"loading",
	],
	[
		"a request that failed is an error",
		{ data: undefined, isError: true, isFetching: false },
		"error",
	],
	[
		"a failed request being asked again is loading",
		{ data: undefined, isError: true, isFetching: true },
		"loading",
	],
	[
		"a failure after an earlier answer is an error, not the earlier answer",
		{ data: AN_ANSWER, isError: true, isFetching: false },
		"error",
	],
	[
		"an empty list that has arrived is ready",
		{ data: [], isError: false, isFetching: false },
		"ready",
	],
];

describe("what a body of the Delivery dialog says about one of its answers", () => {
	it.skip.each(cases)("%s", (_case, query, status) => {
		expect(dialogQueryStatusOf(query)).toBe(status);
	});
});
