export interface IRunChartData {
	history: number;
	valuePerUnitOfTime: number[];
	total: number;
	getValueOnDay(day: number): number;
}

export class RunChartData implements IRunChartData {
	valuePerUnitOfTime: number[];
	history: number;
	total: number;

	constructor(valuePerUnitOfTime: number[], history: number, total: number) {
		this.valuePerUnitOfTime = valuePerUnitOfTime;
		this.history = history;
		this.total = total;
	}

	getValueOnDay(day: number): number {
		if (day < 0 || day >= this.valuePerUnitOfTime.length) {
			throw new RangeError("Invalid day index.");
		}

		return this.valuePerUnitOfTime[day];
	}
}
