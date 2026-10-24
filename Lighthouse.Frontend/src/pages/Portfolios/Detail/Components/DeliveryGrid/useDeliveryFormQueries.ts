import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import type { IWorkItemRuleSchema } from "../../../../../models/WorkItemRules";
import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import type { PremiumVerdict } from "./premiumVerdict";

// The dialog's scenarios were written ahead of these; until they are built, every call throws.
export const __SCAFFOLD__ = true;

/** What a body of the dialog needs to know of one of its queries to say what it is doing. */
export interface DialogQueryState {
	data: unknown;
	isError: boolean;
	isFetching: boolean;
}

/**
 * What a body of the dialog shows for one of its queries. A query that has not started, because the licence, the
 * sources or the open flag are not in yet, has neither data nor an error and reads as loading, never as ready: the
 * reader is waiting for it, whatever the reason it has not been sent.
 */
export function dialogQueryStatusOf(_query: DialogQueryState): WidgetStatus {
	throw new Error("dialogQueryStatusOf is not built yet");
}

/** One of the dialog's own answers, as far as the form needs to know it. */
export interface DeliveryFormAnswer<T> {
	status: WidgetStatus;
	data: T | undefined;
}

export interface DialogFeatureListRequest {
	portfolio: Portfolio;
	isOpen: boolean;
	/** Counts the openings of the dialog, so an answer meant for an earlier one is never shown. */
	opening: number;
}

/**
 * The Features the Manual tab lists, asked for once per opening. A Portfolio with no Features asks for nothing and
 * is ready at once with an empty list.
 */
export function useDialogFeatureList(
	_request: DialogFeatureListRequest,
): DeliveryFormAnswer<IFeature[]> {
	throw new Error("useDialogFeatureList is not built yet");
}

export interface DialogRuleSchemaRequest {
	portfolioId: number;
	isOpen: boolean;
	opening: number;
	verdict: PremiumVerdict;
	/**
	 * Set the first time in this opening that the dialog is open, the licence is known premium and Rule-Based is the
	 * tab on screen, whatever put it there: a click, editing a Rule-Based Delivery, or the licence arriving while
	 * Rule-Based was already chosen. It stays set until the dialog closes, so moving back to Rule-Based never asks twice.
	 */
	latched: boolean;
}

/** The fields a rule may test, asked for at most once per opening and only on a premium licence. */
export function useDialogRuleSchema(
	_request: DialogRuleSchemaRequest,
): DeliveryFormAnswer<IWorkItemRuleSchema> {
	throw new Error("useDialogRuleSchema is not built yet");
}
