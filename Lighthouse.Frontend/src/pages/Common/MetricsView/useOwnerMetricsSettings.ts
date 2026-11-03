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

interface DoingColumns {
	readonly ownerId: number;
	readonly status: WidgetStatus;
	readonly states: string[];
}

/**
 * The settings a Team's or a Portfolio's metrics page reads, fetched for the owner shown. The Doing columns are
 * only ever the shown owner's own; the other values stay those of the last owner loaded until the next arrive.
 */
export function useOwnerMetricsSettings<TSettings extends IBaseSettings>(
	ownerId: number,
	getSettings: (ownerId: number) => Promise<TSettings>,
	ownerLabel: string,
) {
	const [settings, setSettings] = useState<TSettings>();
	const [doingColumns, setDoingColumns] = useState<DoingColumns>();

	useEffect(() => {
		let isCurrentOwner = true;
		const fetchSettings = async () => {
			try {
				const loaded = await getSettings(ownerId);
				if (!isCurrentOwner) return;
				setDoingColumns({
					ownerId,
					status: "ready",
					states: loaded.doingDisplayStates ?? [],
				});
				setSettings(loaded);
			} catch (error) {
				console.error(`Error fetching ${ownerLabel} settings:`, error);
				if (isCurrentOwner) {
					setDoingColumns({ ownerId, status: "error", states: [] });
				}
			}
		};

		fetchSettings().catch((error) => {
			console.error(`Error fetching ${ownerLabel} settings:`, error);
		});
		return () => {
			isCurrentOwner = false;
		};
	}, [ownerId, getSettings, ownerLabel]);

	const hasBlockedConfig = useMemo(
		() =>
			(parseRuleSet(settings?.blockedRuleSetJson)?.conditions.length ?? 0) > 0,
		[settings],
	);

	// Until this owner's own answer arrives, the columns of an owner shown before are not its columns.
	const columnsOfThisOwner =
		doingColumns?.ownerId === ownerId ? doingColumns : undefined;

	return {
		settings,
		doingStates: columnsOfThisOwner?.states ?? NO_STATES,
		doingStatesStatus: columnsOfThisOwner?.status ?? "loading",
		waitStates: settings?.waitStates ?? NO_STATES,
		stateMappings: settings ? settings.stateMappings : NO_MAPPINGS,
		cycleTimeDefinitions: settings?.cycleTimeDefinitions ?? NO_DEFINITIONS,
		hasBlockedConfig,
		stalenessThresholdDays: settings?.stalenessThresholdDays,
		blockedStalenessThresholdDays: settings?.blockedStalenessThresholdDays,
	};
}
