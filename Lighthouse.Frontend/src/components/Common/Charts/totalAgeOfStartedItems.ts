import type { IWorkItem } from "../../../models/WorkItem";
import { calculateHistoricalAge } from "../../../utils/date/age";

// An item without a start date has no age to add; counting it would turn the
// whole day's total into NaN and the chart would draw nothing.
export const totalAgeOfStartedItems = (
	items: IWorkItem[],
	historicalDate: Date,
): { totalAge: number; itemCount: number } => {
	const startedItems = items.filter((item) => item.startedDate != null);
	const totalAge = startedItems.reduce(
		(sum, item) => sum + calculateHistoricalAge(item, historicalDate),
		0,
	);
	return { totalAge, itemCount: startedItems.length };
};
