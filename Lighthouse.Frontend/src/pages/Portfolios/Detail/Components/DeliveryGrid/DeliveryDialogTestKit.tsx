/**
 * The Deliveries page and its Add / Edit dialog as a reader meets them while the server is still
 * answering: what the connection offers, which Releases it holds, and whether the licence is premium all
 * arrive late, out of order, or not at all. Each suite drives those answers by hand, so a scenario can
 * stop at the moment a guess used to be shown and look.
 *
 * Every answer the dialog waits for (the ways to pick, the licence, the Release list, the Feature list,
 * the rule fields) is in one of four states: not asked, on its way, failed, or answered. Closing the
 * dialog sends the answers it asks for itself back to "not asked" for the next opening; the ways to
 * pick belong to the page and are asked again only after they failed. An answer that arrives for an
 * opening already closed, or for another Portfolio, changes nothing on screen.
 *
 * The licence and the terminology are mocked in each suite, not here, because a mock is hoisted only in
 * the file that declares it.
 */
import { act, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { expect, vi } from "vitest";
import SnackbarErrorHandler from "../../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IDelivery } from "../../../../../models/Delivery";
import type {
	IDeliverySource,
	IDeliverySourceOption,
} from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";
import { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import type { IWorkItemRuleSchema } from "../../../../../models/WorkItemRules";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../../../tests/renderWithQueryClient";
import PortfolioDeliveryView from "../../PortfolioDeliveryView";
import { DeliveryCreateModal } from "./DeliveryCreateModal";
import {
	datedInProject,
	JIRA_RELEASE_SOURCE,
	mockPortfolio,
} from "./DeliverySourceTabFixture";

export type DeliveryServiceDouble = ReturnType<
	typeof createMockDeliveryService
>;

export type FeatureServiceDouble = ReturnType<typeof createMockFeatureService>;

export const PREMIUM: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
};

export const COMMUNITY: ILicenseStatus = {
	hasLicense: false,
	isValid: false,
	canUsePremiumFeatures: false,
};

/**
 * The copy the maintainer approved with the sketches, in the words of a tenant who renamed a Delivery
 * to a Launch and a Portfolio to a Value Stream, so a noun written into the code instead of looked up
 * shows up as a failure rather than as the seeded default.
 */
export const COPY = {
	busyTab: "Loading…",
	gettingTheReleases: "Getting the Jira Releases…",
	sourcesCouldNotBeChecked:
		"Couldn't check what this Value Stream's connection offers. Close this dialog and open it again to retry.",
	releasesCouldNotBeLoaded:
		"The list of Jira Release entries could not be loaded. Close this dialog and open it again to retry.",
	jiraReleaseIsPremium:
		"Taking a launch date from a Jira Release is a premium feature. Please upgrade your license to use this functionality.",
	ruleBasedIsPremium: "premium license",
	featuresCouldNotBeLoaded:
		"Couldn't load this Value Stream's Deliverables. Close this dialog and open it again to retry.",
	ruleFieldsCouldNotBeLoaded: "Failed to load rule schema. Please try again.",
	nameIsRequired: "Launch name is required",
	selectDeliverables: "Select Deliverables",
} as const;

/** The role every loading spinner of the dialog has, MUI's `CircularProgress` among them. */
export const SPINNER_ROLE = "progressbar";

/** The attribute a body names its state in, the one the page objects wait on. */
const WIDGET_STATUS = "data-widget-status";

/**
 * MUI's class for a warning-coloured notice. The amber is what the reader sees, but no role or name carries it, so a
 * scenario that pins the colour reads this class.
 */
export const AMBER_NOTICE = "MuiAlert-colorWarning";

export interface Deferred<T> {
	promise: Promise<T>;
	resolve: (value: T) => void;
	reject: (reason: unknown) => void;
}

/** An answer the scenario hands over when it chooses to, never on a timer. */
export const anAnswerNotGivenYet = <T,>(): Deferred<T> => {
	let resolve: (value: T) => void = () => undefined;
	let reject: (reason: unknown) => void = () => undefined;
	const promise = new Promise<T>((onAnswer, onFailure) => {
		resolve = onAnswer;
		reject = onFailure;
	});

	return { promise, resolve, reject };
};

export const whenTheServerAnswers = async <T,>(
	answer: Deferred<T>,
	value: T,
): Promise<void> => {
	await act(async () => {
		answer.resolve(value);
		await answer.promise;
	});
};

export const whenTheServerFails = async <T,>(
	answer: Deferred<T>,
	reason: unknown = new Error("The server could not answer"),
): Promise<void> => {
	await act(async () => {
		answer.reject(reason);
		await answer.promise.catch(() => undefined);
	});
};

/** A connection that offers the given sources and holds the given Releases, both answered at once. */
export const aConnectionOffering = (
	sources: IDeliverySource[],
	releases: IDeliverySourceOption[] = [datedInProject],
): DeliveryServiceDouble => {
	const deliveryService = createMockDeliveryService();
	vi.mocked(deliveryService.getDeliverySources).mockResolvedValue(sources);
	vi.mocked(deliveryService.getDeliverySourceOptions).mockResolvedValue(
		releases,
	);

	return deliveryService;
};

export const aConnectionOfferingJiraReleases = (
	releases: IDeliverySourceOption[] = [datedInProject],
): DeliveryServiceDouble =>
	aConnectionOffering([JIRA_RELEASE_SOURCE], releases);

export const aPortfolio = (id: number, name: string): Portfolio => {
	const portfolio = new Portfolio();
	portfolio.id = id;
	portfolio.name = name;
	portfolio.features = [];
	portfolio.involvedTeams = [];

	return portfolio;
};

/** A Feature as the Manual list shows it: a name to tick, under the id the dialog saves. */
export const aFeature = (id: number, name: string): IFeature =>
	({ id, name, referenceId: `PAY-${id}` }) as unknown as IFeature;

export const aPortfolioHolding = (
	id: number,
	name: string,
	features: IFeature[],
): Portfolio => {
	const portfolio = aPortfolio(id, name);
	portfolio.features = features.map((feature) => ({
		id: feature.id,
		name: feature.name,
	}));

	return portfolio;
};

/** The fields a rule may test, named the way the reader sees them in the rule builder. */
export const aRuleSchemaOffering = (
	...displayNames: string[]
): IWorkItemRuleSchema =>
	({
		fields: displayNames.map((displayName, index) => ({
			fieldKey: `field-${index}`,
			displayName,
			isMultiValue: false,
		})),
		operators: ["equals"],
		maxRules: 10,
		maxValueLength: 100,
	}) as unknown as IWorkItemRuleSchema;

export const PAYMENTS_PLATFORM = aPortfolio(1, "Payments Platform");
export const ONBOARDING = aPortfolio(2, "Onboarding");

/** The Launch "PAY 2026.10", which follows the Jira Release of that name. */
export const aLaunchFollowingAJiraRelease = (): IDelivery =>
	({
		id: 42,
		name: "PAY 2026.10",
		date: "2099-10-15T00:00:00Z",
		portfolioId: 1,
		features: [1],
		selectionMode: "SourceBound",
		sourceKey: JIRA_RELEASE_SOURCE.key,
		sourceReference: datedInProject.id,
		concurrencyToken: "v1",
	}) as unknown as IDelivery;

/** The Features of a Portfolio as the server lists them: none, answered at once. */
export const aFeatureServiceListingNothing = (): FeatureServiceDouble => {
	const featureService = createMockFeatureService();
	vi.mocked(featureService.getFeaturesByIds).mockResolvedValue([]);
	return featureService;
};

export interface TheDialog {
	close: () => void;
	reopen: () => void;
	reopenOn: (portfolio: Portfolio) => void;
	/** Draws the dialog again as it stands, which is how a changed licence reaches it. */
	redraw: () => void;
}

interface DialogSetup {
	deliveryService: DeliveryServiceDouble;
	featureService?: FeatureServiceDouble;
	editingDelivery?: IDelivery | null;
	portfolio?: Portfolio;
	/** Whatever else the scenario answers itself, such as the licence for a suite that reads it for real. */
	otherServices?: Partial<IApiServiceContext>;
}

/**
 * The dialog open on its own, under one query cache for its whole life, so that closing and opening
 * it again is the same dialog meeting the same cache, as on the page.
 */
export const openTheDialog = ({
	deliveryService,
	featureService = aFeatureServiceListingNothing(),
	editingDelivery = null,
	portfolio = mockPortfolio,
	otherServices = {},
}: DialogSetup): TheDialog => {
	const context = createMockApiServiceContext({
		...otherServices,
		deliveryService,
		featureService,
	});
	let shown = { open: true, portfolio, editingDelivery };

	const dialog = () => (
		<ApiServiceContext.Provider value={context}>
			<DeliveryCreateModal
				open={shown.open}
				portfolio={shown.portfolio}
				editingDelivery={shown.editingDelivery}
				onClose={vi.fn()}
				onSave={vi.fn()}
				onUpdate={vi.fn()}
			/>
		</ApiServiceContext.Provider>
	);

	const rendered = renderWithQueryClient(dialog());
	const show = (change: Partial<typeof shown>) => {
		shown = { ...shown, ...change };
		rendered.rerender(dialog());
	};

	return {
		close: () => show({ open: false }),
		reopen: () => show({ open: true }),
		reopenOn: (next) => show({ open: true, portfolio: next }),
		redraw: () => show({}),
	};
};

export interface TheDeliveriesPage {
	/** Editing allowed only now, as with authentication on, where the reader's rights arrive late. */
	grantEditing: () => void;
	redraw: () => void;
}

interface PageSetup {
	deliveryService: DeliveryServiceDouble;
	canEdit?: boolean;
	portfolio?: Portfolio;
}

/** The Deliveries tab of a Portfolio, as the router shows it. */
export const visitTheDeliveriesPage = ({
	deliveryService,
	canEdit = true,
	portfolio = PAYMENTS_PLATFORM,
}: PageSetup): TheDeliveriesPage => {
	const context = createMockApiServiceContext({
		deliveryService,
		featureService: createMockFeatureService(),
	});
	let editing = canEdit;

	const page = () => (
		<ApiServiceContext.Provider value={context}>
			<MemoryRouter>
				<SnackbarErrorHandler>
					<PortfolioDeliveryView portfolio={portfolio} canEdit={editing} />
				</SnackbarErrorHandler>
			</MemoryRouter>
		</ApiServiceContext.Provider>
	);

	const rendered = renderWithQueryClient(page());

	return {
		grantEditing: () => {
			editing = true;
			rendered.rerender(page());
		},
		redraw: () => rendered.rerender(page()),
	};
};

export const theTabRow = () =>
	within(screen.getByRole("group", { name: "Selection Mode" }));

export const theTab = (label: string): HTMLElement | null =>
	theTabRow().queryByRole("button", { name: label });

export const theManualTab = (): HTMLElement | null => theTab("Manual");

export const theRuleBasedTab = (): HTMLElement | null => theTab("Rule-Based");

export const theJiraReleaseTab = (): HTMLElement | null =>
	theTab(JIRA_RELEASE_SOURCE.displayName);

/** Found by its name and then held to it exactly, since a role query matches any name containing it. */
export const theBusyTab = (): HTMLElement | null => {
	const candidate = theTab(COPY.busyTab);
	if (candidate !== null) {
		expect(candidate).toHaveAccessibleName(COPY.busyTab);
	}
	return candidate;
};

export const thePremiumNotice = (): HTMLElement | null =>
	screen.queryByTestId("premium-feature-notice");

/** The body of a source's tab, which says through its status whether its list is on screen yet. */
export const theSourceTabBody = (): HTMLElement | null =>
	screen.queryByTestId("delivery-source-tab-body");

/** What the source tab's body says it is doing: loading, error or ready; null while it says nothing. */
export const theSourceTabBodyStatus = (): string | null =>
	theSourceTabBody()?.getAttribute(WIDGET_STATUS) ?? null;

export const theReleasePicker = (): HTMLElement | null =>
	screen.queryByRole("combobox", { name: JIRA_RELEASE_SOURCE.displayName });

export const theManualFeatureList = (): HTMLElement | null =>
	screen.queryByTestId("feature-grid-mock");

/** The notice a sentence sits in, so its look and what else it holds can be read. */
export const theNoticeSaying = (sentence: string): HTMLElement => {
	const notice = screen.getByText(sentence).closest('[role="alert"]');
	expect(notice, `no notice says "${sentence}"`).not.toBeNull();
	return notice as HTMLElement;
};

export const sourcesAskedFor = (deliveryService: DeliveryServiceDouble) =>
	vi.mocked(deliveryService.getDeliverySources).mock.calls.length;

export const releaseListsAskedFor = (deliveryService: DeliveryServiceDouble) =>
	vi.mocked(deliveryService.getDeliverySourceOptions).mock.calls.length;

export const featureListsAskedFor = (featureService: FeatureServiceDouble) =>
	vi.mocked(featureService.getFeaturesByIds).mock.calls.length;

export const ruleFieldsAskedFor = (deliveryService: DeliveryServiceDouble) =>
	vi.mocked(deliveryService.getRuleSchema).mock.calls.length;

export const untilTheJiraReleaseTabIsOffered = async (): Promise<HTMLElement> =>
	waitFor(() => {
		const tab = theJiraReleaseTab();
		expect(tab).not.toBeNull();
		return tab as HTMLElement;
	});

export const untilThePickerIsReady = async (): Promise<void> => {
	await waitFor(() => {
		expect(theSourceTabBodyStatus()).toBe("ready");
	});
	expect(theReleasePicker()).not.toBeNull();
};
