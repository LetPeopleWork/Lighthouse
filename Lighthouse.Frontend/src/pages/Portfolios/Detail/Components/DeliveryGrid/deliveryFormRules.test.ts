import { describe, expect, it } from "vitest";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import {
	boundSourceSelection,
	dateErrorOf,
	dateInputValue,
	getFirstBlockingError,
	isValidFutureDate,
	nameErrorOf,
	offeredTabs,
} from "./deliveryFormRules";
import {
	type DeliverySelectionState,
	type DeliverySelectionTab,
	type DeliverySelectionTerms,
	deliverySelectionTabsFor,
	MANUAL_SELECTION_TAB_KEY,
	RULE_BASED_SELECTION_TAB_KEY,
} from "./deliverySelectionTabs";

// Both terms are renamed away from the seeded defaults on purpose: a tenant who calls a Delivery a
// Launch must never be shown the word "Delivery", and matching terms would let a hardcoded one through.
const terms: DeliverySelectionTerms = {
	featureTerm: "Deliverable",
	deliveryTerm: "Launch",
	deliveriesTerm: "Launches",
};

const JIRA_RELEASE: IDeliverySource = {
	key: "jira-release",
	displayName: "Jira Release",
};

const JIRA_FIX_VERSION: IDeliverySource = {
	key: "jira-fix-version",
	displayName: "Jira Fix Version",
};

// The date field holds a day on this browser's calendar, so the string for "today" is built from the
// local calendar, never from a UTC instant, or a run near midnight would name a different day.
const dayOnThisBrowsersCalendar = (daysFromToday: number): string => {
	const day = new Date();
	day.setDate(day.getDate() + daysFromToday);
	const month = `${day.getMonth() + 1}`.padStart(2, "0");
	const dayOfMonth = `${day.getDate()}`.padStart(2, "0");

	return `${day.getFullYear()}-${month}-${dayOfMonth}`;
};

const formState = (
	overrides: Partial<DeliverySelectionState> = {},
): DeliverySelectionState => ({
	selectedFeatureIds: [],
	rules: [],
	mode: "and",
	sourceReference: null,
	publishForecastToSource: false,
	rulesValidated: false,
	matchedFeaturesLength: 0,
	...overrides,
});

const storedDelivery = (overrides: Partial<IDelivery> = {}): IDelivery =>
	({
		id: 7,
		name: "Autumn launch",
		date: "2026-09-30T00:00:00Z",
		portfolioId: 1,
		features: [],
		selectionMode: DeliverySelectionMode.Manual,
		...overrides,
	}) as IDelivery;

const followingARelease = (
	source: IDeliverySource,
	overrides: Partial<IDelivery> = {},
): IDelivery =>
	storedDelivery({
		selectionMode: DeliverySelectionMode.SourceBound,
		sourceKey: source.key,
		sourceReference: "release-44",
		...overrides,
	});

const allTabs = (): DeliverySelectionTab[] =>
	deliverySelectionTabsFor([JIRA_RELEASE, JIRA_FIX_VERSION], terms);

const tabWithKey = (
	tabs: DeliverySelectionTab[],
	key: string,
): DeliverySelectionTab =>
	tabs.find((tab) => tab.key === key) as DeliverySelectionTab;

describe("a date to launch on has to be a day still to come", () => {
	it.each([
		["no date at all", "", false],
		["yesterday", dayOnThisBrowsersCalendar(-1), false],
		["today", dayOnThisBrowsersCalendar(0), false],
		["tomorrow", dayOnThisBrowsersCalendar(1), true],
	])("%s is judged by whether it is after today", (_day, date, accepted) => {
		expect(
			isValidFutureDate(date),
			`${_day} must be ${accepted ? "accepted" : "refused"} as a launch date: only days after today at local midnight may be saved; fix the comparison so today and the past are refused`,
		).toBe(accepted);
	});

	it.each([
		["no date at all", "", "Launch date is required"],
		[
			"yesterday",
			dayOnThisBrowsersCalendar(-1),
			"Launch date must be in the future",
		],
		[
			"today",
			dayOnThisBrowsersCalendar(0),
			"Launch date must be in the future",
		],
		["tomorrow", dayOnThisBrowsersCalendar(1), undefined],
	])(
		"%s is answered with the message that names its fix",
		(_day, date, message) => {
			expect(
				dateErrorOf(date, terms.deliveryTerm),
				`${_day}: the reader is told what is wrong using their own word for a delivery, and nothing when the date is fine; fix dateErrorOf to name the missing or past date with the delivery term`,
			).toBe(message);
		},
	);
});

describe("a name of nothing but spaces is no name", () => {
	it.each([
		["nothing typed", "", "Launch name is required"],
		["only spaces", "   ", "Launch name is required"],
		["a name", "Autumn launch", undefined],
		["a name with spaces around it", "  Autumn launch  ", undefined],
	])("%s", (_case, name, message) => {
		expect(
			nameErrorOf(name, terms.deliveryTerm),
			`${_case}: a blank or padded-blank name must be refused in the reader's word for a delivery, a real name must not; fix nameErrorOf to trim before judging`,
		).toBe(message);
	});
});

describe("the one thing the form asks for next", () => {
	const manual = (): DeliverySelectionTab =>
		tabWithKey(allTabs(), MANUAL_SELECTION_TAB_KEY);
	const askedFor = (
		name: string,
		date: string,
		state: Partial<DeliverySelectionState> = {},
	) =>
		getFirstBlockingError({
			name,
			date,
			tab: manual(),
			state: formState(state),
			terms,
			deliveryTerm: terms.deliveryTerm,
		});

	it("names the name first, then the date, then the date being past, then what the tab still needs, one at a time", () => {
		const tomorrow = dayOnThisBrowsersCalendar(1);
		const lastWeek = dayOnThisBrowsersCalendar(-7);

		expect(
			[
				askedFor("", ""),
				askedFor("Autumn launch", ""),
				askedFor("Autumn launch", lastWeek),
				askedFor("Autumn launch", tomorrow),
				askedFor("Autumn launch", tomorrow, { selectedFeatureIds: [1] }),
			],
			"with name, date and tab all wrong the name speaks first, then the date (missing, then past), then the tab, and a complete form speaks no more; fix getFirstBlockingError to keep that order",
		).toEqual([
			"Launch name is required",
			"Launch date is required",
			"Launch date must be in the future",
			"At least one deliverable must be selected",
			null,
		]);
	});

	it("lets a tab that fills in the name and the date itself speak alone", () => {
		const jiraRelease = tabWithKey(allTabs(), "source:jira-release");
		const askedOnTheReleaseTab = (sourceReference: string | null) =>
			getFirstBlockingError({
				name: "",
				date: "",
				tab: jiraRelease,
				state: formState({ sourceReference }),
				terms,
				deliveryTerm: terms.deliveryTerm,
			});

		expect(
			[askedOnTheReleaseTab(null), askedOnTheReleaseTab("release-44")],
			"a Release the reader has not picked is the only thing asked for, never a blank name or date they cannot type into, and a picked Release asks for nothing; fix getFirstBlockingError so a source tab's own message stands alone",
		).toEqual(["Pick a Jira Release to see the date it would set.", null]);
	});

	it("asks the rule-based tab for its validation after the name and the date", () => {
		const ruleBased = tabWithKey(allTabs(), RULE_BASED_SELECTION_TAB_KEY);

		expect(
			getFirstBlockingError({
				name: "Autumn launch",
				date: dayOnThisBrowsersCalendar(1),
				tab: ruleBased,
				state: formState(),
				terms,
				deliveryTerm: terms.deliveryTerm,
			}),
			"a rule-based tab whose rules were not validated must still block once name and date are fine; fix getFirstBlockingError to fall through to the tab's own error",
		).toBe("Rules must be validated before saving");
	});
});

describe("the tabs an edit dialog offers", () => {
	it("offers every tab when no delivery is being edited", () => {
		const tabs = allTabs();

		expect(
			[offeredTabs(tabs, undefined), offeredTabs(tabs, null)],
			"creating a delivery offers all the ways to pick, as the same tab values; fix offeredTabs to return the list it was given when there is no delivery",
		).toEqual([tabs, tabs]);
		expect(offeredTabs(tabs, null)).toBe(tabs);
	});

	it("offers every tab when the delivery was picked by hand or by rules", () => {
		const tabs = allTabs();

		expect(
			[
				offeredTabs(tabs, storedDelivery()),
				offeredTabs(
					tabs,
					storedDelivery({ selectionMode: DeliverySelectionMode.RuleBased }),
				),
			],
			"only a delivery that follows a source is held to one tab; fix offeredTabs to return every tab for a manual or rule-based delivery",
		).toEqual([tabs, tabs]);
	});

	it("offers only the tab of the source a delivery follows", () => {
		const tabs = allTabs();
		const fixVersionTab = tabWithKey(tabs, "source:jira-fix-version");

		const offered = offeredTabs(tabs, followingARelease(JIRA_FIX_VERSION));

		expect(
			offered.map((tab: DeliverySelectionTab) => tab.key),
			"a delivery bound to Jira Fix Version is offered that tab alone, so no save can drop the binding silently; fix offeredTabs to return exactly the claiming source tab, not every tab or the first source",
		).toEqual(["source:jira-fix-version"]);
		expect(offered[0]).toBe(fixVersionTab);
	});

	it("offers every tab to a delivery that follows a source this connection no longer offers", () => {
		const tabs = deliverySelectionTabsFor([JIRA_RELEASE], terms);

		expect(
			offeredTabs(tabs, followingARelease(JIRA_FIX_VERSION)),
			"a bound delivery whose source has no tab falls back to the default tab, which is not a source tab, so everything is offered; fix offeredTabs to restrict only when a source tab claims the delivery",
		).toBe(tabs);
	});
});

describe("what the source picker is told it already follows", () => {
	const releaseTab = (): DeliverySelectionTab =>
		tabWithKey(allTabs(), "source:jira-release");

	it("is nothing when there is no delivery or the delivery follows nothing", () => {
		expect(
			[
				boundSourceSelection(undefined, releaseTab()),
				boundSourceSelection(null, releaseTab()),
				boundSourceSelection(
					storedDelivery({ sourceKey: "jira-release" }),
					releaseTab(),
				),
			],
			"no delivery, or one with no source reference, leaves the picker with no current selection; fix boundSourceSelection to return null without a source reference",
		).toEqual([null, null, null]);
	});

	it("is nothing on a tab for another source or for no source", () => {
		const followed = followingARelease(JIRA_RELEASE);

		expect(
			[
				boundSourceSelection(
					followed,
					tabWithKey(allTabs(), "source:jira-fix-version"),
				),
				boundSourceSelection(
					followed,
					tabWithKey(allTabs(), MANUAL_SELECTION_TAB_KEY),
				),
			],
			"the picker of another source, or the Manual tab, must not be told it follows this Release; fix boundSourceSelection to compare the delivery's source key with the tab's",
		).toEqual([null, null]);
	});

	it("is the entry the delivery follows, with its name and date, on the tab of that source", () => {
		const selection = boundSourceSelection(
			followingARelease(JIRA_RELEASE, {
				name: "PAY 2026.10",
				date: "2026-10-15T00:00:00Z",
			}),
			releaseTab(),
		);

		expect(
			selection,
			"the tab of the followed source is told the entry id, the delivery name and its date as a Date; fix boundSourceSelection to return them",
		).toEqual({
			id: "release-44",
			name: "PAY 2026.10",
			date: new Date("2026-10-15T00:00:00Z"),
		});
	});
});

describe("the day a work tracking system holds, written into the date field", () => {
	it.each([
		[
			"a single-digit month and day get a leading zero",
			"2027-01-05T00:00:00Z",
			"2027-01-05",
		],
		[
			"a two-digit month and day are kept",
			"2026-11-23T12:00:00Z",
			"2026-11-23",
		],
		[
			"the last moments of a UTC day stay on that day",
			"2026-10-15T23:30:00Z",
			"2026-10-15",
		],
		[
			"the first moment of a UTC day stays on that day",
			"2026-10-16T00:00:00Z",
			"2026-10-16",
		],
	])("%s", (_case, instant, expected) => {
		expect(
			dateInputValue(new Date(instant)),
			`${_case}: the field must show the day the tracker holds, read in UTC and zero-padded, whatever this browser's time zone; fix dateInputValue to read the UTC parts and pad them`,
		).toBe(expected);
	});
});
