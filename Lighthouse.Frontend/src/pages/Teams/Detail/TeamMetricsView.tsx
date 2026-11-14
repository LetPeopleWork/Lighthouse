import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { EvaluatorCondition } from "../../../components/Common/Charts/ThroughputChart/evaluateCondition";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";
import { useOwnerMetricsSettings } from "../../Common/MetricsView/useOwnerMetricsSettings";

const parseForecastFilterConditions = (
	json: string | null | undefined,
): readonly EvaluatorCondition[] => {
	if (!json || json.trim() === "") {
		return [];
	}
	try {
		const parsed = JSON.parse(json) as {
			conditions?: EvaluatorCondition[];
		};
		return parsed.conditions ?? [];
	} catch {
		return [];
	}
};

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const { teamMetricsService, teamService } = useContext(ApiServiceContext);
	const [dateRange, setDateRange] = useState<number | undefined>(undefined);
	const [featureWip, setFeatureWip] = useState<number | undefined>(undefined);
	const getTeamSettings = useCallback(
		(teamId: number) => teamService.getTeamSettings(teamId),
		[teamService],
	);
	const {
		settings,
		doingStates,
		doingStatesStatus,
		waitStates,
		stateMappings,
		cycleTimeDefinitions,
		hasBlockedConfig,
		stalenessThresholdDays,
		blockedStalenessThresholdDays,
	} = useOwnerMetricsSettings(team.id, getTeamSettings, "team");
	const forecastFilterConditions = useMemo(
		() => parseForecastFilterConditions(settings?.forecastFilterRuleSetJson),
		[settings],
	);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	useEffect(() => {
		const fetchFeatures = async () => {
			try {
				const featuresData = await teamMetricsService.getFeaturesInProgress(
					team.id,
					new Date(),
				);
				setInProgressFeatures(featuresData);
			} catch (error) {
				console.error(`Error fetching ${featuresTerm} in progress:`, error);
			}
		};

		fetchFeatures().catch((error) => {
			console.error(`Error fetching ${featuresTerm} in progress:`, error);
		});
	}, [team.id, teamMetricsService, featuresTerm]);

	useEffect(() => {
		const fetchFeatureWip = async () => {
			const teamSetting = await teamService.getTeamSettings(team.id);

			let idealFeatureWip: number | undefined;
			if (!teamSetting.automaticallyAdjustFeatureWIP) {
				idealFeatureWip = team.featureWip > 0 ? team.featureWip : undefined;
			}

			setFeatureWip(idealFeatureWip);
		};

		fetchFeatureWip().catch((error) => {
			console.error("Error fetching feature WIP:", error);
		});
	}, [teamService, team]);

	useEffect(() => {
		if (team.useFixedDatesForThroughput) {
			setDateRange(30);
		} else {
			const elapsed =
				team.throughputEndDate.valueOf() - team.throughputStartDate.valueOf();
			// biome-ignore lint/plugin/no-day-count-from-milliseconds: rounding absorbs the one-hour daylight-saving shift
			const range = Math.round(elapsed / (1000 * 60 * 60 * 24));

			setDateRange(range);
		}
	}, [team]);

	return (
		<BaseMetricsView
			entity={team}
			metricsService={teamMetricsService}
			title={workItemsTerm}
			defaultDateRange={dateRange}
			featuresInProgress={inProgressFeatures}
			featureWip={featureWip}
			hasBlockedConfig={hasBlockedConfig}
			doingStates={doingStates}
			doingStatesStatus={doingStatesStatus}
			waitStates={waitStates}
			stateMappings={stateMappings}
			cycleTimeDefinitions={cycleTimeDefinitions}
			hasForecastFilter={forecastFilterConditions.length > 0}
			forecastFilterConditions={forecastFilterConditions}
			stalenessThresholdDays={stalenessThresholdDays}
			blockedStalenessThresholdDays={blockedStalenessThresholdDays}
		/>
	);
};

export default TeamMetricsView;
