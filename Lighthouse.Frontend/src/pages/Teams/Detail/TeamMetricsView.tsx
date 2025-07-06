import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { BaseMetricsView } from "../../Common/MetricsView/BaseMetricsView";
import ItemsInProgress from "./ItemsInProgress";

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [doingStates, setDoingStates] = useState<string[]>([]);
	const { teamMetricsService, teamService } = useContext(ApiServiceContext);
	const [dateRange, setDateRange] = useState<number | undefined>(undefined);

	useEffect(() => {
		const fetchFeatures = async () => {
			try {
				const featuresData = await teamMetricsService.getFeaturesInProgress(
					team.id,
				);
				setInProgressFeatures(featuresData);
			} catch (err) {
				console.error("Error fetching features in progress:", err);
			}
		};

		fetchFeatures();
	}, [team.id, teamMetricsService]);

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

	const renderTeamSpecificContent = () => (
		<ItemsInProgress
			title="Features being Worked On:"
			items={inProgressFeatures}
			idealWip={team.featureWip > 0 ? team.featureWip : undefined}
		/>
	);

	return (
		<BaseMetricsView
			entity={team}
			metricsService={teamMetricsService}
			title="Work Items"
			defaultDateRange={dateRange}
			renderAdditionalComponents={renderTeamSpecificContent}
			doingStates={doingStates}
		/>
	);
};

export default TeamMetricsView;
