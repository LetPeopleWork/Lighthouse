export enum WriteBackValueSource {
	WorkItemAgeCycleTime = 0,
	FeatureSize = 1,
	ForecastPercentile50 = 2,
	ForecastPercentile70 = 3,
	ForecastPercentile85 = 4,
	ForecastPercentile95 = 5,
	// The number is the stored value, not a label: the backend persists the enum's ordinal, so a
	// member may only ever be appended.
	SleRisk = 6,
}

export enum WriteBackAppliesTo {
	Team = 0,
	Portfolio = 1,
}

export enum WriteBackTargetValueType {
	Date = 0,
	FormattedText = 1,
}

export interface IWriteBackMappingDefinition {
	id: number;
	valueSource: WriteBackValueSource;
	appliesTo: WriteBackAppliesTo;
	additionalFieldDefinitionId: number | null;
	targetValueType: WriteBackTargetValueType;
	dateFormat?: string | null;
}

export const FORECAST_SOURCES: ReadonlySet<WriteBackValueSource> = new Set([
	WriteBackValueSource.ForecastPercentile50,
	WriteBackValueSource.ForecastPercentile70,
	WriteBackValueSource.ForecastPercentile85,
	WriteBackValueSource.ForecastPercentile95,
]);

/**
 * A feature sits in several portfolios, each with its own target and its own history, so its
 * risk has several answers and no way to pick one. The question is only ever asked of a team.
 */
export const TEAM_ONLY_SOURCES: ReadonlySet<WriteBackValueSource> = new Set([
	WriteBackValueSource.SleRisk,
]);

export const PORTFOLIO_ONLY_SOURCES: ReadonlySet<WriteBackValueSource> =
	new Set([
		WriteBackValueSource.FeatureSize,
		WriteBackValueSource.ForecastPercentile50,
		WriteBackValueSource.ForecastPercentile70,
		WriteBackValueSource.ForecastPercentile85,
		WriteBackValueSource.ForecastPercentile95,
	]);

export const DATE_FORMAT_PRESETS: readonly string[] = [
	"yyyy-MM-dd",
	"MM/dd/yyyy",
	"dd.MM.yyyy",
	"dd MMM yyyy",
];

export const VALUE_SOURCE_DISPLAY_NAMES: Readonly<
	Record<WriteBackValueSource, string>
> = {
	[WriteBackValueSource.WorkItemAgeCycleTime]: "Work Item Age/Cycle Time",
	[WriteBackValueSource.FeatureSize]: "Feature Size",
	[WriteBackValueSource.ForecastPercentile50]: "Forecast (50th Percentile)",
	[WriteBackValueSource.ForecastPercentile70]: "Forecast (70th Percentile)",
	[WriteBackValueSource.ForecastPercentile85]: "Forecast (85th Percentile)",
	[WriteBackValueSource.ForecastPercentile95]: "Forecast (95th Percentile)",
	[WriteBackValueSource.SleRisk]: "SLE Risk",
};

export const APPLIES_TO_DISPLAY_NAMES: Readonly<
	Record<WriteBackAppliesTo, string>
> = {
	[WriteBackAppliesTo.Team]: "Team",
	[WriteBackAppliesTo.Portfolio]: "Portfolio",
};
