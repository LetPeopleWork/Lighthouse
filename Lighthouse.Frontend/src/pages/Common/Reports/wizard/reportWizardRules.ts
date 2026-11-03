import type { IReportTemplates } from "../../../../models/Reports/Report";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	COULD_NOT_SAVE,
	NO_LONGER_EXISTS,
	type RefusalWords,
	refusalCopyFor,
} from "./reportRefusalCopy";
import type { ReportTypeStep } from "./reportTypes";

export const STALE_SAVE =
	"This report was changed by someone else. Reload to see their changes.";
const TYPE_STEP = 0;
/** Type and Then are locked on an edit, so it opens on Now, the first step it can change. */
const FIRST_EDITABLE_STEP = 2;

export const firstStepOf = (isEditing: boolean): number =>
	isEditing ? FIRST_EDITABLE_STEP : TYPE_STEP;

/** Somebody else saved the report after this edit read it, so the edit would overwrite theirs. */
export const isStaleSave = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 409;

export const isThenBeforeCutoff = (error: unknown): boolean =>
	error instanceof ApiError && error.problemCode === "then-before-cutoff";

/** Somebody else deleted the report after this edit read it. */
export const isGone = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 404;

export const saveFailureCopyFor = (
	error: unknown,
	words: RefusalWords,
): string => {
	if (isStaleSave(error)) return STALE_SAVE;
	if (isGone(error)) return NO_LONGER_EXISTS;
	return refusalCopyFor(error, words, COULD_NOT_SAVE);
};

export const canMoveOn = <TDraft>(
	step: ReportTypeStep<TDraft> | undefined,
	isFrozen: boolean,
	draft: TDraft,
	templates: IReportTemplates | undefined,
): boolean =>
	templates !== undefined &&
	(step === undefined || isFrozen || step.isComplete(draft, templates));
