import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { EvaluatorCondition } from "../../../components/Common/Charts/ThroughputChart/evaluateCondition";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { ICycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { parseRuleSet } from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";
import type { WidgetStatus } from "../../Common/MetricsView/widgetStatus";

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

interface DoingColumns {
	readonly teamId: number;
	readonly status: WidgetStatus;
	readonly states: string[];
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [doingColumns, setDoingColumns] = useState<DoingColumns>();
	const [waitStates, setWaitStates] = useState<string[]>([]);
	const [stateMappings, setStateMappings] = useState<IStateMapping[]>([]);
	const [cycleTimeDefinitions, setCycleTimeDefinitions] = useState<
		ICycleTimeDefinition[]
	>([]);
	const [hasBlockedConfig, setHasBlockedConfig] = useState(false);
	const [stalenessThresholdDays, setStalenessThresholdDays] = useState<
		number | undefined
	>(undefined);
	const [blockedStalenessThresholdDays, setBlockedStalenessThresholdDays] =
		useState<number | undefined>(undefined);
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
		let isCurrentTeam = true;
		const fetchTeamSettings = async () => {
			try {
				const settings = await teamService.getTeamSettings(team.id);
				if (!isCurrentTeam) return;
				setDoingColumns({
					teamId: team.id,
					status: "ready",
					states: settings.doingDisplayStates ?? [],
				});
				setWaitStates(settings.waitStates ?? []);
				setStateMappings(settings.stateMappings);
				setCycleTimeDefinitions(settings.cycleTimeDefinitions ?? []);
				setHasBlockedConfig(
					(parseRuleSet(settings.blockedRuleSetJson)?.conditions.length ?? 0) >
						0,
				);
				setStalenessThresholdDays(settings.stalenessThresholdDays);
				setBlockedStalenessThresholdDays(
					settings.blockedStalenessThresholdDays,
				);
				setForecastFilterConditions(
					parseForecastFilterConditions(settings.forecastFilterRuleSetJson),
				);
			} catch (error) {
				console.error("Error fetching team settings:", error);
				if (isCurrentTeam) {
					setDoingColumns({ teamId: team.id, status: "error", states: [] });
				}
			}
		};

		fetchTeamSettings().catch((error) => {
			console.error("Error fetching team settings:", error);
		});
		return () => {
			isCurrentTeam = false;
		};
	}, [team.id, teamService]);

	// Until this team's own answer arrives, the columns of a team shown before are not its columns.
	const columnsOfThisTeam =
		doingColumns?.teamId === team.id ? doingColumns : undefined;

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
			doingStates={columnsOfThisTeam?.states ?? []}
			doingStatesStatus={columnsOfThisTeam?.status ?? "loading"}
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
