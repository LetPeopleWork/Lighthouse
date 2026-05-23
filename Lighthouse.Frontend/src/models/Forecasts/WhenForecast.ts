import { plainToInstance, Type } from "class-transformer";
import "reflect-metadata";
import type { IForecast } from "./IForecast";

export interface IWhenForecast extends IForecast {
	expectedDate: Date;
	filterApplied?: boolean;
	excludedSummary?: string;
}

export class WhenForecast implements IWhenForecast {
	probability!: number;

	@Type(() => Date)
	expectedDate: Date = new Date();

	filterApplied?: boolean;
	excludedSummary?: string;

	static fromBackend(data: IWhenForecast): WhenForecast {
		return plainToInstance(WhenForecast, data);
	}

	static new(probability: number, expectedDate: Date): WhenForecast {
		const forecast = new WhenForecast();
		forecast.probability = probability;
		forecast.expectedDate = expectedDate;
		return forecast;
	}
}
