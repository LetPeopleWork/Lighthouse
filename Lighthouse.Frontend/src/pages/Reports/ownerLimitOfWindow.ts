import { format, subDays } from "date-fns";
import type { IReportTemplates } from "../../models/Reports/Report";
import type { IReportCreationOwner } from "../../models/Reports/ReportsAcrossOwners";
import { parseLocalDate } from "../../utils/date/localDate";
import { LONG_DAY } from "../Common/Reports/wizard/longDay";
import { ownerCannotHold } from "../Common/Reports/wizard/reportWindowCheck";
import {
	lengthOf,
	thenEndOf,
} from "../Common/Reports/wizard/thenAndNow/thenAndNowDraft";
import type { Draft } from "./bulkWizardSteps";
import { type OwnerLimitOf, ownerKeyOf } from "./OwnerStep";

/**
 * What stops an owner from holding the window as configured so far, or null when it holds it. Only Then can
 * be too long: Now is as long as Then and ends today, so it starts no earlier than a Then that ends by today.
 */
export const limitOfFor =
	(draft: Draft, templates: IReportTemplates): OwnerLimitOf =>
	(owner) => {
		const thenLength = lengthOf(draft.thenLength);
		const thenEndDay = thenEndOf(draft, templates);
		const thenEnd = parseLocalDate(thenEndDay);
		const keptSince =
			owner.earliestStart === null ? null : parseLocalDate(owner.earliestStart);
		if (thenLength === null || thenEnd === null || keptSince === null)
			return null;
		return ownerCannotHold(
			{ side: "then", end: thenEndDay, lengthDays: thenLength },
			templates,
			owner,
		)
			? {
					keptSince: format(keptSince, LONG_DAY),
					window: "Then",
					windowStarts: format(subDays(thenEnd, thenLength - 1), LONG_DAY),
				}
			: null;
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
