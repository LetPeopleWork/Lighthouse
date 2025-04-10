export interface IWorkItem {
	name: string;
	id: number;
	state: string;
	type: string;
	workItemReference: string;
	url: string | null;
	startedDate: Date;
	closedDate: Date;
	cycleTime: number;
	workItemAge: number;
}
