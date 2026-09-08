import type { DateWindowPreset, MetricsOwnerType } from "./dateWindow";

export interface UseDateRangeResult {
	/** The window the widgets are showing. Only this pair reaches the URL. */
	readonly startDate: Date;
	readonly endDate: Date;
	/** What the header label reads. Equals the committed pair unless a commit is outstanding. */
	readonly pendingStartDate: Date;
	readonly pendingEndDate: Date;
	readonly isCommitPending: boolean;
	readonly canStepForward: boolean;
	readonly presets: readonly DateWindowPreset[];
	readonly selectedPresetDays: number | null;
	readonly stepDays: number;
	readonly applyDateRange: (start: Date, end: Date) => void;
	readonly applyPreset: (days: number) => void;
	readonly stepWindow: (direction: -1 | 1) => void;
	readonly handleStartDateChange: (date: Date | null) => void;
	readonly handleEndDateChange: (date: Date | null) => void;
}

export function useDateRange(
	ownerType: MetricsOwnerType,
	defaultDateRange: number,
): UseDateRangeResult {
	throw new Error(
		`useDateRange(${ownerType}, ${defaultDateRange}) is not implemented`,
	);
}
