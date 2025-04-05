export interface IThroughput {
	history: number;
	throughputPerUnitOfTime: number[];
	totalThroughput: number;
	getThroughputOnDay(day: number): number;
}

export class Throughput implements IThroughput {
	throughputPerUnitOfTime: number[];
	history: number;
	totalThroughput: number;

	constructor(
		throughputPerUnitOfTime: number[],
		history: number,
		totalThroughput: number,
	) {
		this.throughputPerUnitOfTime = throughputPerUnitOfTime;
		this.history = history;
		this.totalThroughput = totalThroughput;
	}

	getThroughputOnDay(day: number): number {
		if (day < 0 || day >= this.throughputPerUnitOfTime.length) {
			throw new RangeError("Invalid day index.");
		}

		return this.throughputPerUnitOfTime[day];
	}
}
