import type { IWorkItem } from "../../models/WorkItem";
import { BaseMetricsService, type ITeamMetricsService } from "./MetricsService";

export class TeamMetricsService
	extends BaseMetricsService<IWorkItem>
	implements ITeamMetricsService
{
	constructor() {
		super("teams");
	}

	async getFeaturesInProgress(teamId: number): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/teams/${teamId}/metrics/featuresInProgress`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return workItems;
		});
	}
}
