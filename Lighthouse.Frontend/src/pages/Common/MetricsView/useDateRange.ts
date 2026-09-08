import { useCallback, useState } from "react";
import { useSearchParams } from "react-router";
import { useDebouncedRevisionRun } from "../../../hooks/useDebouncedRevisionRun";
import { isValidDate } from "../../../utils/date/isValidDate";
import { formatLocalDate, parseLocalDate } from "../../../utils/date/localDate";
import {
	canStepForward as canWindowStepForward,
	clampWindowToToday,
	type DateWindow,
	type DateWindowPreset,
	getPresetsForOwner,
	getStepDaysForOwner,
	type MetricsOwnerType,
	matchingPresetDays,
	presetWindow,
	shiftWindow,
} from "./dateWindow";

// Longer than the debounce used for typed input elsewhere: a stepper is clicked deliberately, and
// the gap between two intended clicks is wider than the gap between two keystrokes, so a shorter
// window would fire in the middle of a burst and refetch a window nobody wanted to look at.
const COMMIT_QUIET_PERIOD_MS = 500;

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

function windowFromParams(
	params: URLSearchParams,
	defaultDateRange: number,
): DateWindow {
	const configured = presetWindow(defaultDateRange, new Date());
	return {
		start: parseLocalDate(params.get("startDate") ?? "") ?? configured.start,
		end: parseLocalDate(params.get("endDate") ?? "") ?? configured.end,
	};
}

export function useDateRange(
	ownerType: MetricsOwnerType,
	defaultDateRange: number,
): UseDateRangeResult {
	const [searchParams, setSearchParams] = useSearchParams();
	const [committedWindow, setCommittedWindow] = useState<DateWindow>(() =>
		windowFromParams(searchParams, defaultDateRange),
	);
	const [pendingWindow, setPendingWindow] = useState<DateWindow | null>(null);
	const [stepRevision, setStepRevision] = useState(0);

	// Every control here moves both ends of the window at once. Writing one end at a time rebuilds
	// the other end from the render that was current when the handler was created, so the second
	// write silently undoes the first and the page then fetches a window nobody asked for. This is
	// the only place the window is written, and it is built from its arguments alone.
	const applyDateRange = useCallback(
		(start: Date, end: Date) => {
			if (!isValidDate(start) || !isValidDate(end)) {
				return;
			}

			setPendingWindow(null);
			setCommittedWindow({ start, end });

			const nextParams = new URLSearchParams(searchParams);
			nextParams.set("startDate", formatLocalDate(start));
			nextParams.set("endDate", formatLocalDate(end));
			setSearchParams(nextParams, { replace: true });
		},
		[searchParams, setSearchParams],
	);

	const stepDays = getStepDaysForOwner(ownerType);
	const displayedWindow = pendingWindow ?? committedWindow;

	const commitPendingWindow = useCallback(() => {
		if (pendingWindow === null) {
			return;
		}
		applyDateRange(pendingWindow.start, pendingWindow.end);
	}, [applyDateRange, pendingWindow]);

	useDebouncedRevisionRun(
		stepRevision,
		commitPendingWindow,
		COMMIT_QUIET_PERIOD_MS,
	);

	const applyPreset = useCallback(
		(days: number) => {
			const next = presetWindow(days, new Date());
			applyDateRange(next.start, next.end);
		},
		[applyDateRange],
	);

	// Each click has to walk on from where the previous one left the window, and a burst of them
	// lands before any re-render, so the step is taken from the queued window rather than from the
	// one this render closed over.
	const stepWindow = useCallback(
		(direction: -1 | 1) => {
			const today = new Date();
			setPendingWindow((queued) =>
				clampWindowToToday(
					shiftWindow(queued ?? committedWindow, direction * stepDays),
					today,
				),
			);
			setStepRevision((revision) => revision + 1);
		},
		[committedWindow, stepDays],
	);

	const handleStartDateChange = useCallback(
		(date: Date | null) => {
			if (!isValidDate(date)) {
				return;
			}
			applyDateRange(date, committedWindow.end);
		},
		[applyDateRange, committedWindow.end],
	);

	const handleEndDateChange = useCallback(
		(date: Date | null) => {
			if (!isValidDate(date)) {
				return;
			}
			applyDateRange(committedWindow.start, date);
		},
		[applyDateRange, committedWindow.start],
	);

	const today = new Date();
	const presets = getPresetsForOwner(ownerType);

	return {
		startDate: committedWindow.start,
		endDate: committedWindow.end,
		pendingStartDate: displayedWindow.start,
		pendingEndDate: displayedWindow.end,
		isCommitPending: pendingWindow !== null,
		canStepForward: canWindowStepForward(displayedWindow, today),
		presets,
		selectedPresetDays: matchingPresetDays(displayedWindow, presets, today),
		stepDays,
		applyDateRange,
		applyPreset,
		stepWindow,
		handleStartDateChange,
		handleEndDateChange,
	};
}
