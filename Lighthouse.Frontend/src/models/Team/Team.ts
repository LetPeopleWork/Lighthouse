import { z } from "zod";
import {
	EntityReferenceSchema,
	type IEntityReference,
} from "../EntityReference";
import type { IFeatureOwner } from "../IFeatureOwner";

export interface ITeam extends IFeatureOwner {
	portfolios: IEntityReference[];
	featureWip: number;
	useFixedDatesForThroughput: boolean;
	throughputStartDate: Date;
	throughputEndDate: Date;
	workItemTypes: string[];
	hasThroughputBlackoutOverlap: boolean;
	hasForecastFilter: boolean;
}

export const TeamSchema = z.object({
	name: z.string().optional().default(""),
	id: z.number().optional().default(0),
	portfolios: z.array(EntityReferenceSchema).optional().default([]),
	features: z.array(EntityReferenceSchema).optional().default([]),
	tags: z.array(z.string()).optional().default([]),
	workItemTypes: z.array(z.string()).optional().default([]),
	featureWip: z.number().optional().default(0),
	useFixedDatesForThroughput: z.boolean().optional().default(false),
	hasThroughputBlackoutOverlap: z.boolean().optional().default(false),
	hasForecastFilter: z.boolean().optional().default(false),
	serviceLevelExpectationProbability: z.number().optional().default(0),
	serviceLevelExpectationRange: z.number().optional().default(0),
	systemWIPLimit: z.number().optional().default(0),
	lastUpdated: z.coerce.date().optional(),
	throughputStartDate: z.coerce.date().optional(),
	throughputEndDate: z.coerce.date().optional(),
});

export type TeamData = z.infer<typeof TeamSchema>;

export class Team implements ITeam {
	name = "";
	id = 0;

	portfolios: IEntityReference[] = [];
	features: IEntityReference[] = [];

	tags: string[] = [];
	workItemTypes: string[] = [];

	featureWip = 0;

	useFixedDatesForThroughput = false;

	hasThroughputBlackoutOverlap = false;

	hasForecastFilter = false;

	serviceLevelExpectationProbability = 0;
	serviceLevelExpectationRange = 0;

	systemWIPLimit = 0;

	lastUpdated: Date = new Date();
	throughputStartDate: Date = new Date();
	throughputEndDate: Date = new Date();

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromParsed(data: TeamData): Team {
		const team = new Team();
		team.name = data.name;
		team.id = data.id;
		team.portfolios = data.portfolios;
		team.features = data.features;
		team.tags = data.tags;
		team.workItemTypes = data.workItemTypes;
		team.featureWip = data.featureWip;
		team.useFixedDatesForThroughput = data.useFixedDatesForThroughput;
		team.hasThroughputBlackoutOverlap = data.hasThroughputBlackoutOverlap;
		team.hasForecastFilter = data.hasForecastFilter;
		team.serviceLevelExpectationProbability =
			data.serviceLevelExpectationProbability;
		team.serviceLevelExpectationRange = data.serviceLevelExpectationRange;
		team.systemWIPLimit = data.systemWIPLimit;
		if (data.lastUpdated) team.lastUpdated = data.lastUpdated;
		if (data.throughputStartDate)
			team.throughputStartDate = data.throughputStartDate;
		if (data.throughputEndDate) team.throughputEndDate = data.throughputEndDate;
		return team;
	}
}
