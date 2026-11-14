import type { IDelivery } from "../../../../../models/Delivery";
import type { DeliverySourceCurrentSelection } from "./DeliverySourceTab";
import {
	type DeliverySelectionState,
	type DeliverySelectionTab,
	type DeliverySelectionTerms,
	deliveryTabForDelivery,
} from "./deliverySelectionTabs";

/**
 * A date the work tracking system holds, in the form the date field wants. Read in UTC, because that
 * is the day the tracker holds and the day that gets stored. Read as this browser's day instead, a
 * reader west of UTC would be shown the day before the one their board says, and the field would
 * quietly disagree with the record.
 */
export const dateInputValue = (date: Date): string => {
	const month = `${date.getUTCMonth() + 1}`.padStart(2, "0");
	const day = `${date.getUTCDate()}`.padStart(2, "0");

	return `${date.getUTCFullYear()}-${month}-${day}`;
};

/**
 * A delivery that follows a source is released from it by an action of its own, never by wandering
 * onto another tab: a save made that way has everything typed alongside it discarded by the server,
 * so the edit would vanish without a word. Offering only the tab it is bound to closes that road.
 */
export const offeredTabs = (
	tabs: DeliverySelectionTab[],
	editingDelivery: IDelivery | null | undefined,
): DeliverySelectionTab[] => {
	if (!editingDelivery) {
		return tabs;
	}

	const claimed = deliveryTabForDelivery(editingDelivery, tabs);
	return claimed.source ? [claimed] : tabs;
};

/**
 * The entry a delivery being edited already follows, when the tab on screen is the one offering it.
 * The picker is told separately because the offered list hides entries whose Release has shipped or
 * been archived, and a binding to one of those has to survive that.
 */
export const boundSourceSelection = (
	delivery: IDelivery | null | undefined,
	tab: DeliverySelectionTab,
): DeliverySourceCurrentSelection | null => {
	if (!delivery?.sourceReference) {
		return null;
	}

	if (delivery.sourceKey !== tab.source?.key) {
		return null;
	}

	return {
		id: delivery.sourceReference,
		name: delivery.name,
		date: new Date(delivery.date),
	};
};

export const isValidFutureDate = (date: string): boolean => {
	if (!date) {
		return false;
	}

	const dayParts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date);
	if (!dayParts) {
		return false;
	}

	// The field holds a calendar day. Parsing the string directly reads it as UTC midnight, which
	// west of UTC is still the evening before, so tomorrow would count as today.
	const [, year, month, day] = dayParts.map(Number);
	const selectedDate = new Date(year, month - 1, day);

	// The Date constructor rolls a day the calendar lacks (month 13, 30 February) over into the
	// next month or year, so a date that came back as another day was never a real one.
	const isOnTheCalendar =
		selectedDate.getFullYear() === year &&
		selectedDate.getMonth() === month - 1 &&
		selectedDate.getDate() === day;
	if (!isOnTheCalendar) {
		return false;
	}

	const today = new Date();
	today.setHours(0, 0, 0, 0);

	return selectedDate > today;
};

export const nameErrorOf = (
	name: string,
	deliveryTerm: string,
): string | undefined =>
	name.trim() ? undefined : `${deliveryTerm} name is required`;

export const dateErrorOf = (
	date: string,
	deliveryTerm: string,
): string | undefined => {
	if (!date) {
		return `${deliveryTerm} date is required`;
	}
	if (!isValidFutureDate(date)) {
		return `${deliveryTerm} date must be in the future`;
	}
	return undefined;
};

export interface ValidationOptions {
	name: string;
	date: string;
	tab: DeliverySelectionTab;
	state: DeliverySelectionState;
	terms: DeliverySelectionTerms;
	deliveryTerm: string;
}

export const getFirstBlockingError = ({
	name,
	date,
	tab,
	state,
	terms,
	deliveryTerm,
}: ValidationOptions): string | null => {
	const tabError = tab.firstBlockingError(state, terms);

	// A tab that fills the name and the date in itself owns them, so it speaks first. Asking someone
	// to fill in a field they cannot type into names a fix that does not exist, and saying nothing
	// would be kinder than that.
	if (tab.source !== undefined) {
		return tabError;
	}

	return (
		nameErrorOf(name, deliveryTerm) ??
		dateErrorOf(date, deliveryTerm) ??
		tabError
	);
};
