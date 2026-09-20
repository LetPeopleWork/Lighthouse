import { z } from "zod";
import {
	EntityReferenceSchema,
	type IEntityReference,
} from "./EntityReference";
import {
	FeatureDependencySchema,
	type IFeatureDependency,
} from "./FeatureDependency";
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
	// When work on this Feature begins, and where that answer came from. Optional for the same reason
	// every other additive field here is: a fixture built before it existed is still a fixture, and an
	// older instance must not crash a newer client.
	startForecast?: IFeatureStart;
	teamForecasts?: IFeatureTeamForecast[];
	// Non-empty means no forecast exists at all, and names the teams to chase.
	teamsWithoutForecast?: string[];
	// The place in the order across the whole instance, supplied by the backend and never counted here.
	position?: number;
	// The move verdict is the SERVER's, never re-derived here. `projects` has already had the Portfolios
	// the caller cannot read removed from it, and is empty for a Feature in no Portfolio at all, so a
	// client-side rule written over it would grant the move in both of those cases.
	canMove?: boolean;
	moveBlockReason?: string;
	blockingPortfolios?: IEntityReference[];
	// Which Features this one is waiting on, named on the row. The server has already left out any link
	// pointing at something it does not hold, so this is not every link drawn in the tracker. Optional
	// for the same reason the other additive fields are: a fixture built before it existed still is one.
	dependsOn?: IFeatureDependency[];

	getRemainingWorkForFeature(): number;
	getRemainingWorkForTeam(id: number): number;
	getTotalWorkForFeature(): number;
	getTotalWorkForTeam(id: number): number;
}

/**
 * Where a Feature's start date came from. Carried rather than worked out from what is missing: "no
 * percentiles because work has started" and "no percentiles because nothing can be forecast" are
 * opposite situations that look identical from the outside.
 */
export type StartDateSource = "Unknown" | "Forecast" | "Observed";

export interface IFeatureStart {
	source: StartDateSource;
	// Set only when the source is Observed. A date here is a fact, not a percentile.
	observedDate?: Date;
	// Filled only when the source is Forecast. Empty otherwise, deliberately.
	percentiles: IWhenForecast[];
}

// One contributing team's share of a Feature, at both ends. Carried by the model and read by nothing
// yet - the table shows one forecast at four percentiles and no expander; the timeline's sub-lanes are
// what these are for.
export interface IFeatureTeamForecast {
	teamId: number;
	startPercentiles: IWhenForecast[];
	completionPercentiles: IWhenForecast[];
}

const WorkByTeamSchema = z.record(z.string(), z.number());

export const FeatureStartSchema = z.object({
	source: z.enum(["Unknown", "Forecast", "Observed"]),
	observedDate: z.coerce
		.date()
		.nullish()
		.transform((value) => value ?? undefined),
	percentiles: z.array(WhenForecastSchema).optional().default([]),
});

export const FeatureTeamForecastSchema = z.object({
	teamId: z.number(),
	startPercentiles: z.array(WhenForecastSchema).optional().default([]),
	completionPercentiles: z.array(WhenForecastSchema).optional().default([]),
});

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
	startForecast: FeatureStartSchema.optional(),
	teamForecasts: z.array(FeatureTeamForecastSchema).optional().default([]),
	teamsWithoutForecast: z.array(z.string()).optional().default([]),
	position: z.number().nullable().optional(),
	canMove: z.boolean().nullable().optional(),
	moveBlockReason: z.string().nullable().optional(),
	blockingPortfolios: z.array(EntityReferenceSchema).optional().default([]),
	dependsOn: z.array(FeatureDependencySchema).optional().default([]),
});

export type FeatureData = z.infer<typeof FeatureSchema>;

/**
 * How a percentile arrives from the backend and how it is held here. Written once because the two
 * forecast columns, the per-team breakdown and the aggregate all decode the same thing, and four
 * copies of one decision is four chances for them to stop agreeing.
 */
const asWhenForecasts = (
	percentiles: z.infer<typeof WhenForecastSchema>[],
): IWhenForecast[] =>
	percentiles.map((percentile) =>
		WhenForecast.new(percentile.probability, percentile.expectedDate),
	);

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
	startForecast?: IFeatureStart;
	teamForecasts: IFeatureTeamForecast[] = [];
	teamsWithoutForecast: string[] = [];
	position?: number;
	canMove?: boolean;
	moveBlockReason?: string;
	blockingPortfolios: IEntityReference[] = [];
	dependsOn: IFeatureDependency[] = [];

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
		feature.forecasts = asWhenForecasts(data.forecasts);
		feature.startForecast = data.startForecast
			? {
					source: data.startForecast.source,
					observedDate: data.startForecast.observedDate,
					percentiles: asWhenForecasts(data.startForecast.percentiles),
				}
			: undefined;
		feature.teamForecasts = data.teamForecasts.map((forTeam) => ({
			teamId: forTeam.teamId,
			startPercentiles: asWhenForecasts(forTeam.startPercentiles),
			completionPercentiles: asWhenForecasts(forTeam.completionPercentiles),
		}));
		feature.teamsWithoutForecast = data.teamsWithoutForecast ?? [];
		feature.position = data.position ?? undefined;
		feature.canMove = data.canMove ?? undefined;
		feature.moveBlockReason = data.moveBlockReason ?? undefined;
		feature.blockingPortfolios = data.blockingPortfolios;
		feature.dependsOn = data.dependsOn;
		return feature;
	}
}
