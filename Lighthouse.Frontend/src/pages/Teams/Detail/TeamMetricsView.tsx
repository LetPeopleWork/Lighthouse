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
	const { teamMetricsService } = useContext(ApiServiceContext);

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

	const renderTeamSpecificContent = () => (
		<ItemsInProgress
			title="Features being Worked On:"
			items={inProgressFeatures}
			idealWip={team.featureWip}
		/>
	);

	return (
		<BaseMetricsView
			entity={team}
			metricsService={teamMetricsService}
			title="Work Items"
			defaultDateRange={30}
			renderAdditionalComponents={renderTeamSpecificContent}
		/>
	);
};

export default TeamMetricsView;
