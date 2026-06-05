import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { EvaluatorCondition } from "../../../components/Common/Charts/ThroughputChart/evaluateCondition";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";

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
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const [waitStates, setWaitStates] = useState<string[]>([]);
	const [stateMappings, setStateMappings] = useState<IStateMapping[]>([]);
	const [hasBlockedConfig, setHasBlockedConfig] = useState(false);
	const [stalenessThresholdDays, setStalenessThresholdDays] = useState<
		number | undefined
	>(undefined);
	const { teamMetricsService, teamService } = useContext(ApiServiceContext);
	const [dateRange, setDateRange] = useState<number | undefined>(undefined);
	const [featureWip, setFeatureWip] = useState<number | undefined>(undefined);
	const [forecastFilterConditions, setForecastFilterConditions] = useState<
		readonly EvaluatorCondition[]
	>([]);

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
			} catch (err) {
				console.error(`Error fetching ${featuresTerm} in progress:`, err);
			}
		};

		fetchFeatures();
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

		fetchFeatureWip();
	}, [teamService, team]);

	useEffect(() => {
		const fetchTeamSettings = async () => {
			try {
				const settings = await teamService.getTeamSettings(team.id);
				setDoingStates(settings.doingStates);
				setWaitStates(settings.waitStates ?? []);
				setStateMappings(settings.stateMappings);
				setHasBlockedConfig(
					settings.blockedStates.length > 0 || settings.blockedTags.length > 0,
				);
				setStalenessThresholdDays(settings.stalenessThresholdDays);
				setForecastFilterConditions(
					parseForecastFilterConditions(settings.forecastFilterRuleSetJson),
				);
			} catch (err) {
				console.error("Error fetching team settings:", err);
			}
		};

		fetchTeamSettings();
	}, [team.id, teamService]);

	useEffect(() => {
		if (team.useFixedDatesForThroughput) {
			setDateRange(30);
		} else {
			const range = Math.round(
				(team.throughputEndDate.valueOf() -
					team.throughputStartDate.valueOf()) /
					(1000 * 60 * 60 * 24),
			);

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
			waitStates={waitStates}
			stateMappings={stateMappings}
			hasForecastFilter={forecastFilterConditions.length > 0}
			forecastFilterConditions={forecastFilterConditions}
			stalenessThresholdDays={stalenessThresholdDays}
		/>
	);
};

export default TeamMetricsView;
