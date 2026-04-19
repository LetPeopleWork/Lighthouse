import type { IForecastInputCandidates } from "../../models/Forecasts/ForecastInputCandidates";
import type { IFeaturesWorkedOnInfo } from "../../models/Metrics/InfoWidgetData";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseMetricsService, type ITeamMetricsService } from "./MetricsService";

export class TeamMetricsService
	extends BaseMetricsService<IWorkItem>
	implements ITeamMetricsService
{
	constructor() {
		super("teams");
	}

	async getFeaturesInProgress(
		teamId: number,
		asOfDate: Date,
	): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const year = asOfDate.getFullYear();
			const month = String(asOfDate.getMonth() + 1).padStart(2, "0");
			const day = String(asOfDate.getDate()).padStart(2, "0");
			const formattedDate = `${year}-${month}-${day}`;

			const response = await this.apiService.get<IWorkItem[]>(
				`/teams/${teamId}/metrics/featuresInProgress?asOfDate=${formattedDate}`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return workItems;
		});
	}

	async getForecastInputCandidates(
		teamId: number,
	): Promise<IForecastInputCandidates> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IForecastInputCandidates>(
				`/teams/${teamId}/metrics/forecastInputCandidates`,
			);
			return response.data;
		});
	}

	async getFeaturesWorkedOnInfo(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeaturesWorkedOnInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IFeaturesWorkedOnInfo>(
				`/teams/${teamId}/metrics/featuresWorkedOnInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}
}
