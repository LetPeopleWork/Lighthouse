import { format, subDays } from "date-fns";
import type { IReportTemplates } from "../../models/Reports/Report";
import type { IReportCreationOwner } from "../../models/Reports/ReportsAcrossOwners";
import { parseLocalDate } from "../../utils/date/localDate";
import { LONG_DAY } from "../Common/Reports/wizard/longDay";
import { ownerCannotHold } from "../Common/Reports/wizard/reportWindowCheck";
import {
	lengthOf,
	nowLengthOf,
	thenEndOf,
} from "../Common/Reports/wizard/thenAndNow/thenAndNowDraft";
import type { Draft } from "./bulkWizardSteps";
import { type OwnerLimitOf, ownerKeyOf } from "./OwnerStep";

/** What stops an owner from holding the window as configured so far, or null when it holds it. */
export const limitOfFor =
	(draft: Draft, templates: IReportTemplates): OwnerLimitOf =>
	(owner) => {
		const thenLength = lengthOf(draft.thenLength);
		const nowLength = lengthOf(nowLengthOf(draft));
		const thenEndDay = thenEndOf(draft, templates);
		const thenEnd = parseLocalDate(thenEndDay);
		const keptSince =
			owner.earliestStart === null ? null : parseLocalDate(owner.earliestStart);
		if (thenLength === null || thenEnd === null || keptSince === null)
			return null;
		const today = parseLocalDate(templates.today);
		const tooLong: { window: "Then" | "Now"; start: Date }[] = [];
		if (
			ownerCannotHold(
				{ side: "then", end: thenEndDay, lengthDays: thenLength },
				templates,
				owner,
			)
		)
			tooLong.push({
				window: "Then",
				start: subDays(thenEnd, thenLength - 1),
			});
		if (
			nowLength !== null &&
			today !== null &&
			ownerCannotHold({ side: "now", lengthDays: nowLength }, templates, owner)
		)
			tooLong.push({ window: "Now", start: subDays(today, nowLength - 1) });
		tooLong.sort((left, right) => left.start.getTime() - right.start.getTime());
		const furthestBack = tooLong[0];
		return furthestBack === undefined
			? null
			: {
					keptSince: format(keptSince, LONG_DAY),
					window: furthestBack.window,
					windowStarts: format(furthestBack.start, LONG_DAY),
				};
	};

/** The owners that can hold the window. */
export const holdableOwners = (
	owners: readonly IReportCreationOwner[],
	limitOf: OwnerLimitOf,
): IReportCreationOwner[] => owners.filter((owner) => limitOf(owner) === null);

/** The picked owners that can hold the window; a pick that cannot is not one. */
export const pickedOwnersOf = (
	owners: readonly IReportCreationOwner[],
	picked: ReadonlySet<string>,
	limitOf: OwnerLimitOf,
): IReportCreationOwner[] =>
	holdableOwners(owners, limitOf).filter((owner) =>
		picked.has(ownerKeyOf(owner)),
	);

/** The picks the window can still hold, as keys; the rest are dropped. */
export const picksThatCanStillHold = (
	owners: readonly IReportCreationOwner[],
	picked: ReadonlySet<string>,
	limitOf: OwnerLimitOf,
): Set<string> =>
	new Set(pickedOwnersOf(owners, picked, limitOf).map(ownerKeyOf));
