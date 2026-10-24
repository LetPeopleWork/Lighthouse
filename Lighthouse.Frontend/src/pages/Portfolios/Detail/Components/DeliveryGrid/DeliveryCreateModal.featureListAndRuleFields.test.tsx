import { notifyManager } from "@tanstack/react-query";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IFeature } from "../../../../../models/Feature";
import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type {
	IWorkItemRuleFieldDefinition,
	IWorkItemRuleSchema,
} from "../../../../../models/WorkItemRules";
import { createMockFeatureService } from "../../../../../tests/MockApiServiceProvider";
import {
	AMBER_NOTICE,
	aConnectionOffering,
	aFeature,
	aFeatureServiceListingNothing,
	anAnswerNotGivenYet,
	aPortfolioHolding,
	aRuleSchemaOffering,
	COMMUNITY,
	COPY,
	type Deferred,
	type FeatureServiceDouble,
	featureListsAskedFor,
	openTheDialog,
	PREMIUM,
	ruleFieldsAskedFor,
	SPINNER_ROLE,
	theManualFeatureList,
	theManualTab,
	theNoticeSaying,
	thePremiumNotice,
	theRuleBasedTab,
	whenTheServerAnswers,
	whenTheServerFails,
} from "./DeliveryDialogTestKit";

/**
 * The Manual tab's list of Features and the Rule-Based tab's fields are the dialog's two other answers
 * from the server. While one is on its way the tab says it is loading; when it could not be loaded the
 * tab says so; and neither ever shows an earlier opening's answer, or another Portfolio's, as this one's.
 */

const licence = vi.hoisted(() => ({
	status: null as ILicenseStatus | null,
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: licence.status,
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.DELIVERY]: "Launch",
				[TERMINOLOGY_KEYS.DELIVERIES]: "Launches",
				[TERMINOLOGY_KEYS.FEATURES]: "Deliverables",
				[TERMINOLOGY_KEYS.FEATURE]: "Deliverable",
				[TERMINOLOGY_KEYS.PORTFOLIO]: "Value Stream",
			};
			return terms[key] ?? key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

// The real grid draws through MUI's data grid, whose busy overlay is a look; this one says the same
// thing as an attribute, so a scenario reads whether the grid was told it is loading.
vi.mock("../../../../../components/Common/FeatureGrid", () => ({
	FeatureGrid: ({
		features,
		selectedFeatureIds = [],
		loading = false,
	}: {
		features: IFeature[];
		selectedFeatureIds?: number[];
		loading?: boolean;
	}) => (
		<table data-testid="feature-grid-mock" aria-busy={loading}>
			<tbody>
				{features.map((feature) => (
					<tr
						key={feature.id}
						aria-selected={selectedFeatureIds.includes(feature.id)}
					>
						<td>{feature.name}</td>
					</tr>
				))}
			</tbody>
		</table>
	),
}));

vi.mock("../../../../../components/Common/DeliveryRuleBuilder", () => ({
	DeliveryRuleBuilder: ({
		fields,
	}: {
		fields: IWorkItemRuleFieldDefinition[];
	}) => (
		<ul data-testid="delivery-rule-builder">
			{fields.map((field) => (
				<li key={field.fieldKey}>{field.displayName}</li>
			))}
		</ul>
	),
}));

// The query cache tells its readers on a timer by default, which lands after the act() a scenario
// hands an answer over in; told on a microtask instead, the answer is on screen when act() returns.
notifyManager.setScheduler(queueMicrotask);

beforeEach(() => {
	vi.clearAllMocks();
	licence.status = PREMIUM;
});

const CHECKOUT = aFeature(1, "Checkout redesign");
const REFUNDS = aFeature(2, "Instant refunds");
const SIGN_UP = aFeature(11, "Guided sign-up");

const PAYMENTS_PLATFORM = aPortfolioHolding(1, "Payments Platform", [
	CHECKOUT,
	REFUNDS,
]);
const ONBOARDING = aPortfolioHolding(2, "Onboarding", [SIGN_UP]);
const A_NEW_VALUE_STREAM = aPortfolioHolding(3, "Partner API", []);

const AUTUMN_LAUNCH = {
	id: 7,
	name: "Autumn launch",
	date: "2099-11-20T00:00:00Z",
	portfolioId: 1,
	features: [CHECKOUT.id, REFUNDS.id],
	selectionMode: "Manual",
	concurrencyToken: "v1",
} as unknown as IDelivery;

const EVERYTHING_TAGGED_PAYMENTS = {
	id: 8,
	name: "Everything tagged Payments",
	date: "2099-12-01T00:00:00Z",
	portfolioId: 1,
	features: [CHECKOUT.id],
	selectionMode: "RuleBased",
	rules: [{ fieldKey: "field-0", operator: "equals", value: "Payments" }],
	concurrencyToken: "v1",
} as unknown as IDelivery;

const THE_FIELDS = aRuleSchemaOffering("Team", "Tags");
const THE_FIELDS_OF_AN_EARLIER_OPENING = aRuleSchemaOffering("Old field");

const aFeatureServiceAnswering = (
	...answers: Deferred<IFeature[]>[]
): FeatureServiceDouble => {
	const featureService = aFeatureServiceListingNothing();
	for (const answer of answers) {
		vi.mocked(featureService.getFeaturesByIds).mockReturnValueOnce(
			answer.promise,
		);
	}
	return featureService;
};

const aConnectionWhoseRuleFieldsAnswer = (
	...answers: Deferred<IWorkItemRuleSchema>[]
) => {
	const deliveryService = aConnectionOffering([]);
	for (const answer of answers) {
		vi.mocked(deliveryService.getRuleSchema).mockReturnValueOnce(
			answer.promise,
		);
	}
	return deliveryService;
};

const theGridIsBusy = () =>
	expect(theManualFeatureList()).toHaveAttribute("aria-busy", "true");

const theGridIsReady = () =>
	expect(theManualFeatureList()).toHaveAttribute("aria-busy", "false");

const theRowFor = (feature: IFeature): HTMLElement | null =>
	screen.queryByRole("row", { name: feature.name });

const theRuleFieldsSpinner = () => screen.queryByRole(SPINNER_ROLE);

const theRuleBuilderOffers = (displayName: string) =>
	within(screen.getByTestId("delivery-rule-builder")).getByText(displayName);

/** Up to the moment it is asked, whether a sentence was ever put on screen, even for a single frame. */
const watchFor = (sentence: string) => {
	let shown = false;
	const holdsTheSentence = (node: Node | null) =>
		node?.textContent?.includes(sentence) ?? false;
	// A sentence can arrive as a new element or as new text in an element already on screen, so both count.
	const look = (records: MutationRecord[]) => {
		for (const record of records) {
			if (record.type === "characterData") {
				shown ||= holdsTheSentence(record.target.parentNode);
			}
			for (const node of record.addedNodes) {
				shown ||= holdsTheSentence(node);
			}
		}
	};
	const observer = new MutationObserver(look);
	observer.observe(document.body, {
		childList: true,
		subtree: true,
		characterData: true,
	});

	return {
		wasEverShown: () => {
			look(observer.takeRecords());
			observer.disconnect();
			return shown;
		},
	};
};

const chooseRuleBased = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(theRuleBasedTab() as HTMLElement);
};

const chooseManual = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(theManualTab() as HTMLElement);
};

describe("Lena picks Deliverables by hand on Payments Platform", () => {
	it("while the list is on its way the grid says it is busy, and it fills once the list arrives", async () => {
		const list = anAnswerNotGivenYet<IFeature[]>();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService: aFeatureServiceAnswering(list),
			portfolio: PAYMENTS_PLATFORM,
		});

		theGridIsBusy();
		await waitFor(theGridIsBusy);
		expect(theRowFor(CHECKOUT)).toBeNull();

		await whenTheServerAnswers(list, [CHECKOUT, REFUNDS]);

		await waitFor(theGridIsReady);
		expect(theRowFor(CHECKOUT)).not.toBeNull();
		expect(theRowFor(REFUNDS)).not.toBeNull();
	});

	it("asks for exactly the Value Stream's own Deliverables", async () => {
		const featureService = aFeatureServiceListingNothing();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService,
			portfolio: PAYMENTS_PLATFORM,
		});

		await waitFor(() =>
			expect(featureService.getFeaturesByIds).toHaveBeenCalledWith([
				CHECKOUT.id,
				REFUNDS.id,
			]),
		);
	});

	it("while the list is on its way the grid holds no row at all, not even an empty one", async () => {
		const list = anAnswerNotGivenYet<IFeature[]>();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService: aFeatureServiceAnswering(list),
			portfolio: PAYMENTS_PLATFORM,
		});

		await waitFor(theGridIsBusy);
		expect(
			within(theManualFeatureList() as HTMLElement).queryAllByRole("row"),
		).toHaveLength(0);
	});

	it("a list that could not be loaded is an amber notice (the warning colour, which only its class carries) in her words, with no button and no empty grid", async () => {
		const list = anAnswerNotGivenYet<IFeature[]>();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService: aFeatureServiceAnswering(list),
			portfolio: PAYMENTS_PLATFORM,
		});

		await whenTheServerFails(list);

		const notice = await waitFor(() =>
			theNoticeSaying(COPY.featuresCouldNotBeLoaded),
		);
		expect(notice).toHaveClass(AMBER_NOTICE);
		expect(within(notice).queryByRole("button")).toBeNull();
		expect(theManualFeatureList()).toBeNull();
	});

	it("reopening the dialog after that asks again and shows the list", async () => {
		const failed = anAnswerNotGivenYet<IFeature[]>();
		const featureService = aFeatureServiceAnswering(failed);
		const dialog = openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService,
			portfolio: PAYMENTS_PLATFORM,
		});
		await whenTheServerFails(failed);
		dialog.close();
		vi.mocked(featureService.getFeaturesByIds).mockResolvedValue([
			CHECKOUT,
			REFUNDS,
		]);

		dialog.reopen();

		await waitFor(() => expect(theRowFor(CHECKOUT)).not.toBeNull());
		expect(screen.queryByText(COPY.featuresCouldNotBeLoaded)).toBeNull();
		expect(featureListsAskedFor(featureService)).toBe(2);
	});

	it("reopening never shows the last opening's list, only the busy grid until this opening's list arrives", async () => {
		const first = anAnswerNotGivenYet<IFeature[]>();
		const second = anAnswerNotGivenYet<IFeature[]>();
		const dialog = openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService: aFeatureServiceAnswering(first, second),
			portfolio: PAYMENTS_PLATFORM,
		});
		await whenTheServerAnswers(first, [CHECKOUT, REFUNDS]);
		await waitFor(() => expect(theRowFor(CHECKOUT)).not.toBeNull());
		dialog.close();

		dialog.reopen();

		await waitFor(theGridIsBusy);
		expect(theRowFor(CHECKOUT)).toBeNull();
		expect(theRowFor(REFUNDS)).toBeNull();
	});

	it.each<[string, Portfolio]>([
		["Payments Platform again", PAYMENTS_PLATFORM],
		[ONBOARDING.name, ONBOARDING],
	])(
		"an answer for an opening she closed never fills the list when she reopens on %s",
		async (_, next) => {
			const closed = anAnswerNotGivenYet<IFeature[]>();
			const current = anAnswerNotGivenYet<IFeature[]>();
			const dialog = openTheDialog({
				deliveryService: aConnectionOffering([]),
				featureService: aFeatureServiceAnswering(closed, current),
				portfolio: PAYMENTS_PLATFORM,
			});
			dialog.close();
			dialog.reopenOn(next);

			await whenTheServerAnswers(closed, [CHECKOUT, REFUNDS]);

			expect(theRowFor(CHECKOUT)).toBeNull();
			theGridIsBusy();

			await whenTheServerAnswers(current, [SIGN_UP]);

			await waitFor(() => expect(theRowFor(SIGN_UP)).not.toBeNull());
			expect(theRowFor(CHECKOUT)).toBeNull();
		},
	);

	it("a Value Stream with no Deliverables asks for nothing and shows its empty grid at once", () => {
		const featureService = createMockFeatureService();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService,
			portfolio: A_NEW_VALUE_STREAM,
		});

		theGridIsReady();
		expect(screen.queryAllByRole("row")).toHaveLength(0);
		expect(screen.queryByText(COPY.featuresCouldNotBeLoaded)).toBeNull();
		expect(featureListsAskedFor(featureService)).toBe(0);
	});

	it("editing Autumn launch, Update stays off until the list arrives, and her ticks arrive with it", async () => {
		const list = anAnswerNotGivenYet<IFeature[]>();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService: aFeatureServiceAnswering(list),
			portfolio: PAYMENTS_PLATFORM,
			editingDelivery: AUTUMN_LAUNCH,
		});

		await waitFor(theGridIsBusy);
		expect(screen.getByRole("button", { name: "Update" })).toBeDisabled();

		await whenTheServerAnswers(list, [CHECKOUT, REFUNDS]);

		await waitFor(theGridIsReady);
		expect(theRowFor(CHECKOUT)).toHaveAttribute("aria-selected", "true");
		expect(theRowFor(REFUNDS)).toHaveAttribute("aria-selected", "true");
		expect(screen.getByRole("button", { name: "Update" })).toBeEnabled();
	});

	it("editing Autumn launch, Update stays off when the list could not be loaded", async () => {
		const list = anAnswerNotGivenYet<IFeature[]>();
		openTheDialog({
			deliveryService: aConnectionOffering([]),
			featureService: aFeatureServiceAnswering(list),
			portfolio: PAYMENTS_PLATFORM,
			editingDelivery: AUTUMN_LAUNCH,
		});

		await whenTheServerFails(list);

		const notice = await waitFor(() =>
			theNoticeSaying(COPY.featuresCouldNotBeLoaded),
		);
		expect(notice).toHaveClass(AMBER_NOTICE);
		expect(screen.getByRole("button", { name: "Update" })).toBeDisabled();
	});
});

describe("the rule fields are asked for whenever Rule-Based is on screen with a premium licence, however it got there", () => {
	it("editing a Rule-Based Launch asks for its fields once without a click, and moving to Manual and back asks no more", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOffering([]);
		vi.mocked(deliveryService.getRuleSchema).mockResolvedValue(THE_FIELDS);
		openTheDialog({
			deliveryService,
			portfolio: PAYMENTS_PLATFORM,
			editingDelivery: EVERYTHING_TAGGED_PAYMENTS,
		});

		await waitFor(() =>
			expect(theRuleBuilderOffers("Team")).toBeInTheDocument(),
		);
		expect(ruleFieldsAskedFor(deliveryService)).toBe(1);

		await chooseManual(user);
		await chooseRuleBased(user);

		expect(theRuleBuilderOffers("Team")).toBeInTheDocument();
		expect(ruleFieldsAskedFor(deliveryService)).toBe(1);
	});

	it("chosen before the licence has answered, Rule-Based shows the spinner and asks nothing; once the licence answers premium, it asks once", async () => {
		licence.status = null;
		const user = userEvent.setup();
		const deliveryService = aConnectionOffering([]);
		vi.mocked(deliveryService.getRuleSchema).mockResolvedValue(THE_FIELDS);
		const notice = watchFor(COPY.ruleFieldsCouldNotBeLoaded);
		const dialog = openTheDialog({
			deliveryService,
			portfolio: PAYMENTS_PLATFORM,
		});

		expect(theRuleBasedTab()).toBeEnabled();
		await chooseRuleBased(user);

		expect(theRuleFieldsSpinner()).not.toBeNull();
		expect(ruleFieldsAskedFor(deliveryService)).toBe(0);

		licence.status = PREMIUM;
		dialog.redraw();

		await waitFor(() =>
			expect(theRuleBuilderOffers("Team")).toBeInTheDocument(),
		);
		expect(ruleFieldsAskedFor(deliveryService)).toBe(1);
		expect(notice.wasEverShown()).toBe(false);
	});

	it("chosen before the licence has answered, Rule-Based never asks when the licence then answers not premium, and shows the premium notice", async () => {
		licence.status = null;
		const user = userEvent.setup();
		const deliveryService = aConnectionOffering([]);
		const dialog = openTheDialog({
			deliveryService,
			portfolio: PAYMENTS_PLATFORM,
		});
		expect(theRuleBasedTab()).toBeEnabled();
		await chooseRuleBased(user);

		licence.status = COMMUNITY;
		dialog.redraw();

		await waitFor(() => expect(thePremiumNotice()).not.toBeNull());
		expect(ruleFieldsAskedFor(deliveryService)).toBe(0);
		expect(screen.queryByText(COPY.ruleFieldsCouldNotBeLoaded)).toBeNull();
	});
});

describe("Lena chooses Rule-Based on a premium licence", () => {
	it("the spinner shows from the first moment, never the could-not-load notice, until the fields arrive", async () => {
		const user = userEvent.setup();
		const fields = anAnswerNotGivenYet<IWorkItemRuleSchema>();
		openTheDialog({
			deliveryService: aConnectionWhoseRuleFieldsAnswer(fields),
			portfolio: PAYMENTS_PLATFORM,
		});
		const notice = watchFor(COPY.ruleFieldsCouldNotBeLoaded);

		await chooseRuleBased(user);

		expect(theRuleFieldsSpinner()).not.toBeNull();
		await whenTheServerAnswers(fields, THE_FIELDS);
		expect(theRuleBuilderOffers("Team")).toBeInTheDocument();
		expect(notice.wasEverShown()).toBe(false);
	});

	it("nothing is asked while she stays on Manual, and once she has chosen Rule-Based, moving between the tabs never asks again", async () => {
		const user = userEvent.setup();
		const fields = anAnswerNotGivenYet<IWorkItemRuleSchema>();
		const deliveryService = aConnectionWhoseRuleFieldsAnswer(fields);
		openTheDialog({ deliveryService, portfolio: PAYMENTS_PLATFORM });
		expect(ruleFieldsAskedFor(deliveryService)).toBe(0);

		await chooseRuleBased(user);
		await whenTheServerAnswers(fields, THE_FIELDS);
		await chooseManual(user);
		await chooseRuleBased(user);

		expect(theRuleBuilderOffers("Team")).toBeInTheDocument();
		expect(ruleFieldsAskedFor(deliveryService)).toBe(1);
	});

	it("fields that could not be loaded show today's notice and are not asked again in that opening, whichever tab she moves to", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOffering([]);
		vi.mocked(deliveryService.getRuleSchema).mockRejectedValue(
			new Error("The server could not answer"),
		);
		openTheDialog({ deliveryService, portfolio: PAYMENTS_PLATFORM });

		await chooseRuleBased(user);
		await screen.findByText(COPY.ruleFieldsCouldNotBeLoaded);
		await chooseManual(user);
		await chooseRuleBased(user);

		expect(
			await screen.findByText(COPY.ruleFieldsCouldNotBeLoaded),
		).toBeInTheDocument();
		await new Promise((settle) => setTimeout(settle, 50));
		expect(ruleFieldsAskedFor(deliveryService)).toBe(1);
	});

	it("reopening the dialog is the retry: the next opening asks once more and shows the fields", async () => {
		const user = userEvent.setup();
		const failed = anAnswerNotGivenYet<IWorkItemRuleSchema>();
		const deliveryService = aConnectionWhoseRuleFieldsAnswer(failed);
		const dialog = openTheDialog({
			deliveryService,
			portfolio: PAYMENTS_PLATFORM,
		});
		await chooseRuleBased(user);
		vi.mocked(deliveryService.getRuleSchema).mockResolvedValue(THE_FIELDS);
		await whenTheServerFails(failed);
		await screen.findByText(COPY.ruleFieldsCouldNotBeLoaded);
		await new Promise((settle) => setTimeout(settle, 50));
		expect(ruleFieldsAskedFor(deliveryService)).toBe(1);
		dialog.close();

		dialog.reopen();
		await chooseRuleBased(user);

		await waitFor(() =>
			expect(theRuleBuilderOffers("Team")).toBeInTheDocument(),
		);
		expect(ruleFieldsAskedFor(deliveryService)).toBe(2);
	});

	it.each<[string, Portfolio]>([
		["Payments Platform again", PAYMENTS_PLATFORM],
		[ONBOARDING.name, ONBOARDING],
	])(
		"an answer for an opening she closed never fills the rule builder when she reopens on %s",
		async (_, next) => {
			const user = userEvent.setup();
			const closed = anAnswerNotGivenYet<IWorkItemRuleSchema>();
			const current = anAnswerNotGivenYet<IWorkItemRuleSchema>();
			const deliveryService = aConnectionWhoseRuleFieldsAnswer(closed, current);
			const dialog = openTheDialog({
				deliveryService,
				portfolio: PAYMENTS_PLATFORM,
			});
			await chooseRuleBased(user);
			dialog.close();
			dialog.reopenOn(next);
			await chooseRuleBased(user);

			await whenTheServerAnswers(closed, THE_FIELDS_OF_AN_EARLIER_OPENING);

			expect(screen.queryByText("Old field")).toBeNull();
			expect(theRuleFieldsSpinner()).not.toBeNull();

			await whenTheServerAnswers(current, THE_FIELDS);

			expect(theRuleBuilderOffers("Team")).toBeInTheDocument();
			expect(screen.queryByText("Old field")).toBeNull();
			expect(ruleFieldsAskedFor(deliveryService)).toBe(2);
		},
	);

	it("editing a Rule-Based Launch, the spinner shows while its fields load, never the notice, and Update stays off", async () => {
		const fields = anAnswerNotGivenYet<IWorkItemRuleSchema>();
		const notice = watchFor(COPY.ruleFieldsCouldNotBeLoaded);
		openTheDialog({
			deliveryService: aConnectionWhoseRuleFieldsAnswer(fields),
			portfolio: PAYMENTS_PLATFORM,
			editingDelivery: EVERYTHING_TAGGED_PAYMENTS,
		});

		await waitFor(() => expect(theRuleFieldsSpinner()).not.toBeNull());
		expect(screen.getByRole("button", { name: "Update" })).toBeDisabled();

		await whenTheServerAnswers(fields, THE_FIELDS);

		expect(theRuleBuilderOffers("Team")).toBeInTheDocument();
		expect(notice.wasEverShown()).toBe(false);
	});
});
