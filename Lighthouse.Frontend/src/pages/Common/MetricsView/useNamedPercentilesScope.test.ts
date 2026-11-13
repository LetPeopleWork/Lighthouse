import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ICycleTimePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useNamedPercentilesScope } from "./useNamedPercentilesScope";

type Deferred<V> = {
	promise: Promise<V>;
	resolve: (value: V) => void;
	reject: (reason: unknown) => void;
};

function deferred<V>(): Deferred<V> {
	let resolve!: (value: V) => void;
	let reject!: (reason: unknown) => void;
	const promise = new Promise<V>((res, rej) => {
		resolve = res;
		reject = rej;
	});
	return { promise, resolve, reject };
}

const valuesFor = (percentile: number): IPercentileValue[] => [
	{ percentile, value: percentile / 10 },
];
const infoFor = (marker: number) =>
	({ marker }) as unknown as ICycleTimePercentilesInfo;

const startDate = new Date(2026, 0, 1);
const endDate = new Date(2026, 0, 31);

function serviceAnswering(
	values: (id: number) => Promise<IPercentileValue[]>,
	info: (id: number) => Promise<ICycleTimePercentilesInfo>,
) {
	const getCycleTimePercentiles = vi.fn(
		(
			_entityId: number,
			_start: Date,
			_end: Date,
			definitionId?: number | null,
		) => values(definitionId as number),
	);
	const getCycleTimePercentilesInfo = vi.fn(
		(
			_entityId: number,
			_start: Date,
			_end: Date,
			definitionId?: number | null,
		) => info(definitionId as number),
	);
	return {
		service: {
			getCycleTimePercentiles,
			getCycleTimePercentilesInfo,
		} as unknown as IMetricsService<IWorkItem>,
		getCycleTimePercentiles,
		getCycleTimePercentilesInfo,
	};
}

function renderScope(
	service: IMetricsService<IWorkItem>,
	initial: { entityId: number; startDate: Date; endDate: Date } = {
		entityId: 5,
		startDate,
		endDate,
	},
) {
	return renderHook(
		(props: typeof initial) =>
			useNamedPercentilesScope({ metricsService: service, ...props }),
		{ initialProps: initial },
	);
}

const answerAtOnce = () =>
	serviceAnswering(
		(id) => Promise.resolve(valuesFor(id)),
		(id) => Promise.resolve(infoFor(id)),
	);

describe("useNamedPercentilesScope", () => {
	it("starts on the Default window with nothing scoped", () => {
		const { service } = answerAtOnce();

		const { result } = renderScope(service);

		expect(result.current.percentilesScopeDefinitionId).toBeNull();
		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
	});

	it("asks for the named definition on this owner and window and shows its numbers", async () => {
		const { service, getCycleTimePercentiles, getCycleTimePercentilesInfo } =
			answerAtOnce();
		const { result } = renderScope(service);

		act(() => result.current.handlePercentilesScopeChange(3));

		await waitFor(() =>
			expect(result.current.scopedPercentileValues).toEqual(valuesFor(3)),
		);
		expect(result.current.percentilesScopeDefinitionId).toBe(3);
		expect(result.current.scopedPercentilesInfo).toEqual(infoFor(3));
		expect(getCycleTimePercentiles).toHaveBeenCalledExactlyOnceWith(
			5,
			startDate,
			endDate,
			3,
		);
		expect(getCycleTimePercentilesInfo).toHaveBeenCalledExactlyOnceWith(
			5,
			startDate,
			endDate,
			3,
		);
	});

	it("goes back to Default without asking anything when the Default is chosen again", async () => {
		const { service, getCycleTimePercentiles } = answerAtOnce();
		const { result } = renderScope(service);
		act(() => result.current.handlePercentilesScopeChange(3));
		await waitFor(() =>
			expect(result.current.scopedPercentilesInfo).toEqual(infoFor(3)),
		);
		getCycleTimePercentiles.mockClear();

		act(() => result.current.handlePercentilesScopeChange(null));

		expect(result.current.percentilesScopeDefinitionId).toBeNull();
		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
		expect(getCycleTimePercentiles).not.toHaveBeenCalled();
	});

	it("ignores a slow answer for an earlier definition that lands after the current one", async () => {
		const slowValues = deferred<IPercentileValue[]>();
		const slowInfo = deferred<ICycleTimePercentilesInfo>();
		const { service } = serviceAnswering(
			(id) => (id === 1 ? slowValues.promise : Promise.resolve(valuesFor(id))),
			(id) => (id === 1 ? slowInfo.promise : Promise.resolve(infoFor(id))),
		);
		const { result } = renderScope(service);

		act(() => result.current.handlePercentilesScopeChange(1));
		act(() => result.current.handlePercentilesScopeChange(2));
		await waitFor(() =>
			expect(result.current.scopedPercentileValues).toEqual(valuesFor(2)),
		);
		await act(async () => {
			slowValues.resolve(valuesFor(1));
			slowInfo.resolve(infoFor(1));
			await Promise.resolve();
		});

		expect(result.current.scopedPercentileValues).toEqual(valuesFor(2));
		expect(result.current.scopedPercentilesInfo).toEqual(infoFor(2));
		expect(result.current.percentilesScopeDefinitionId).toBe(2);
	});

	it("snaps back to Default when the named numbers cannot be fetched", async () => {
		const { service } = serviceAnswering(
			() => Promise.reject(new Error("no values")),
			() => Promise.reject(new Error("no info")),
		);
		const { result } = renderScope(service);

		act(() => result.current.handlePercentilesScopeChange(4));

		await waitFor(() =>
			expect(result.current.percentilesScopeDefinitionId).toBeNull(),
		);
		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
	});

	it("snaps back to Default when only the info fetch fails and drops the values that did arrive", async () => {
		const { service } = serviceAnswering(
			(id) => Promise.resolve(valuesFor(id)),
			() => Promise.reject(new Error("no info")),
		);
		const { result } = renderScope(service);

		act(() => result.current.handlePercentilesScopeChange(4));

		await waitFor(() =>
			expect(result.current.percentilesScopeDefinitionId).toBeNull(),
		);
		await act(async () => {
			await Promise.resolve();
		});
		expect(result.current.scopedPercentileValues).toBeNull();
	});

	it("does not snap back because an earlier, superseded request failed", async () => {
		const failingValues = deferred<IPercentileValue[]>();
		const failingInfo = deferred<ICycleTimePercentilesInfo>();
		const { service } = serviceAnswering(
			(id) =>
				id === 1 ? failingValues.promise : Promise.resolve(valuesFor(id)),
			(id) => (id === 1 ? failingInfo.promise : Promise.resolve(infoFor(id))),
		);
		const { result } = renderScope(service);
		act(() => result.current.handlePercentilesScopeChange(1));
		act(() => result.current.handlePercentilesScopeChange(2));
		await waitFor(() =>
			expect(result.current.scopedPercentilesInfo).toEqual(infoFor(2)),
		);

		await act(async () => {
			failingValues.reject(new Error("late failure"));
			failingInfo.reject(new Error("late failure"));
			await Promise.resolve();
		});

		expect(result.current.percentilesScopeDefinitionId).toBe(2);
		expect(result.current.scopedPercentileValues).toEqual(valuesFor(2));
	});

	it("drops the scope when the owner changes and asks the new owner on the next choice", async () => {
		const { service, getCycleTimePercentiles } = answerAtOnce();
		const { result, rerender } = renderScope(service);
		act(() => result.current.handlePercentilesScopeChange(3));
		await waitFor(() =>
			expect(result.current.scopedPercentilesInfo).toEqual(infoFor(3)),
		);

		rerender({ entityId: 6, startDate, endDate });

		expect(result.current.percentilesScopeDefinitionId).toBeNull();
		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
		act(() => result.current.handlePercentilesScopeChange(3));
		await waitFor(() =>
			expect(result.current.scopedPercentileValues).toEqual(valuesFor(3)),
		);
		expect(getCycleTimePercentiles).toHaveBeenLastCalledWith(
			6,
			startDate,
			endDate,
			3,
		);
	});

	it("drops the scope when the window changes", async () => {
		const { service } = answerAtOnce();
		const { result, rerender } = renderScope(service);
		act(() => result.current.handlePercentilesScopeChange(3));
		await waitFor(() =>
			expect(result.current.scopedPercentilesInfo).toEqual(infoFor(3)),
		);

		rerender({ entityId: 5, startDate, endDate: new Date(2026, 1, 28) });

		expect(result.current.percentilesScopeDefinitionId).toBeNull();
		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
	});

	it("discards an answer still on its way when the owner changes", async () => {
		const lateValues = deferred<IPercentileValue[]>();
		const lateInfo = deferred<ICycleTimePercentilesInfo>();
		const { service } = serviceAnswering(
			() => lateValues.promise,
			() => lateInfo.promise,
		);
		const { result, rerender } = renderScope(service);
		act(() => result.current.handlePercentilesScopeChange(3));

		rerender({ entityId: 6, startDate, endDate });
		await act(async () => {
			lateValues.resolve(valuesFor(3));
			lateInfo.resolve(infoFor(3));
			await Promise.resolve();
		});

		expect(result.current.scopedPercentileValues).toBeNull();
		expect(result.current.scopedPercentilesInfo).toBeNull();
		expect(result.current.percentilesScopeDefinitionId).toBeNull();
	});
});
