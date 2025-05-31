import type { IWorkTrackingSystemOption } from "./WorkTrackingSystemOption";

export type WorkTrackingSystemType = "Jira" | "AzureDevOps" | "Linear";

export interface IWorkTrackingSystemConnection {
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
}

export class WorkTrackingSystemConnection
	implements IWorkTrackingSystemConnection
{
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];

	constructor(
		name: string,
		workTrackingSystem: WorkTrackingSystemType,
		options: IWorkTrackingSystemOption[],
		id: number | null = null,
	) {
		this.id = id;
		this.name = name;
		this.workTrackingSystem = workTrackingSystem;
		this.options = options;
	}
}
