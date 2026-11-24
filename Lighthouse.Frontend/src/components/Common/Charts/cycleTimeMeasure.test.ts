import { describe, expect, it } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import {
	DefaultCycleTimeMeasure,
	NamedCycleTimeMeasure,
} from "./cycleTimeMeasure";

const workItem = (
	id: number,
	closedDate: Date,
	cycleTime: number,
	namedCycleTimes: IWorkItem["namedCycleTimes"],
): IWorkItem => ({
	id,
	referenceId: `ITEM-${id}`,
	name: `Item ${id}`,
	url: null,
	state: "Done",
	stateCategory: "Done",
	type: "Task",
	startedDate: new Date(2026, 10, 1),
	closedDate,
	cycleTime,
	namedCycleTimes,
	workItemAge: cycleTime,
	parentWorkItemReference: "",
	isBlocked: false,
});

const analysisToReadyForRelease = new NamedCycleTimeMeasure({
	id: 7,
	name: "Analysis to Ready for Release",
	endState: "Ready for Release",
});

const terms = { workItems: "Work Items", cycleTime: "Cycle Time" };

describe("NamedCycleTimeMeasure", () => {
	it("plots each Work Item with a value for the definition at its days, on the day it reached the end state", () => {
		const reached = workItem(1, new Date(2026, 10, 20), 15, [
			{ definitionId: 8, days: 2, endDate: new Date(2026, 10, 9) },
			{ definitionId: 7, days: 6, endDate: new Date(2026, 10, 5) },
		]);
		const otherDefinitionOnly = workItem(2, new Date(2026, 10, 21), 9, [
			{ definitionId: 8, days: 3, endDate: new Date(2026, 10, 10) },
		]);

		const plotted = analysisToReadyForRelease.plot([
			reached,
			otherDefinitionOnly,
		]);

		expect(
			plotted.map(({ id, cycleTime, plottedDate }) => ({
				id,
				cycleTime,
				plottedDate,
			})),
		).toEqual([{ id: 1, cycleTime: 6, plottedDate: new Date(2026, 10, 5) }]);
	});

	it("names the date by the definition's end state", () => {
		const [dot] = analysisToReadyForRelease.plot([
			workItem(1, new Date(2026, 10, 20), 15, [
				{ definitionId: 7, days: 6, endDate: new Date(2026, 10, 5) },
			]),
		]);

		expect(analysisToReadyForRelease.dateAxisLabel).toBe(
			"Date reached Ready for Release",
		);
		expect(analysisToReadyForRelease.dotListTitle(terms, dot)).toBe(
			`Work Items that reached Ready for Release on ${new Date(2026, 10, 5).toLocaleDateString()} with Cycle Time of 6 days`,
		);
		expect(analysisToReadyForRelease.groupLabel(3, "Work Items")).toBe(
			"3 Work Items that reached Ready for Release",
		);
	});
});

describe("DefaultCycleTimeMeasure", () => {
	it("plots every Work Item at its Cycle Time on the day it closed, and says so", () => {
		const measure = new DefaultCycleTimeMeasure();
		const [dot] = measure.plot([
			workItem(1, new Date(2026, 10, 20), 15, [
				{ definitionId: 7, days: 6, endDate: new Date(2026, 10, 5) },
			]),
		]);

		expect(dot.cycleTime).toBe(15);
		expect(dot.plottedDate).toEqual(new Date(2026, 10, 20));
		expect(measure.dateAxisLabel).toBe("Date");
		expect(measure.dotListTitle(terms, dot)).toBe(
			`Work Items closed on ${new Date(2026, 10, 20).toLocaleDateString()} with Cycle Time of 15 days`,
		);
		expect(measure.groupLabel(2, "Work Items")).toBe("2 Closed Work Items");
	});
});
