export type DayOfWeek =
	| "Sunday"
	| "Monday"
	| "Tuesday"
	| "Wednesday"
	| "Thursday"
	| "Friday"
	| "Saturday";

export const ORDERED_WEEKDAYS: readonly DayOfWeek[] = [
	"Monday",
	"Tuesday",
	"Wednesday",
	"Thursday",
	"Friday",
	"Saturday",
	"Sunday",
];

export interface IRecurringBlackoutRule {
	id: number;
	weekdays: DayOfWeek[];
	intervalWeeks: number;
	start: string;
	end: string | null;
	description: string;
	summary: string;
}
