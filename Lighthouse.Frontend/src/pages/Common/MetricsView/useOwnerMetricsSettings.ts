import { useEffect, useMemo, useState } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ICycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import { parseRuleSet } from "../../../models/WorkItemRules";
import type { WidgetStatus } from "./widgetStatus";

// One shared empty value each, so a page whose settings have not arrived hands its charts the same value on
// every render.
const NO_STATES: string[] = [];
const NO_MAPPINGS: IStateMapping[] = [];
const NO_DEFINITIONS: ICycleTimeDefinition[] = [];

interface OwnerSettings<TSettings> {
	readonly ownerId: number;
	readonly status: WidgetStatus;
	readonly settings?: TSettings;
}

/**
 * The settings a Team's or a Portfolio's metrics page reads, fetched for the owner shown. Every value is the shown
 * owner's own: until its answer arrives, or when it cannot be loaded, the page reads none rather than those of an
 * owner shown before.
 */
export function useOwnerMetricsSettings<TSettings extends IBaseSettings>(
	ownerId: number,
	getSettings: (ownerId: number) => Promise<TSettings>,
	ownerLabel: string,
) {
	const [loaded, setLoaded] = useState<OwnerSettings<TSettings>>();

	useEffect(() => {
		let isCurrentOwner = true;
		getSettings(ownerId).then(
			(settings) => {
				if (isCurrentOwner) {
					setLoaded({ ownerId, status: "ready", settings });
				}
			},
			(error) => {
				console.error(`Error fetching ${ownerLabel} settings:`, error);
				if (isCurrentOwner) {
					setLoaded({ ownerId, status: "error" });
				}
			},
		);
		return () => {
			isCurrentOwner = false;
		};
	}, [ownerId, getSettings, ownerLabel]);

	const ofThisOwner = loaded?.ownerId === ownerId ? loaded : undefined;
	const settings = ofThisOwner?.settings;

	const hasBlockedConfig = useMemo(
		() =>
			(parseRuleSet(settings?.blockedRuleSetJson)?.conditions.length ?? 0) > 0,
		[settings],
	);

	return {
		settings,
		// An older server sends no display list; its Doing list is then the closest thing to the columns.
		doingStates:
			settings?.doingDisplayStates ?? settings?.doingStates ?? NO_STATES,
		doingStatesStatus: ofThisOwner?.status ?? "loading",
		waitStates: settings?.waitStates ?? NO_STATES,
		stateMappings: settings?.stateMappings ?? NO_MAPPINGS,
		cycleTimeDefinitions: settings?.cycleTimeDefinitions ?? NO_DEFINITIONS,
		hasBlockedConfig,
		stalenessThresholdDays: settings?.stalenessThresholdDays,
		blockedStalenessThresholdDays: settings?.blockedStalenessThresholdDays,
	};
}
