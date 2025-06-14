import type { IWorkItem } from "../WorkItem";

export interface IRunChartData {
	history: number;
	workItemsPerUnitOfTime: { [key: number]: IWorkItem[] };
	total: number;
	getValueOnDay(day: number): number;
}

export class RunChartData implements IRunChartData {
	workItemsPerUnitOfTime: { [key: number]: IWorkItem[] };
	history: number;
	total: number;

	constructor(
		workItemsPerUnitOfTime: { [key: number]: IWorkItem[] },
		history: number,
		total: number,
	) {
		this.workItemsPerUnitOfTime = workItemsPerUnitOfTime;
		this.history = history;
		this.total = total;
	}

	getValueOnDay(day: number): number {
		if (day < 0 || day >= this.history) {
			throw new RangeError("Invalid day index.");
		}

		return this.workItemsPerUnitOfTime[day].length;
	}
}
