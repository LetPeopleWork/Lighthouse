import { MOST_PER_BULK_RUN } from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";

export const TOO_MANY_PICKED = `Pick at most ${MOST_PER_BULK_RUN} reports.`;

/**
 * Why Remove is held back, or "" when it is not. Too many picked comes first; then permissions that never arrived,
 * because the hook then answers as if the reader were Admin everywhere; then picks on owners the reader cannot change.
 */
export const removeBlockedBecause = (
	picked: number,
	permissionsFailed: boolean,
	notAdmin: number,
	getTerm: (key: string) => string,
): string => {
	if (picked > MOST_PER_BULK_RUN) return TOO_MANY_PICKED;
	if (permissionsFailed)
		return "Your permissions couldn't be loaded. Reload the page to try again.";
	if (notAdmin === 0) return "";
	return `${notAdmin} of the ${picked} selected reports ${notAdmin === 1 ? "is" : "are"} on a ${getTerm(TERMINOLOGY_KEYS.TEAM)} or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)} you are not Admin of.`;
};

/** Why Export PDF is held back, or "" when it is not. */
export const exportBlockedBecause = (picked: number): string => {
	if (picked === 0) return "Select at least one report.";
	if (picked > MOST_PER_BULK_RUN) return TOO_MANY_PICKED;
	return "";
};
