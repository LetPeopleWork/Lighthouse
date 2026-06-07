import { z } from "zod";
import {
	EntityReferenceSchema,
	type IEntityReference,
} from "../EntityReference";
import type { IFeatureOwner } from "../IFeatureOwner";

export interface IPortfolio extends IFeatureOwner {
	involvedTeams: IEntityReference[];
	featureSizeTargetProbability: number;
	featureSizeTargetRange: number;
}

export const PortfolioSchema = z.object({
	name: z.string().optional().default(""),
	id: z.number().optional().default(0),
	features: z.array(EntityReferenceSchema).optional().default([]),
	involvedTeams: z.array(EntityReferenceSchema).optional().default([]),
	tags: z.array(z.string()).optional().default([]),
	serviceLevelExpectationProbability: z.number().optional().default(0),
	serviceLevelExpectationRange: z.number().optional().default(0),
	featureSizeTargetProbability: z.number().optional().default(0),
	featureSizeTargetRange: z.number().optional().default(0),
	systemWIPLimit: z.number().optional().default(0),
	lastUpdated: z.coerce.date().optional(),
});

export type PortfolioData = z.infer<typeof PortfolioSchema>;

export class Portfolio implements IPortfolio {
	name = "";
	id = 0;

	features: IEntityReference[] = [];
	involvedTeams: IEntityReference[] = [];

	tags: string[] = [];

	lastUpdated: Date = new Date();

	serviceLevelExpectationProbability = 0;
	serviceLevelExpectationRange = 0;

	featureSizeTargetProbability = 0;
	featureSizeTargetRange = 0;

	systemWIPLimit = 0;

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromParsed(data: PortfolioData): Portfolio {
		const portfolio = new Portfolio();
		portfolio.name = data.name;
		portfolio.id = data.id;
		portfolio.features = data.features;
		portfolio.involvedTeams = data.involvedTeams;
		portfolio.tags = data.tags;
		portfolio.serviceLevelExpectationProbability =
			data.serviceLevelExpectationProbability;
		portfolio.serviceLevelExpectationRange = data.serviceLevelExpectationRange;
		portfolio.featureSizeTargetProbability = data.featureSizeTargetProbability;
		portfolio.featureSizeTargetRange = data.featureSizeTargetRange;
		portfolio.systemWIPLimit = data.systemWIPLimit;
		if (data.lastUpdated) portfolio.lastUpdated = data.lastUpdated;
		return portfolio;
	}
}
