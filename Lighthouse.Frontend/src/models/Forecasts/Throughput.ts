export interface IThroughput {
	history: number;
	getThroughputOnDay(day: number): number;
}

export class Throughput implements IThroughput {
	throughput: number[];

	constructor(throughput: number[]) {
		this.throughput = throughput;
	}

	get history(): number {
		return this.throughput.length;
	}

	getThroughputOnDay(day: number): number {
		if (day < 0 || day >= this.throughput.length) {
			throw new RangeError("Invalid day index.");
		}

		return this.throughput[day];
	}
}
