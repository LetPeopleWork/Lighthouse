import { Transform, Type, plainToInstance } from "class-transformer";
import "reflect-metadata";
import { Feature } from "../Feature";
import type { IFeatureOwner } from "../IFeatureOwner";
import { type IProject, Project } from "../Project/Project";

export interface ITeam extends IFeatureOwner {
	name: string;
	id: number;
	projects: IProject[];
	featureWip: number;
	lastUpdated: Date;
	useFixedDatesForThroughput: boolean;
	throughputStartDate: Date;
	throughputEndDate: Date;
}

export class Team implements ITeam {
	name = "";
	id = 0;

	@Type(() => Project)
	@Transform(({ value }) => value.map(Project.fromBackend), {
		toClassOnly: true,
	})
	projects: Project[] = [];

	@Type(() => Feature)
	@Transform(({ value }) => value.map(Feature.fromBackend), {
		toClassOnly: true,
	})
	features: Feature[] = [];

	tags: string[] = [];

	featureWip = 0;

	useFixedDatesForThroughput = false;

	@Type(() => Date)
	lastUpdated: Date = new Date();

	@Type(() => Date)
	throughputStartDate: Date = new Date();

	@Type(() => Date)
	throughputEndDate: Date = new Date();

	get remainingWork(): number {
		return this.features.reduce(
			(acc, feature) => acc + feature.getRemainingWorkForTeam(this.id),
			0,
		);
	}

	get totalWork(): number {
		return this.features.reduce(
			(acc, feature) => acc + feature.getTotalWorkForTeam(this.id),
			0,
		);
	}

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromBackend(data: ITeam): Team {
		return plainToInstance(Team, data);
	}
}
