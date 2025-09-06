import { plainToInstance, Type } from "class-transformer";
import "reflect-metadata";
import type { IEntityReference } from "../EntityReference";
import type { IFeatureOwner } from "../IFeatureOwner";

export interface ITeam extends IFeatureOwner {
	projects: IEntityReference[];
	featureWip: number;
	useFixedDatesForThroughput: boolean;
	throughputStartDate: Date;
	throughputEndDate: Date;
	workItemTypes: string[];
}

export class Team implements ITeam {
	name = "";
	id = 0;

	projects: IEntityReference[] = [];
	features: IEntityReference[] = [];

	tags: string[] = [];
	workItemTypes: string[] = [];

	featureWip = 0;

	useFixedDatesForThroughput = false;

	serviceLevelExpectationProbability = 0;
	serviceLevelExpectationRange = 0;

	systemWIPLimit = 0;

	@Type(() => Date)
	lastUpdated: Date = new Date();

	@Type(() => Date)
	throughputStartDate: Date = new Date();

	@Type(() => Date)
	throughputEndDate: Date = new Date();

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromBackend(data: ITeam): Team {
		return plainToInstance(Team, data);
	}
}
