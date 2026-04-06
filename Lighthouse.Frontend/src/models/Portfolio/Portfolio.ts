import { plainToInstance, Type } from "class-transformer";
import "reflect-metadata";
import type { IEntityReference } from "../EntityReference";
import type { IFeatureOwner } from "../IFeatureOwner";

export interface IPortfolio extends IFeatureOwner {
	involvedTeams: IEntityReference[];
	featureSizeTargetProbability: number;
	featureSizeTargetRange: number;
}

export class Portfolio implements IPortfolio {
	name!: string;
	id!: number;

	features: IEntityReference[] = [];
	involvedTeams: IEntityReference[] = [];

	tags: string[] = [];

	@Type(() => Date)
	lastUpdated: Date = new Date();

	serviceLevelExpectationProbability = 0;
	serviceLevelExpectationRange = 0;

	featureSizeTargetProbability = 0;
	featureSizeTargetRange = 0;

	systemWIPLimit = 0;

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromBackend(data: IPortfolio): Portfolio {
		return plainToInstance(Portfolio, data);
	}
}
