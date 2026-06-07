import { z } from "zod";
import {
	EntityReferenceSchema,
	type IEntityReference,
} from "./EntityReference";
import { WhenForecastSchema } from "./Forecasts/forecastSchemas";
import { type IWhenForecast, WhenForecast } from "./Forecasts/WhenForecast";
import type { IWorkItem, StateCategory } from "./WorkItem";

export interface IFeature extends IWorkItem {
	lastUpdated: Date;
	isUsingDefaultFeatureSize: boolean;
	size: number;
	owningTeam: string;
	remainingWork: { [key: number]: number };
	totalWork: { [key: number]: number };
	projects: IEntityReference[];
	forecasts: IWhenForecast[];

	getRemainingWorkForFeature(): number;
	getRemainingWorkForTeam(id: number): number;
	getTotalWorkForFeature(): number;
	getTotalWorkForTeam(id: number): number;
}

const WorkByTeamSchema = z.record(z.string(), z.number());

export const FeatureSchema = z.object({
	name: z.string(),
	id: z.number(),
	referenceId: z.string(),
	state: z.string(),
	type: z.string(),
	stateCategory: z.enum(["Unknown", "ToDo", "Doing", "Done"]),
	lastUpdated: z.coerce.date(),
	startedDate: z.coerce.date().nullable(),
	closedDate: z.coerce.date().nullable(),
	cycleTime: z.number(),
	workItemAge: z.number(),
	size: z.number(),
	owningTeam: z.string(),
	isUsingDefaultFeatureSize: z.boolean(),
	parentWorkItemReference: z.string(),
	isBlocked: z.boolean().optional().default(false),
	url: z.string().nullable().optional(),
	projects: z.array(EntityReferenceSchema).optional().default([]),
	remainingWork: WorkByTeamSchema,
	totalWork: WorkByTeamSchema,
	forecasts: z.array(WhenForecastSchema),
});

export type FeatureData = z.infer<typeof FeatureSchema>;

export class Feature implements IFeature {
	name!: string;
	id!: number;
	referenceId!: string;
	state!: string;
	type!: string;
	lastUpdated!: Date;
	isUsingDefaultFeatureSize!: boolean;
	parentWorkItemReference!: string;
	isBlocked!: boolean;

	projects: IEntityReference[] = [];
	remainingWork: { [key: number]: number } = {};
	totalWork: { [key: number]: number } = {};
	forecasts: IWhenForecast[] = [];

	owningTeam!: string;

	url = "";
	stateCategory: StateCategory = "Unknown";

	startedDate: Date = new Date();
	closedDate: Date = new Date();

	cycleTime!: number;
	workItemAge!: number;
	size!: number;

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

	static fromParsed(data: FeatureData): Feature {
		const feature = new Feature();
		feature.name = data.name;
		feature.id = data.id;
		feature.referenceId = data.referenceId;
		feature.state = data.state;
		feature.type = data.type;
		feature.stateCategory = data.stateCategory;
		feature.lastUpdated = data.lastUpdated;
		// Backend StartedDate/ClosedDate are DateTime? — null for not-started/not-closed
		// items. IWorkItem types them as Date but consumers rely on the runtime null
		// (e.g. BaseMetricsView's `closedDate === null`), so preserve it rather than
		// coercing to epoch.
		feature.startedDate = data.startedDate as Date;
		feature.closedDate = data.closedDate as Date;
		feature.cycleTime = data.cycleTime;
		feature.workItemAge = data.workItemAge;
		feature.size = data.size;
		feature.owningTeam = data.owningTeam;
		feature.isUsingDefaultFeatureSize = data.isUsingDefaultFeatureSize;
		feature.parentWorkItemReference = data.parentWorkItemReference;
		feature.isBlocked = data.isBlocked;
		feature.url = data.url ?? "";
		feature.projects = data.projects;
		feature.remainingWork = data.remainingWork;
		feature.totalWork = data.totalWork;
		feature.forecasts = data.forecasts.map((forecast) =>
			WhenForecast.new(forecast.probability, forecast.expectedDate),
		);
		return feature;
	}
}
