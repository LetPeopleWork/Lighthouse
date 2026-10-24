import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import type { IWorkItemRuleSchema } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import { askedAfreshQueryOptions } from "./askedAfreshQueryOptions";
import type { PremiumVerdict } from "./premiumVerdict";

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
export function dialogQueryStatusOf(query: DialogQueryState): WidgetStatus {
	if (query.isFetching) {
		return "loading";
	}
	if (query.isError) {
		return "error";
	}
	if (query.data === undefined) {
		return "loading";
	}
	return "ready";
}

/** One of the dialog's own answers, as far as the form needs to know it. */
export interface DeliveryFormAnswer<T> {
	status: WidgetStatus;
	data: T | undefined;
}

function answerOf<T>(
	query: DialogQueryState & { data: T | undefined },
): DeliveryFormAnswer<T> {
	const status = dialogQueryStatusOf(query);
	return { status, data: status === "ready" ? query.data : undefined };
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
export function useDialogFeatureList({
	portfolio,
	isOpen,
	opening,
}: DialogFeatureListRequest): DeliveryFormAnswer<IFeature[]> {
	const { featureService } = useContext(ApiServiceContext);
	const featureIds = portfolio.features.map((feature) => feature.id);
	const hasFeatures = featureIds.length > 0;

	const query = useQuery({
		...askedAfreshQueryOptions,
		queryKey: ["deliveryDialogFeatures", portfolio.id, opening],
		queryFn: () => featureService.getFeaturesByIds(featureIds),
		enabled: isOpen && hasFeatures,
	});

	return hasFeatures ? answerOf(query) : NO_FEATURES_TO_ASK_FOR;
}

const NO_FEATURES_TO_ASK_FOR: DeliveryFormAnswer<IFeature[]> = {
	status: "ready",
	data: [],
};

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
export function useDialogRuleSchema({
	portfolioId,
	isOpen,
	opening,
	verdict,
	latched,
}: DialogRuleSchemaRequest): DeliveryFormAnswer<IWorkItemRuleSchema> {
	const { deliveryService } = useContext(ApiServiceContext);

	const query = useQuery({
		...askedAfreshQueryOptions,
		queryKey: ["deliveryRuleSchema", portfolioId, opening],
		queryFn: () => deliveryService.getRuleSchema(portfolioId),
		enabled: isOpen && verdict === "premium" && latched,
	});

	return answerOf(query);
}
