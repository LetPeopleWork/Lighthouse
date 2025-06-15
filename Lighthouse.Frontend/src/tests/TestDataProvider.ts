import type { IWorkItem } from "../models/WorkItem";

export function generateWorkItemMapForRunChart(rawDataInput: number[]) {
	const workItemsPerUnitOfTime: { [key: number]: IWorkItem[] } = {};

	let counter = 0;
	for (let day = 0; day < rawDataInput.length; day++) {
		const itemCount = rawDataInput[day];
		workItemsPerUnitOfTime[day] = [];

		// For each day, generate the required number of work items
		for (let i = 0; i < itemCount; i++) {
			const workItem = generateWorkItem(counter++);
			workItemsPerUnitOfTime[day].push(workItem);
		}
	}

	return workItemsPerUnitOfTime;
}

export function generateWorkItem(id: number): IWorkItem {
	// Random date between now and 30 days ago
	const getRandomDate = (maxDaysAgo: number) => {
		const today = new Date();
		const daysAgo = Math.floor(Math.random() * (maxDaysAgo + 1));
		const date = new Date(today);
		date.setDate(today.getDate() - daysAgo);
		return date;
	};

	// Generate random work item
	const startedDate = getRandomDate(30);
	// Closed date must be after start date (or same day)
	const daysAfterStart = Math.floor(
		Math.random() * (30 - (30 - startedDate.getDate()) + 1),
	);

	const closedDate = new Date(startedDate);
	closedDate.setDate(startedDate.getDate() + daysAfterStart);

	return {
		name: `Work Item that has a very long name so I can test whether the text wrapping works so I'm just adding more text and see whenever it's getting too big. I wonder what people think, don't they know titles should be short - put all that other stuff in the description...anyway, is this wrapped? - ${id}`,
		id: id,
		referenceId: `WI-${id}`,
		url: `https://example.com/work-items/${id}`,
		state: "In Progress",
		stateCategory: "Doing",
		type: "Feature",
		workItemAge: Math.floor(Math.random() * (19 - 3 + 1)) + 3,
		startedDate,
		closedDate,
		cycleTime: daysAfterStart + 1,
	};
}
