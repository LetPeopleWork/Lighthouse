import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import {
	type ConnectionDefaultName,
	type EstimationDefaultName,
	type IConnectionDefaults,
	type IConnectionUsage,
	type IEstimationDefault,
	type IFollowerGroup,
	isEstimationDefaultName,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { WhatChanges, WhyUpdate } from "./UpdateQuestion";

// Stored values and rules are keyed by a field's id, so a field that comes back under a new id is a
// change even when its name and reference are the same. The order of the list still does not matter.
const whatTheFieldsLoad = (fields: IAdditionalFieldDefinition[]): string =>
	fields
		.map((field) =>
			JSON.stringify([field.id, field.displayName, field.reference]),
		)
		.sort((a, b) => a.localeCompare(b))
		.join("\n");

export const fieldsChangedSince = (
	loaded: IAdditionalFieldDefinition[],
	current: IAdditionalFieldDefinition[],
): boolean => whatTheFieldsLoad(loaded) !== whatTheFieldsLoad(current);

const isUsed = (usage: IConnectionUsage): boolean =>
	usage.teams + usage.portfolios > 0;

export type DefaultsGroup = "teams" | "portfolios";

// A default read while updating changes nothing until its followers are updated, so changing it asks
// whether to update them. A default read when a page loads takes effect by itself, so there is nothing to ask.
type ReadWhen = "updating" | "pageLoads";

export const WhereEachDefaultSits: Record<
	ConnectionDefaultName,
	{
		group: DefaultsGroup;
		readWhen: ReadWhen;
		changes?: WhatChanges;
		label: (featureTerm: string) => string;
	}
> = {
	parentOverrideForTeams: {
		group: "teams",
		readWhen: "updating",
		changes: "parents",
		label: () => "Parent Override Field",
	},
	estimationForTeams: {
		group: "teams",
		readWhen: "pageLoads",
		label: () => "Estimation Field",
	},
	parentOverrideForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "parents",
		label: () => "Parent Override Field",
	},
	dependencyFieldForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "dependencies",
		label: () => "Dependency Field",
	},
	sizeEstimateFieldForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "sizes",
		label: () => "Size Estimate Field",
	},
	featureOwnerFieldForPortfolios: {
		group: "portfolios",
		readWhen: "updating",
		changes: "owners",
		label: (featureTerm) => `${featureTerm} Owner Field`,
	},
	estimationForPortfolios: {
		group: "portfolios",
		readWhen: "pageLoads",
		label: () => "Estimation Field",
	},
};

// A server that sends no groups is read as if every owner followed a single default.
const followerGroupsOf = (usage: IConnectionUsage): IFollowerGroup[] =>
	usage.followerGroups ??
	(
		Object.entries(usage.followers ?? {}) as [ConnectionDefaultName, number][]
	).map(([name, count]) => ({ follows: [name], count }));

const followersOfDefault = (
	usage: IConnectionUsage,
	name: ConnectionDefaultName,
): number =>
	followerGroupsOf(usage)
		.filter((group) => group.follows.includes(name))
		.reduce((sum, group) => sum + group.count, 0);

export const changedDefaultsToUpdateFollowersOf = (
	loaded: IConnectionDefaults,
	current: IConnectionDefaults,
	usage: IConnectionUsage,
): ConnectionDefaultName[] =>
	(Object.keys(WhereEachDefaultSits) as ConnectionDefaultName[]).filter(
		(name) =>
			WhereEachDefaultSits[name].readWhen === "updating" &&
			followersOfDefault(usage, name) > 0 &&
			(loaded[name] ?? null) !== (current[name] ?? null),
	);

// The Additional Field a default points at, which for an Estimation default sits inside its bundle.
export const fieldOfDefault = (
	name: ConnectionDefaultName,
	defaults: IConnectionDefaults,
): number | null | undefined =>
	isEstimationDefaultName(name)
		? defaults[name]?.additionalFieldDefinitionId
		: defaults[name];

const describedInOrder = (bundle: IEstimationDefault) => [
	bundle.additionalFieldDefinitionId,
	bundle.unit,
	bundle.useNonNumericEstimation,
	bundle.estimationCategoryValues,
];

const sameEstimation = (
	loaded: IEstimationDefault | null | undefined,
	current: IEstimationDefault | null | undefined,
): boolean =>
	JSON.stringify(loaded ? describedInOrder(loaded) : null) ===
	JSON.stringify(current ? describedInOrder(current) : null);

export const changedEstimationDefaultsWithFollowers = (
	loaded: IConnectionDefaults,
	current: IConnectionDefaults,
	usage: IConnectionUsage,
): EstimationDefaultName[] =>
	(Object.keys(WhereEachDefaultSits) as ConnectionDefaultName[])
		.filter(isEstimationDefaultName)
		.filter(
			(name) =>
				followersOfDefault(usage, name) > 0 &&
				!sameEstimation(loaded[name], current[name]),
		);

// An owner following several of the changed defaults is one owner to update, so it is counted once.
const followersIn = (
	group: DefaultsGroup,
	changed: ConnectionDefaultName[],
	usage: IConnectionUsage,
): number =>
	followerGroupsOf(usage)
		.filter((followers) =>
			followers.follows.some(
				(name) =>
					changed.includes(name) && WhereEachDefaultSits[name].group === group,
			),
		)
		.reduce((sum, followers) => sum + followers.count, 0);

// One question per save: a changed Additional Field updates everyone on the connection, which already
// covers whoever follows a changed default, and a question about updating also saves a changed Estimation
// default. Only a save that changes nothing but Estimation defaults asks just whether to save.
export const whyUpdate = (
	fieldsChanged: boolean,
	changedDefaults: ConnectionDefaultName[],
	changedEstimations: EstimationDefaultName[],
	usage: IConnectionUsage,
	featureTerm: string,
): WhyUpdate | null => {
	if (fieldsChanged && isUsed(usage)) {
		return {
			because: "fields",
			teams: usage.teams,
			portfolios: usage.portfolios,
		};
	}
	if (changedDefaults.length === 0 && changedEstimations.length > 0) {
		return {
			because: "estimation",
			teams: followersIn("teams", changedEstimations, usage),
			portfolios: followersIn("portfolios", changedEstimations, usage),
			defaults: changedEstimations.length,
		};
	}
	if (changedDefaults.length === 0) {
		return null;
	}
	return {
		because: "defaults",
		teams: followersIn("teams", changedDefaults, usage),
		portfolios: followersIn("portfolios", changedDefaults, usage),
		defaults: [
			...new Set(
				changedDefaults.map((name) =>
					WhereEachDefaultSits[name].label(featureTerm),
				),
			),
		],
		changes: [
			...new Set(
				changedDefaults.flatMap(
					(name) => WhereEachDefaultSits[name].changes ?? [],
				),
			),
		],
	};
};
