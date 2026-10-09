import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import {
	type CategoryKey,
	getWidgetsForCategory,
	type MetricsFetchKey,
} from "./categoryMetadata";
import {
	combineWidgetStatuses,
	type FetchKeyState,
	type FetchKeyStates,
	fetchKeyStateOf,
	type QueryProgress,
	useReportWidgetStatus,
	type WidgetStatus,
	widgetHasData,
	widgetStatusFor,
} from "./widgetStatus";

const answered: FetchKeyState = { status: "ready", hasData: true };
const behindWithOldData: FetchKeyState = { status: "loading", hasData: true };
const neverAnswered: FetchKeyState = { status: "loading", hasData: false };
const failed: FetchKeyState = { status: "error", hasData: false };

const onItsWay: QueryProgress = {
	isPending: true,
	isError: false,
	isPlaceholderData: false,
};
const showingThePreviousWindow: QueryProgress = {
	isPending: false,
	isError: false,
	isPlaceholderData: true,
};
const failedQuery: QueryProgress = {
	isPending: false,
	isError: true,
	isPlaceholderData: false,
};
const answeredQuery: QueryProgress = {
	isPending: false,
	isError: false,
	isPlaceholderData: false,
};

function allAnswered(keys: readonly MetricsFetchKey[]): FetchKeyStates {
	return Object.fromEntries(keys.map((key) => [key, answered]));
}

function permutationsOf<T>(items: readonly T[]): T[][] {
	if (items.length <= 1) return [[...items]];
	return items.flatMap((item, index) =>
		permutationsOf([...items.slice(0, index), ...items.slice(index + 1)]).map(
			(rest) => [item, ...rest],
		),
	);
}

const everyCategory: readonly CategoryKey[] = [
	"flow-overview",
	"flow-metrics",
	"predictability",
	"portfolio",
];

const everyPlacedWidget = [
	...new Set(
		everyCategory.flatMap((category) =>
			(["team", "portfolio"] as const).flatMap((owner) =>
				getWidgetsForCategory(category, owner).map((w) => w.widgetKey),
			),
		),
	),
];

describe("a widget's status, from the things it waits on", () => {
	it.skip.each<[readonly WidgetStatus[], WidgetStatus]>([
		[[], "ready"],
		[["ready", "ready"], "ready"],
		[["ready", "error"], "error"],
		[["error", "error"], "error"],
		[["loading", "ready"], "loading"],
		[["error", "loading"], "loading"],
		[["loading", "error", "ready"], "loading"],
	])("waiting on %j reads as %s", (statuses, expected) => {
		expect(combineWidgetStatuses(statuses)).toBe(expected);
	});

	it.skip.each<[readonly WidgetStatus[], WidgetStatus]>([
		[["loading", "error", "ready"], "loading"],
		[["error", "ready", "ready"], "error"],
	])(
		"reads the same whichever order %j are listed in",
		(statuses, expected) => {
			for (const order of permutationsOf(statuses)) {
				expect(combineWidgetStatuses(order)).toBe(expected);
			}
		},
	);
});

describe("which widgets are behind the selected window", () => {
	it.skip.each(everyPlacedWidget)(
		"%s is loading while a stepped window is still waiting to be committed",
		(widgetKey) => {
			const everythingAnswered = allAnswered([
				"throughput",
				"arrivals",
				"inProgressItems",
				"cycleTimeData",
				"cycleTimePercentiles",
			]);

			expect(widgetStatusFor(widgetKey, everythingAnswered, true)).toBe(
				"loading",
			);
		},
	);

	it.skip("a chart is loading while any one of its inputs has not answered the selected window", () => {
		const states: FetchKeyStates = {
			arrivals: answered,
			throughput: behindWithOldData,
		};

		expect(widgetStatusFor("arrivals", states, false)).toBe("loading");
	});

	it.skip("a chart shows it failed only once nothing else it waits on is still on its way", () => {
		const oneFailedOneOnItsWay: FetchKeyStates = {
			cycleTimePercentiles: failed,
			cycleTimeData: behindWithOldData,
			blackoutPeriods: answered,
		};
		expect(widgetStatusFor("cycleScatter", oneFailedOneOnItsWay, false)).toBe(
			"loading",
		);

		const restAnswered: FetchKeyStates = {
			...oneFailedOneOnItsWay,
			cycleTimeData: answered,
		};
		expect(widgetStatusFor("cycleScatter", restAnswered, false)).toBe("error");
	});

	it.skip("a failure in something a chart does not show leaves that chart ready", () => {
		const cycleTimePercentilesFailed: FetchKeyStates = {
			cycleTimePercentiles: failed,
			workItemAgePercentiles: answered,
			inProgressItems: answered,
		};

		expect(
			widgetStatusFor(
				"workItemAgePercentiles",
				cycleTimePercentilesFailed,
				false,
			),
		).toBe("ready");
	});

	it.skip.each(["percentilesOverTime", "pbcOverTime"])(
		"%s fetches for itself, so the page alone holds it back only while a step is pending",
		(widgetKey) => {
			expect(widgetStatusFor(widgetKey, {}, false)).toBe("ready");
			expect(widgetStatusFor(widgetKey, {}, true)).toBe("loading");
		},
	);
});

describe("whether a chart has an older picture to dim", () => {
	it.skip("has none on a first visit, before anything it shows has answered", () => {
		expect(
			widgetHasData("arrivals", {
				arrivals: neverAnswered,
				throughput: neverAnswered,
			}),
		).toBe(false);
	});

	it.skip("has one once everything it shows has answered some window, even while it is behind again", () => {
		expect(
			widgetHasData("arrivals", {
				arrivals: behindWithOldData,
				throughput: answered,
			}),
		).toBe(true);
	});

	it.skip("has none while any one of its inputs has never answered", () => {
		expect(
			widgetHasData("arrivals", {
				arrivals: answered,
				throughput: neverAnswered,
			}),
		).toBe(false);
	});
});

describe("one request's progress, as a chart sees it", () => {
	it.skip.each<[string, readonly QueryProgress[], FetchKeyState["status"]]>([
		["on its way", [onItsWay], "loading"],
		[
			"still showing the previous window",
			[showingThePreviousWindow],
			"loading",
		],
		["failed", [failedQuery], "error"],
		["answered", [answeredQuery], "ready"],
		[
			"answered beside one still on its way",
			[answeredQuery, onItsWay],
			"loading",
		],
		["answered beside one that failed", [answeredQuery, failedQuery], "error"],
		["failed beside one still on its way", [failedQuery, onItsWay], "loading"],
	])("a request %s reads as %s", (_label, queries, expected) => {
		expect(fetchKeyStateOf(queries, true).status).toBe(expected);
	});

	it.skip("a request still showing the previous window has an older picture to dim", () => {
		expect(fetchKeyStateOf([showingThePreviousWindow], true).hasData).toBe(
			true,
		);
	});

	it.skip("a request on its way for the first time has nothing to dim", () => {
		expect(fetchKeyStateOf([onItsWay], true).hasData).toBe(false);
	});

	it.skip.each<[string, QueryProgress]>([
		["on its way", onItsWay],
		["showing the previous window", showingThePreviousWindow],
		["failed", failedQuery],
		["answered", answeredQuery],
	])(
		"something this kind of owner never asks for is ready, even when its request reads %s",
		(_label, progress) => {
			expect(fetchKeyStateOf([progress], false).status).toBe("ready");
		},
	);
});

describe("a chart that fetches for itself", () => {
	it.skip("reports nothing, and breaks nothing, when it is drawn outside a dashboard frame", () => {
		expect(() =>
			renderHook(() => useReportWidgetStatus("loading")),
		).not.toThrow();
	});
});
