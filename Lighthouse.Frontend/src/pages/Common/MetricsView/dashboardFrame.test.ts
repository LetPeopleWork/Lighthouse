import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import type { FetchKeyStates, WidgetStatus } from "./widgetStatus";

interface DashboardFrame {
	isPlaced(
		widgetKey: string,
		node: ReactNode,
		fetchStates: FetchKeyStates,
	): boolean;
	columnsStatusOf(
		widgetKey: string,
		doingStatesStatus: WidgetStatus,
	): WidgetStatus;
	frameStateOf(
		widgetKey: string,
		fetchStates: FetchKeyStates,
		isCommitPending: boolean,
		columnsStatus: WidgetStatus,
	): { status: WidgetStatus; hasContentToDim: boolean };
	bodyOf(
		widgetKey: string,
		node: ReactNode,
		fetchStates: FetchKeyStates,
		columnsStatus: WidgetStatus,
	): ReactNode;
}

const dashboardFrameModule = "./dashboardFrame.ts";

/** Loads the frame helpers; a missing module fails the row that asked for them, not the whole file. */
const loadFrame = async (): Promise<DashboardFrame> => {
	try {
		return (await import(
			/* @vite-ignore */ dashboardFrameModule
		)) as DashboardFrame;
	} catch {
		throw new Error(
			"The frame helpers are not in dashboardFrame.ts yet. Move isPlaced, columnsStatusOf, frameStateOf and bodyOf there unchanged and export them, so the dashboard frame rules can be checked without rendering the metrics view.",
		);
	}
};

const estimation = "estimationVsCycleTime";
const answered = (status: WidgetStatus, hasData = true): FetchKeyStates => ({
	estimationVsCycleTime: { status, hasData },
});
const chart = "a chart" as ReactNode;

describe("the dashboard frame rules", () => {
	describe("which charts get a frame", () => {
		it("holds back Estimation vs Cycle Time until it has a chart or could not load", async () => {
			const { isPlaced } = await loadFrame();

			expect(isPlaced(estimation, null, answered("ready"))).toBe(false);
			expect(isPlaced(estimation, null, answered("loading", false))).toBe(
				false,
			);
			expect(isPlaced(estimation, chart, answered("ready"))).toBe(true);
			expect(isPlaced(estimation, null, answered("error", false))).toBe(true);
		});

		it("places every other chart from the first render, even with nothing to show", async () => {
			const { isPlaced } = await loadFrame();

			expect(isPlaced("throughput", null, {})).toBe(true);
			expect(
				isPlaced("aging", null, {
					cycleTimeData: { status: "loading", hasData: false },
				}),
			).toBe(true);
		});
	});

	describe("what the frame says about its data", () => {
		it("waits on the Doing states only for the aging chart", async () => {
			const { columnsStatusOf } = await loadFrame();

			expect(columnsStatusOf("aging", "loading")).toBe("loading");
			expect(columnsStatusOf("aging", "error")).toBe("error");
			expect(columnsStatusOf("throughput", "loading")).toBe("ready");
		});

		it("reports the worst of the data and the columns, and dims only a chart that has an older picture", async () => {
			const { frameStateOf } = await loadFrame();

			expect(
				frameStateOf(estimation, answered("ready"), false, "ready"),
			).toEqual({ status: "ready", hasContentToDim: true });
			expect(
				frameStateOf(estimation, answered("error"), false, "ready").status,
			).toBe("error");
			expect(
				frameStateOf(estimation, answered("ready"), false, "loading"),
			).toEqual({ status: "loading", hasContentToDim: false });
			expect(
				frameStateOf(estimation, answered("ready"), true, "ready").status,
			).toBe("loading");
			expect(
				frameStateOf(estimation, answered("loading", false), false, "ready"),
			).toEqual({ status: "loading", hasContentToDim: false });
		});

		it("shows the chart only once its data and columns have arrived", async () => {
			const { bodyOf } = await loadFrame();

			expect(bodyOf(estimation, chart, answered("ready"), "ready")).toBe(chart);
			expect(
				bodyOf(estimation, chart, answered("loading", false), "ready"),
			).toBeNull();
			expect(
				bodyOf(estimation, chart, answered("ready"), "loading"),
			).toBeNull();
		});
	});
});
