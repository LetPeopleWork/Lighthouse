import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";
import type { DashboardItem } from "../../Common/MetricsView/Dashboard";
import ItemsInProgress from "./ItemsInProgress";

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const { teamMetricsService, teamService } = useContext(ApiServiceContext);
	const [dateRange, setDateRange] = useState<number | undefined>(undefined);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	useEffect(() => {
		const fetchFeatures = async () => {
			try {
				const featuresData = await teamMetricsService.getFeaturesInProgress(
					team.id,
				);
				setInProgressFeatures(featuresData);
			} catch (err) {
				console.error(`Error fetching ${featuresTerm} in progress:`, err);
			}
		};

		fetchFeatures();
	}, [team.id, teamMetricsService, featuresTerm]);

	useEffect(() => {
		const fetchTeamSettings = async () => {
			try {
				const settings = await teamService.getTeamSettings(team.id);
				setDoingStates(settings.doingStates);
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
			const range = Math.floor(
				(team.throughputEndDate.valueOf() -
					team.throughputStartDate.valueOf()) /
					(1000 * 60 * 60 * 24),
			);

			setDateRange(range);
		}
	}, [team]);

	const renderFeaturesTeamWorksOn = () => (
		<ItemsInProgress
			title={`${featuresTerm} being Worked On:`}
			items={inProgressFeatures}
			idealWip={team.featureWip > 0 ? team.featureWip : undefined}
		/>
	);

	const additionalItems: DashboardItem[] = [
		{
			id: "featuresTeamWorksOn",
			node: renderFeaturesTeamWorksOn(),
		},
	];

	return (
		<BaseMetricsView
			entity={team}
			metricsService={teamMetricsService}
			title={workItemsTerm}
			defaultDateRange={dateRange}
			additionalItems={additionalItems}
			doingStates={doingStates}
		/>
	);
};

export default TeamMetricsView;
