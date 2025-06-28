import { plainToInstance, Transform, Type } from "class-transformer";
import "reflect-metadata";

import { type IWhenForecast, WhenForecast } from "./Forecasts/WhenForecast";
import type { IWorkItem, StateCategory } from "./WorkItem";

export interface IFeature extends IWorkItem {
	lastUpdated: Date;
	isUsingDefaultFeatureSize: boolean;
	remainingWork: { [key: number]: number };
	totalWork: { [key: number]: number };
	milestoneLikelihood: { [key: number]: number };
	projects: { [key: number]: string };
	forecasts: IWhenForecast[];

	getRemainingWorkForFeature(): number;
	getRemainingWorkForTeam(id: number): number;
	getTotalWorkForFeature(): number;
	getTotalWorkForTeam(id: number): number;
	getMilestoneLikelihood(milestoneId: number): number;
}

class DictionaryObject<TValue> {
	readonly [key: number]: TValue;
}

export class Feature implements IFeature {
	name!: string;
	id!: number;
	referenceId!: string;
	state!: string;
	type!: string;
	lastUpdated!: Date;
	isUsingDefaultFeatureSize!: boolean;
	parentWorkItemReference!: string;

	@Type(() => DictionaryObject<string>)
	projects: DictionaryObject<string> = {};

	@Type(() => DictionaryObject<number>)
	remainingWork: DictionaryObject<number> = {};

	@Type(() => DictionaryObject<number>)
	totalWork: { [key: number]: number } = {};

	@Type(() => DictionaryObject<number>)
	milestoneLikelihood: DictionaryObject<number> = {};

	@Type(() => WhenForecast)
	@Transform(({ value }) => value.map(WhenForecast.fromBackend), {
		toClassOnly: true,
	})
	forecasts!: IWhenForecast[];

	url = "";
	stateCategory: StateCategory = "Unknown";

	@Type(() => Date)
	startedDate: Date = new Date();

	@Type(() => Date)
	closedDate: Date = new Date();

	cycleTime!: number;
	workItemAge!: number;

	getRemainingWorkForTeam(id: number): number {
		return this.getWorkForTeam(id, this.remainingWork);
	}

	getTotalWorkForTeam(id: number): number {
		return this.getWorkForTeam(id, this.totalWork);
	}

	getCompletionPercentageForTeam(id: number): number {
		return Number.parseFloat(
			(
				(100 / this.getTotalWorkForTeam(id)) *
				this.getRemainingWorkForTeam(id)
			).toFixed(2),
		);
	}

	getRemainingWorkForFeature(): number {
		return this.getAllWork(this.remainingWork);
	}

	getTotalWorkForFeature(): number {
		return this.getAllWork(this.totalWork);
	}

	getCompletionPercentageForFeature(): number {
		return Number.parseFloat(
			(
				(100 / this.getTotalWorkForFeature()) *
				this.getRemainingWorkForFeature()
			).toFixed(2),
		);
	}

	getMilestoneLikelihood(milestoneId: number) {
		return this.milestoneLikelihood[milestoneId] ?? 0;
	}

	getAllWork(work: { [key: number]: number }): number {
		if (!work) return 0;

		let totalWork = 0;
		const values = Object.values(work);

		for (const work of values) {
			totalWork += work;
		}

		return totalWork;
	}

	getWorkForTeam(id: number, work: { [key: number]: number }): number {
		if (!work) return 0;
		return work[id] ?? 0;
	}

	static fromBackend(data: IFeature): Feature {
		return plainToInstance(Feature, data);
	}
}
