import { describe, expect, it } from "vitest";
import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import {
	type DialogQueryState,
	dialogAnswerOf,
	dialogQueryStatusOf,
} from "./dialogQueryStatus";

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
	it.each(cases)("%s", (_case, query, status) => {
		expect(dialogQueryStatusOf(query)).toBe(status);
	});
});

describe("what a body of the Delivery dialog is handed of one of its answers", () => {
	it("hands over the answer once it is ready", () => {
		expect(
			dialogAnswerOf({ data: AN_ANSWER, isError: false, isFetching: false }),
		).toEqual({ status: "ready", data: AN_ANSWER });
	});

	it("withholds an earlier answer while it is asked for again, so it is never shown as current", () => {
		expect(
			dialogAnswerOf({ data: AN_ANSWER, isError: false, isFetching: true }),
		).toEqual({ status: "loading", data: undefined });
	});

	it("withholds an earlier answer once asking again has failed", () => {
		expect(
			dialogAnswerOf({ data: AN_ANSWER, isError: true, isFetching: false }),
		).toEqual({ status: "error", data: undefined });
	});
});
