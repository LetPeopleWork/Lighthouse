import AutoModeIcon from "@mui/icons-material/AutoMode";
import LinkIcon from "@mui/icons-material/Link";
import TouchAppIcon from "@mui/icons-material/TouchApp";
import type React from "react";
import type { Delivery } from "../../../../../models/Delivery";
import { CANNOT_FORECAST_SHORT } from "../../../../../utils/forecast/cannotForecast";
import { formatLikelihood } from "../../../../../utils/forecast/formatLikelihood";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "../../../../../utils/forecast/insufficientForecastData";
import { jointLikelihoodLabel } from "../../../../../utils/forecast/jointLikelihoodLabel";
import DeliveryPublishRefusedNotice from "./DeliveryPublishRefusedNotice";
import DeliverySourceUnavailableNotice from "./DeliverySourceUnavailableNotice";

/**
 * The marker beside a Delivery's name, and the sentence a pointer gets for it. Out here rather than
 * inline because the component is at Sonar's cognitive-complexity limit and every branch left in it
 * costs one — but also because this is a lookup, not part of rendering: three ways a Delivery's
 * Features can have been chosen, and one suffix for the reader who is allowed to undo the third.
 */
export function howTheFeaturesWereChosen({
	isSourceBound,
	isRuleBased,
	canUnbind,
	sourceLabel,
	featuresTerm,
}: {
	isSourceBound: boolean;
	isRuleBased: boolean;
	canUnbind: boolean;
	sourceLabel: string;
	featuresTerm: string;
}): { Icon: typeof TouchAppIcon; hint: string } {
	const chosen = (() => {
		if (isSourceBound) {
			return { Icon: LinkIcon, hint: `Bound to ${sourceLabel}` };
		}

		if (isRuleBased) {
			return {
				Icon: AutoModeIcon,
				hint: `Rule-Based: ${featuresTerm} automatically update based on rules`,
			};
		}

		return { Icon: TouchAppIcon, hint: `Manual: ${featuresTerm} are fixed` };
	})();

	return canUnbind
		? { ...chosen, hint: `${chosen.hint} — click to stop following` }
		: chosen;
}

/**
 * Everything a Delivery has to say about the source it follows before the row itself is read. Both
 * notices can stand at once and each sends the reader somewhere different: one says the values below
 * are frozen because nothing is maintaining them, the other says the Delivery is perfectly healthy and
 * one optional thing it does elsewhere did not happen.
 *
 * Out here rather than inline because the component below is at Sonar's cognitive-complexity limit and
 * every branch left in it costs one - but also because this is a statement about the binding, not part
 * of rendering the Delivery.
 */
export const WhatIsWrongWithTheSource: React.FC<{
	delivery: Delivery;
	isSourceBound: boolean;
	sourceLabel: string;
	deliveryTerm: string;
	onUnbind?: () => void;
}> = ({ delivery, isSourceBound, sourceLabel, deliveryTerm, onUnbind }) => (
	<>
		{isSourceBound && delivery.sourceUnavailableReason !== null && (
			<DeliverySourceUnavailableNotice
				reason={delivery.sourceUnavailableReason}
				sourceLabel={sourceLabel}
				lastSyncedOn={delivery.sourceLastSyncedOn}
				onUnbind={onUnbind}
			/>
		)}
		{isSourceBound && delivery.lastPublishRefusalReason !== null && (
			<DeliveryPublishRefusedNotice
				reason={delivery.lastPublishRefusalReason}
				sourceLabel={sourceLabel}
				refusedOn={delivery.lastPublishRefusedOn}
				deliveryTerm={deliveryTerm}
			/>
		)}
	</>
);

/**
 * What the header chip says about the Delivery as a whole. The three answers are mutually exclusive
 * and ordered: no forecast at all beats not enough history, which beats the number itself.
 */
export function whatTheHeaderChipSays({
	delivery,
	featuresTerm,
	cannotBeForecast: deliveryCannotBeForecast,
	hasInsufficientData,
}: {
	delivery: Delivery;
	featuresTerm: string;
	cannotBeForecast: boolean;
	hasInsufficientData: boolean;
}): string {
	if (deliveryCannotBeForecast || delivery.likelihoodPercentage === null) {
		return CANNOT_FORECAST_SHORT;
	}

	if (hasInsufficientData) {
		return INSUFFICIENT_FORECAST_DATA_SHORT;
	}

	return jointLikelihoodLabel({
		term: featuresTerm,
		date: delivery.getFormattedDate(),
		value: formatLikelihood(delivery.likelihoodPercentage, {
			hasRemainingWork: delivery.remainingWork > 0,
			precision: "round",
		}),
	});
}
