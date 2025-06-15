import type { IWorkItem } from "../models/WorkItem";

export const getWorkItemName = (workItem: IWorkItem): string => {
	if (workItem.name?.toLowerCase().includes("unparented")) {
		return workItem.name;
	}

	return `${workItem.referenceId}: ${workItem.name}`;
};
