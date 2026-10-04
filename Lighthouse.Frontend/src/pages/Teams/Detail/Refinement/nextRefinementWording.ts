import { parseLocalDate } from "../../../../utils/date/localDate";

/** A day as "Thu 8 Oct". */
export const formatDayAndDate = (date: Date): string =>
	date.toLocaleDateString("en-GB", {
		weekday: "short",
		day: "numeric",
		month: "short",
	});

const describeDistance = (days: number): string =>
	days === 1 ? "tomorrow" : `in ${days} days`;

/**
 * "Next Refinement: Thu 8 Oct · in 4 days", or null when there is no next Refinement to name. The days
 * are the server's count from the instance's today, which in another time zone is not the viewer's.
 */
export const describeNextRefinement = (
	nextRefinementDate: string | null | undefined,
	daysUntilNextRefinement: number | null | undefined,
	refinementTerm: string,
): string | null => {
	const day =
		nextRefinementDate == null ? null : parseLocalDate(nextRefinementDate);
	if (day === null) {
		return null;
	}
	const named = `Next ${refinementTerm}: ${formatDayAndDate(day)}`;
	return daysUntilNextRefinement == null
		? named
		: `${named} · ${describeDistance(daysUntilNextRefinement)}`;
};

/** The words a Team has renamed that the hint for a Team without a cadence uses. */
export interface CadenceHintTerms {
	team: string;
	refinement: string;
	workItems: string;
}

/** How to get a cadence: somebody who may change the Team's settings is pointed there, anybody else to a Team admin. */
export const describeHowToGetACadence = (
	canChangeSettings: boolean,
	terms: CadenceHintTerms,
): string =>
	canChangeSettings
		? `Set a ${terms.refinement} cadence in Settings to see how many ${terms.workItems} are needed`
		: `A ${terms.team} admin can set a ${terms.refinement} cadence to see how many ${terms.workItems} are needed`;
