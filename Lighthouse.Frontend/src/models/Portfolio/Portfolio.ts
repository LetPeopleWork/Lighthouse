import { plainToInstance, Transform, Type } from "class-transformer";
import "reflect-metadata";
import type { IEntityReference } from "../EntityReference";
import { type IWhenForecast, WhenForecast } from "../Forecasts/WhenForecast";
import type { IFeatureOwner } from "../IFeatureOwner";

export interface IPortfolio extends IFeatureOwner {
	involvedTeams: IEntityReference[];
	totalWorkItems: number;
	remainingWorkItems: number;
	forecasts: IWhenForecast[];
}

export class Portfolio implements IPortfolio {
	name!: string;
	id!: number;

	features: IEntityReference[] = [];
	involvedTeams: IEntityReference[] = [];

	tags: string[] = [];

	totalWorkItems: number = 0;
	remainingWorkItems: number = 0;

	@Transform(({ value }) => value.map(WhenForecast.fromBackend), {
		toClassOnly: true,
	})
	forecasts: IWhenForecast[] = [];

	@Type(() => Date)
	lastUpdated: Date = new Date();

	serviceLevelExpectationProbability = 0;
	serviceLevelExpectationRange = 0;

	systemWIPLimit = 0;

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromBackend(data: IPortfolio): Portfolio {
		return plainToInstance(Portfolio, data);
	}
}
