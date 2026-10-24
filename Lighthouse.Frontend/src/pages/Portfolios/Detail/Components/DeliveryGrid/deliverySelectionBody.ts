import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import {
	type DeliverySelectionTab,
	MANUAL_SELECTION_TAB_KEY,
	RULE_BASED_SELECTION_TAB_KEY,
} from "./deliverySelectionTabs";
import type { PremiumVerdict } from "./premiumVerdict";
import type { SourcesStatus } from "./useDialogDeliverySources";

/**
 * What the body under the tabs shows. A premium tab says it is premium only once the licence has
 * said so; until the licence answers it shows the same loading look as a list still on its way, so
 * nobody is told to upgrade a licence they may well have.
 */
export const PREMIUM_NOTICE = "premiumNotice";
export const UNAVAILABLE = "unavailable";
export const FEATURE_LIST_LOADING = "featureListLoading";
export const FEATURE_LIST_FAILED = "featureListFailed";

export type SelectionBody =
	| { kind: typeof PREMIUM_NOTICE; notice: string }
	| { kind: "loading" }
	| { kind: typeof UNAVAILABLE }
	| { kind: typeof FEATURE_LIST_LOADING }
	| { kind: typeof FEATURE_LIST_FAILED }
	| { kind: "content" };

export interface SelectionBodyQuestion {
	tab: DeliverySelectionTab;
	verdict: PremiumVerdict;
	ruleSchemaStatus: WidgetStatus;
	sourcesStatus: SourcesStatus;
	followsTrackerEntry: boolean;
	featureListStatus: WidgetStatus;
}

/**
 * The Manual list stays on screen while it loads, under the grid's own busy look, so the reader sees
 * where the Features will appear; only a list that could not be loaded gives its place to a notice.
 */
const manualBodyOf: Record<WidgetStatus, SelectionBody> = {
	loading: { kind: FEATURE_LIST_LOADING },
	error: { kind: FEATURE_LIST_FAILED },
	ready: { kind: "content" },
};

const contentBodyOf = ({
	tab,
	ruleSchemaStatus,
	featureListStatus,
}: Pick<
	SelectionBodyQuestion,
	"tab" | "ruleSchemaStatus" | "featureListStatus"
>): SelectionBody => {
	if (tab.key === MANUAL_SELECTION_TAB_KEY) {
		return manualBodyOf[featureListStatus];
	}
	if (
		tab.key === RULE_BASED_SELECTION_TAB_KEY &&
		ruleSchemaStatus === "loading"
	) {
		return { kind: "loading" };
	}
	return { kind: "content" };
};

/**
 * A delivery that follows an entry of the work tracking system belongs on a tab that only exists
 * once the server has listed it. Until then the form stands on no tab at all: any tab pressed, any
 * body shown and any name typed would be a guess, and the Manual list in particular would read as
 * what the delivery holds.
 */
export const awaitsBoundSourceTab = ({
	tab,
	sourcesStatus,
	followsTrackerEntry,
}: Pick<
	SelectionBodyQuestion,
	"tab" | "sourcesStatus" | "followsTrackerEntry"
>): boolean =>
	followsTrackerEntry && tab.source === undefined && sourcesStatus !== "ready";

/**
 * A tab pressed while the form waits for the one it belongs on would let go of the entry the delivery
 * follows and of its broadcast, and the tab arriving afterwards would not bring either back. So no tab
 * can be pressed until then, besides the premium tabs a licence without premium locks.
 */
export const selectionTabIsLocked = (
	tab: DeliverySelectionTab,
	verdict: PremiumVerdict,
	awaitingBoundTab: boolean,
): boolean =>
	awaitingBoundTab ||
	(verdict === "notPremium" && tab.premiumGate?.whenLocked === "lockTab");

export const selectionBodyOf = (
	question: SelectionBodyQuestion,
): SelectionBody => {
	const { tab, verdict, sourcesStatus } = question;
	if (awaitsBoundSourceTab(question)) {
		return sourcesStatus === "loading"
			? { kind: "loading" }
			: { kind: UNAVAILABLE };
	}
	const gate = tab.premiumGate;
	if (gate !== undefined && verdict === "notPremium") {
		return { kind: PREMIUM_NOTICE, notice: gate.notice };
	}
	if (gate !== undefined && verdict === "unknown") {
		return { kind: "loading" };
	}
	return contentBodyOf(question);
};

/**
 * Whatever stands under the form reads as the dialog's verdict on it, so nothing stands there, and
 * nothing can be saved, while the dialog is still waiting to hear what it may offer.
 */
export const dialogKnowsItsAnswer = (
	sourcesStatus: SourcesStatus,
	verdict: PremiumVerdict,
): boolean => sourcesStatus !== "loading" && verdict !== "unknown";

const BODIES_THAT_BLOCK_SAVING: ReadonlySet<SelectionBody["kind"]> = new Set([
	"loading",
	UNAVAILABLE,
	FEATURE_LIST_LOADING,
	FEATURE_LIST_FAILED,
]);

const bodyBlocksSaving = (body: SelectionBody): boolean =>
	BODIES_THAT_BLOCK_SAVING.has(body.kind);

export const saveIsAllowed = (
	knowsItsAnswer: boolean,
	blockingError: string | null,
	body: SelectionBody,
): boolean =>
	knowsItsAnswer && blockingError === null && !bodyBlocksSaving(body);
