import type { IWorkTrackingSystemOption } from "./WorkTrackingSystemOption";

export type WorkTrackingSystemType = "Jira" | "AzureDevOps" | "Linear" | "Csv";

export type DataSourceType = "Query" | "File";

export interface IWorkTrackingSystemConnection {
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
	dataSourceType: DataSourceType;
}

export class WorkTrackingSystemConnection
	implements IWorkTrackingSystemConnection
{
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
	dataSourceType: DataSourceType;

	constructor(
		name: string,
		workTrackingSystem: WorkTrackingSystemType,
		options: IWorkTrackingSystemOption[],
		dataSourceType: DataSourceType = "Query",
		id: number | null = null,
	) {
		this.id = id;
		this.name = name;
		this.workTrackingSystem = workTrackingSystem;
		this.options = options;
		this.dataSourceType = dataSourceType;
	}
}
