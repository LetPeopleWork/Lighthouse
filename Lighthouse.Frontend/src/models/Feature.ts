import type { IWhenForecast } from "./Forecasts/WhenForecast";
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

export interface DictionaryObject<TValue> {
	readonly [key: number]: TValue;
}

export class Feature implements IFeature {
	name!: string;
	id!: number;
	workItemReference!: string;
	state!: string;
	type!: string;
	lastUpdated!: Date;
	isUsingDefaultFeatureSize!: boolean;
	projects: DictionaryObject<string>;
	remainingWork: DictionaryObject<number>;
	totalWork: { [key: number]: number };
	milestoneLikelihood: DictionaryObject<number>;
	forecasts!: IWhenForecast[];
	url: string | null;
	stateCategory: StateCategory;
	startedDate!: Date;
	closedDate!: Date;
	cycleTime!: number;
	workItemAge!: number;

	constructor(
		name: string,
		id: number,
		workItemReference: string,
		state: string,
		type: string,
		lastUpdated: Date,
		isUsingDefaultFeatureSize: boolean,
		projects: DictionaryObject<string>,
		remainingWork: DictionaryObject<number>,
		totalWork: { [key: number]: number },
		milestoneLikelihood: DictionaryObject<number>,
		forecasts: IWhenForecast[],
		url: string | null,
		stateCategory: StateCategory,
		startedDate: Date,
		closedDate: Date,
		cycleTime: number,
		workItemAge: number,
	) {
		this.name = name;
		this.id = id;
		this.workItemReference = workItemReference;
		this.state = state;
		this.type = type;
		this.lastUpdated = lastUpdated;
		this.isUsingDefaultFeatureSize = isUsingDefaultFeatureSize;
		this.projects = projects;
		this.remainingWork = remainingWork;
		this.totalWork = totalWork;
		this.milestoneLikelihood = milestoneLikelihood;
		this.forecasts = forecasts;
		this.url = url;
		this.stateCategory = stateCategory;
		this.startedDate = startedDate;
		this.closedDate = closedDate;
		this.cycleTime = cycleTime;
		this.workItemAge = workItemAge;
	}

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
}
