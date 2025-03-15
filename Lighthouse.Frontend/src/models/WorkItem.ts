export interface IWorkItem {
	name: string;
	id: number;
	workItemReference: string;
	url: string | null;
	startedDate: Date;
	closedDate: Date;
}
