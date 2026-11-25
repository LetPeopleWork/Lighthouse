import type { IBaseSettings, SettingSource } from "../Common/BaseSettings";
import type { IEntityReference } from "../EntityReference";

export const DEFAULT_PERCENTILE_HISTORY_DAYS = 90;

export interface IPortfolioSettings extends IBaseSettings {
	owningTeam?: IEntityReference;

	overrideRealChildCountStates: string[];

	usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
	defaultAmountOfWorkItemsPerFeature: number;
	defaultWorkItemPercentile: number;
	percentileHistoryInDays: number;
	sizeEstimateAdditionalFieldDefinitionId: number | null;
	sizeEstimateSource?: SettingSource;
	sizeEstimateDefaultName?: string | null;
	featureOwnerAdditionalFieldDefinitionId: number | null;
	featureOwnerSource?: SettingSource;
	featureOwnerDefaultName?: string | null;
	dependencyOverrideAdditionalFieldDefinitionId: number | null;
	dependencyFieldSource?: SettingSource;
	dependencyFieldDefaultName?: string | null;
	ignoreDependencies: boolean;
	infersDefaultFeatureSize?: boolean;
}

/** A Portfolio saved before the switch existed has no value and reads as on. */
export const infersDefaultFeatureSize = (
	settings: IPortfolioSettings | null | undefined,
): boolean => settings?.infersDefaultFeatureSize ?? true;
