import type { Feature } from "../Feature";
import type { IFeatureOwner } from "../IFeatureOwner";
import type { IProject, Project } from "../Project/Project";

export interface ITeam extends IFeatureOwner {
	name: string;
	id: number;
	projects: IProject[];
	featureWip: number;
	featuresInProgress: string[];
	lastUpdated: Date;
	throughput: number[];
	useFixedDatesForThroughput: boolean;
	throughputStartDate: Date;
	throughputEndDate: Date;
}

export class Team implements ITeam {
	name!: string;
	id!: number;
	projects!: Project[];
	features!: Feature[];
	featureWip: number;
	featuresInProgress: string[];
	lastUpdated: Date;
	throughput: number[];
	useFixedDatesForThroughput: boolean;
	throughputStartDate: Date;
	throughputEndDate: Date;

	constructor(
		name: string,
		id: number,
		projects: Project[],
		features: Feature[],
		featureWip: number,
		featuresInProgress: string[],
		lastUpdated: Date,
		throughput: number[],
		useFixedDatesForThroughput: boolean,
		throughputStartDate: Date,
		throughputEndDate: Date,
	) {
		this.name = name;
		this.id = id;
		this.projects = projects;
		this.features = features;
		this.featureWip = featureWip;
		this.featuresInProgress = featuresInProgress;
		this.lastUpdated = lastUpdated;
		this.throughput = throughput;
		this.useFixedDatesForThroughput = useFixedDatesForThroughput;
		this.throughputStartDate = throughputStartDate;
		this.throughputEndDate = throughputEndDate;
	}

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
}
