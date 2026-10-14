const CANNOT_BE_REBUILT = "This Then window can no longer be rebuilt.";

/** A Then whose days have fallen out of the kept history is gone for good once its report is. */
export const rebuildWarningOf = (report: {
	thenAndNow?: { thenRebuildable: boolean };
}): string | undefined =>
	report.thenAndNow?.thenRebuildable === false ? CANNOT_BE_REBUILT : undefined;
