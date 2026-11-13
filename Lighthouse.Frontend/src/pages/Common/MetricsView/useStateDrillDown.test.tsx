import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { ICumulativeStateTimeItemRow } from "../../../models/Metrics/CumulativeStateTimeItems";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useStateDrillDown } from "./useStateDrillDown";

const startDate = new Date(2026, 0, 1);
const endDate = new Date(2026, 0, 31);
const owner = { id: 9, lastUpdated: new Date(2026, 0, 15) } as IFeatureOwner;

const row: ICumulativeStateTimeItemRow = {
	workItemId: 41,
	referenceId: "WI-41",
	title: "Fix login",
	type: "Bug",
	state: "Review",
	stateCategory: "Doing",
	url: "https://tracker.example/WI-41",
	daysContributed: 2.449,
};

function setup() {
	const forTeam = vi.fn().mockResolvedValue({ state: "Review", items: [row] });
	const forPortfolio = vi
		.fn()
		.mockResolvedValue({ state: "Review", items: [] });
	const service = {
		getCumulativeStateTimeItemsForTeam: forTeam,
		getCumulativeStateTimeItemsForPortfolio: forPortfolio,
	} as unknown as IMetricsService<IWorkItem>;
	const client = new QueryClient();
	const render = (
		ownerType: "team" | "portfolio" = "team",
		selectedItemIds: number[] = [],
	) =>
		renderHook(
			(props: { selectedItemIds: number[] }) =>
				useStateDrillDown({
					metricsService: service,
					ownerType,
					entity: owner,
					startDate,
					endDate,
					selectedItemIds: props.selectedItemIds,
				}),
			{
				initialProps: { selectedItemIds },
				wrapper: ({ children }: { children: ReactNode }) =>
					createElement(QueryClientProvider, { client }, children),
			},
		);
	return { forTeam, forPortfolio, render };
}

describe("useStateDrillDown", () => {
	it("is closed and asks nothing until a bar is clicked", () => {
		const { forTeam, forPortfolio, render } = setup();

		const { result } = render();

		expect(result.current.drillDownOpen).toBe(false);
		expect(result.current.drillDownState).toBeNull();
		expect(result.current.drillDownStatus).toBe("loading");
		expect(result.current.drillDownWorkItems).toEqual([]);
		expect(forTeam).not.toHaveBeenCalled();
		expect(forPortfolio).not.toHaveBeenCalled();
	});

	it("opens on the clicked state and asks the team for its items, all of them when none are selected", async () => {
		const { forTeam, forPortfolio, render } = setup();
		const { result } = render("team", []);

		act(() => result.current.onBarClick("Review"));

		await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));
		expect(result.current.drillDownOpen).toBe(true);
		expect(result.current.drillDownState).toBe("Review");
		expect(forTeam).toHaveBeenCalledExactlyOnceWith(
			9,
			"Review",
			startDate,
			endDate,
			undefined,
		);
		expect(forPortfolio).not.toHaveBeenCalled();
	});

	it("narrows the question to the selected items when there are some", async () => {
		const { forTeam, render } = setup();
		const { result } = render("team", [41, 42]);

		act(() => result.current.onBarClick("Review"));

		await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));
		expect(forTeam).toHaveBeenCalledExactlyOnceWith(
			9,
			"Review",
			startDate,
			endDate,
			[41, 42],
		);
	});

	it("asks the portfolio, not the team, when the owner is a portfolio", async () => {
		const { forTeam, forPortfolio, render } = setup();
		const { result } = render("portfolio");

		act(() => result.current.onBarClick("Review"));

		await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));
		expect(forPortfolio).toHaveBeenCalledExactlyOnceWith(
			9,
			"Review",
			startDate,
			endDate,
			undefined,
		);
		expect(forTeam).not.toHaveBeenCalled();
	});

	it("turns each row into a Work Item that is neither blocked nor dated, and rounds the days to a tenth", async () => {
		const { render } = setup();
		const { result } = render();

		act(() => result.current.onBarClick("Review"));

		await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));
		expect(result.current.drillDownWorkItems).toEqual([
			{
				id: 41,
				referenceId: "WI-41",
				name: "Fix login",
				type: "Bug",
				state: "Review",
				stateCategory: "Doing",
				url: "https://tracker.example/WI-41",
				startedDate: new Date(0),
				closedDate: new Date(0),
				cycleTime: 0,
				workItemAge: 0,
				parentWorkItemReference: "",
				isBlocked: false,
			},
		]);
		const [item] = result.current.drillDownWorkItems;
		expect(result.current.drillDownDaysColumn.daysFor(item)).toBe(2.4);
		expect(
			result.current.drillDownDaysColumn.daysFor({ id: 999 } as IWorkItem),
		).toBe(0);
	});

	it("closes on request but remembers the state, and stops asking once closed", async () => {
		const { forTeam, render } = setup();
		const { result, rerender } = render("team", []);
		act(() => result.current.onBarClick("Review"));
		await waitFor(() => expect(result.current.drillDownStatus).toBe("ready"));

		act(() => result.current.onClose());
		rerender({ selectedItemIds: [41] });

		expect(result.current.drillDownOpen).toBe(false);
		expect(result.current.drillDownState).toBe("Review");
		expect(forTeam).toHaveBeenCalledTimes(1);
	});
});
