import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { ICumulativeStateTimeItemRow } from "../../../models/Metrics/CumulativeStateTimeItems";
import type { ICycleTimePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useNamedPercentilesScope } from "./useNamedPercentilesScope";
import { useStateDrillDown } from "./useStateDrillDown";

type AnyMetricsService = IMetricsService<IWorkItem | IFeature>;

interface Deferred<T> {
	promise: Promise<T>;
	resolve: (value: T) => void;
	reject: (reason: unknown) => void;
}

function deferred<T>(): Deferred<T> {
	let resolve!: (value: T) => void;
	let reject!: (reason: unknown) => void;
	const promise = new Promise<T>((res, rej) => {
		resolve = res;
		reject = rej;
	});
	return { promise, resolve, reject };
}

const START = new Date(2026, 6, 1);
const END = new Date(2026, 6, 30);
const EARLIER_START = new Date(2026, 5, 1);
const LATER_END = new Date(2026, 7, 15);

function ownerWithId(id: number): IFeatureOwner {
	return {
		id,
		name: `Owner ${id}`,
		lastUpdated: new Date(2026, 5, 1),
	} as IFeatureOwner;
}

function percentiles(value: number): IPercentileValue[] {
	return [{ percentile: 85, value }];
}

function infoWith(value: number): ICycleTimePercentilesInfo {
	return {
		percentiles: [{ percentile: 85, value }],
	} as unknown as ICycleTimePercentilesInfo;
}

describe("the percentiles the reader scopes to a named cycle time definition", () => {
	interface ScopeProps {
		entityId: number;
		startDate: Date;
		endDate: Date;
	}

	function scopeFor(
		getCycleTimePercentiles: ReturnType<typeof vi.fn>,
		getCycleTimePercentilesInfo: ReturnType<typeof vi.fn>,
	) {
		const metricsService = {
			getCycleTimePercentiles,
			getCycleTimePercentilesInfo,
		} as unknown as AnyMetricsService;
		return renderHook(
			({ entityId, startDate, endDate }: ScopeProps) =>
				useNamedPercentilesScope({
					metricsService,
					entityId,
					startDate,
					endDate,
				}),
			{ initialProps: { entityId: 1, startDate: START, endDate: END } },
		);
	}

	it("shows the answers of the latest selection only, even when an earlier one answers last", async () => {
		const slowValues = deferred<IPercentileValue[]>();
		const slowInfo = deferred<ICycleTimePercentilesInfo>();
		const getValues = vi.fn((_id, _from, _to, definitionId: number) =>
			definitionId === 1
				? slowValues.promise
				: Promise.resolve(percentiles(20)),
		);
		const getInfo = vi.fn((_id, _from, _to, definitionId: number) =>
			definitionId === 1 ? slowInfo.promise : Promise.resolve(infoWith(20)),
		);
		const { result } = scopeFor(getValues, getInfo);

		act(() => result.current.handlePercentilesScopeChange(1));
		act(() => result.current.handlePercentilesScopeChange(2));
		await waitFor(() =>
			expect(result.current.scopedPercentileValues).toEqual(percentiles(20)),
		);
		await act(async () => {
			slowValues.resolve(percentiles(10));
			slowInfo.resolve(infoWith(10));
			await Promise.all([slowValues.promise, slowInfo.promise]);
		});

		expect(result.current.percentilesScopeDefinitionId).toBe(2);
		expect(result.current.scopedPercentileValues).toEqual(percentiles(20));
		expect(result.current.scopedPercentilesInfo).toEqual(infoWith(20));
		expect(getValues).toHaveBeenCalledWith(1, START, END, 1);
		expect(getValues).toHaveBeenCalledWith(1, START, END, 2);
		expect(getInfo).toHaveBeenCalledWith(1, START, END, 2);
	});

	it.each([
		["the percentile values", true],
		["the percentile info", false],
	])(
		"returns to Default when fetching %s for a named definition fails",
		async (_which, valuesFail) => {
			const getValues = vi.fn(() =>
				valuesFail
					? Promise.reject(new Error("boom"))
					: Promise.resolve(percentiles(30)),
			);
			const getInfo = vi.fn(() =>
				valuesFail
					? Promise.resolve(infoWith(30))
					: Promise.reject(new Error("boom")),
			);
			const { result } = scopeFor(getValues, getInfo);

			act(() => result.current.handlePercentilesScopeChange(5));

			await waitFor(() =>
				expect(result.current.percentilesScopeDefinitionId).toBeNull(),
			);
			await act(async () => {
				await Promise.resolve();
			});
			expect(result.current.percentilesScopeDefinitionId).toBeNull();
			expect(result.current.scopedPercentileValues).toBeNull();
			expect(result.current.scopedPercentilesInfo).toBeNull();
		},
	);

	it("returns to Default when the reader picks Default again", async () => {
		const { result } = scopeFor(
			vi.fn().mockResolvedValue(percentiles(40)),
			vi.fn().mockResolvedValue(infoWith(40)),
		);
		act(() => result.current.handlePercentilesScopeChange(3));
		await waitFor(() =>
			expect(result.current.scopedPercentilesInfo).toEqual(infoWith(40)),
		);

		act(() => result.current.handlePercentilesScopeChange(null));

		expect(result.current.percentilesScopeDefinitionId).toBeNull();
		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
	});

	it.each([
		["another entity", { entityId: 2, startDate: START, endDate: END }],
		[
			"a window that starts earlier",
			{ entityId: 1, startDate: EARLIER_START, endDate: END },
		],
		[
			"a window that ends later",
			{ entityId: 1, startDate: START, endDate: LATER_END },
		],
	])(
		"drops a chosen scope and ignores answers still on their way when the reader moves to %s",
		async (_which, next) => {
			const lateValues = deferred<IPercentileValue[]>();
			const lateInfo = deferred<ICycleTimePercentilesInfo>();
			const { result, rerender } = scopeFor(
				vi.fn(() => lateValues.promise),
				vi.fn(() => lateInfo.promise),
			);
			act(() => result.current.handlePercentilesScopeChange(4));
			expect(result.current.percentilesScopeDefinitionId).toBe(4);

			rerender(next);
			await act(async () => {
				lateValues.resolve(percentiles(50));
				lateInfo.resolve(infoWith(50));
				await Promise.all([lateValues.promise, lateInfo.promise]);
			});

			expect(result.current.percentilesScopeDefinitionId).toBeNull();
			expect(result.current.scopedPercentileValues).toBeNull();
			expect(result.current.scopedPercentilesInfo).toBeNull();
		},
	);
});

describe("the Work Items behind a state bar the reader clicks", () => {
	const rows: ICumulativeStateTimeItemRow[] = [
		{
			workItemId: 11,
			referenceId: "ABC-11",
			title: "Slow login",
			type: "Bug",
			state: "Doing",
			stateCategory: "Doing",
			url: "https://tracker.example/ABC-11",
			daysContributed: 1.26,
		},
		{
			workItemId: 12,
			referenceId: "ABC-12",
			title: "Fast logout",
			type: "Story",
			state: "Doing",
			stateCategory: "Doing",
			url: null,
			daysContributed: 2.04,
		},
	];

	interface DrillProps {
		selectedItemIds: number[];
	}

	function drillDownFor(ownerType: "team" | "portfolio") {
		const answer = vi.fn().mockResolvedValue({ state: "Doing", items: rows });
		const metricsService = {
			getCumulativeStateTimeItemsForTeam:
				ownerType === "team" ? answer : vi.fn(),
			getCumulativeStateTimeItemsForPortfolio:
				ownerType === "portfolio" ? answer : vi.fn(),
		} as unknown as AnyMetricsService;
		const queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
		const rendered = renderHook(
			({ selectedItemIds }: DrillProps) =>
				useStateDrillDown({
					metricsService,
					ownerType,
					entity: ownerWithId(7),
					startDate: START,
					endDate: END,
					selectedItemIds,
				}),
			{
				initialProps: { selectedItemIds: [] as number[] },
				wrapper: ({ children }: { children: ReactNode }) =>
					createElement(QueryClientProvider, { client: queryClient }, children),
			},
		);
		return { answer, ...rendered };
	}

	it.each(["team", "portfolio"] as const)(
		"lists the rows the %s's service returns, with their days rounded to one decimal",
		async (ownerType) => {
			const { result, answer } = drillDownFor(ownerType);
			expect(answer).not.toHaveBeenCalled();
			expect(result.current.drillDownOpen).toBe(false);

			act(() => result.current.onBarClick("Doing"));

			await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));
			expect(result.current.drillDownOpen).toBe(true);
			expect(result.current.drillDownState).toBe("Doing");
			expect(answer).toHaveBeenCalledTimes(1);
			expect(answer).toHaveBeenCalledWith(7, "Doing", START, END, undefined);
			expect(result.current.drillDownWorkItems).toHaveLength(2);
			expect(result.current.drillDownWorkItems[0]).toMatchObject({
				id: 11,
				referenceId: "ABC-11",
				name: "Slow login",
				type: "Bug",
				state: "Doing",
				stateCategory: "Doing",
				url: "https://tracker.example/ABC-11",
			});
			const [first, second] = result.current.drillDownWorkItems;
			expect(result.current.drillDownDaysColumn.daysFor(first)).toBe(1.3);
			expect(result.current.drillDownDaysColumn.daysFor(second)).toBe(2);
			expect(
				result.current.drillDownDaysColumn.daysFor({ id: 999 } as IWorkItem),
			).toBe(0);

			act(() => result.current.onClose());

			expect(result.current.drillDownOpen).toBe(false);
		},
	);

	it("asks again with the new Work Item selection when the selection changes while a state is open", async () => {
		const { result, answer, rerender } = drillDownFor("team");
		act(() => result.current.onBarClick("Doing"));
		await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));

		rerender({ selectedItemIds: [11, 12] });

		await waitFor(() => expect(answer).toHaveBeenCalledTimes(2));
		expect(answer).toHaveBeenLastCalledWith(7, "Doing", START, END, [11, 12]);
	});
});
