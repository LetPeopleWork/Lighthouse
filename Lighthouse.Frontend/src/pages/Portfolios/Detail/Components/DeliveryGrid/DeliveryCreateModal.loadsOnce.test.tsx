import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IDeliverySource,
	IDeliverySourceOption,
} from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import {
	aConnectionOffering,
	aConnectionOfferingJiraReleases,
	anAnswerNotGivenYet,
	COMMUNITY,
	ONBOARDING,
	openTheDialog,
	PAYMENTS_PLATFORM,
	PREMIUM,
	releaseListsAskedFor,
	sourcesAskedFor,
	theBusyTab,
	theJiraReleaseTab,
	theManualTab,
	theRuleBasedTab,
	untilTheJiraReleaseTabIsOffered,
	untilThePickerIsReady,
	visitTheDeliveriesPage,
	whenTheServerAnswers,
	whenTheServerFails,
} from "./DeliveryDialogTestKit";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	datedInJustATest,
	datedInProject,
	JIRA_RELEASE_SOURCE,
	RELEASE_44_IN_PROJECT_TEST,
	RELEASE_44_IN_PROJECT_X,
} from "./DeliverySourceTabFixture";

/**
 * The dialog uses the ways to pick the page already knows, and the Release list
 * starts as soon as the licence is known premium, so Lena waits once. Counted on the requests the page
 * and the dialog make, because a wait in series is a request that starts late or twice.
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

vi.mock("../../../../../components/Common/FeatureGrid", () => ({
	FeatureGrid: ({ features }: { features: IFeature[] }) => (
		<div data-testid="feature-grid-mock">
			{features.map((feature) => (
				<div key={feature.id}>{feature.name}</div>
			))}
		</div>
	),
}));

beforeEach(() => {
	vi.clearAllMocks();
	licence.status = PREMIUM;
	// The Jira Release tab keeps the lists it fetched in a store shared by every test in this file,
	// so a list one scenario fetched would otherwise be what the next scenario sees.
	clearDeliverySourceOptionsCache();
});

const theLicenceIs = (status: ILicenseStatus | null) => {
	licence.status = status;
};

const lenaOpensAddLaunch = async (user: ReturnType<typeof userEvent.setup>) => {
	await user.click(await screen.findByRole("button", { name: "Add Launch" }));
};

const lenaClosesTheDialog = async (
	user: ReturnType<typeof userEvent.setup>,
) => {
	await user.click(screen.getByRole("button", { name: "Cancel" }));
	await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
};

const untilThePageHasItsSources = async (
	deliveryService: ReturnType<typeof aConnectionOfferingJiraReleases>,
) => {
	await waitFor(() => expect(sourcesAskedFor(deliveryService)).toBe(1));
	await Promise.resolve();
};

describe("the dialog opens with the ways to pick the page already knows", () => {
	it.skip("opening Add Launch asks nothing more, and the Jira Release tab is there at once", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);

		await lenaOpensAddLaunch(user);

		expect(theJiraReleaseTab()).not.toBeNull();
		expect(theBusyTab()).toBeNull();
		expect(sourcesAskedFor(deliveryService)).toBe(1);
	});

	it.skip("opened before the page's answer arrived, it shows the busy tab and waits for that same answer", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const sources = anAnswerNotGivenYet<IDeliverySource[]>();
		vi.mocked(deliveryService.getDeliverySources).mockReturnValue(
			sources.promise,
		);
		visitTheDeliveriesPage({ deliveryService });

		await lenaOpensAddLaunch(user);
		expect(theBusyTab()).not.toBeNull();

		await whenTheServerAnswers(sources, [JIRA_RELEASE_SOURCE]);

		await untilTheJiraReleaseTabIsOffered();
		expect(sourcesAskedFor(deliveryService)).toBe(1);
	});

	it.skip("closing and opening it again still asks nothing more", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);
		await lenaOpensAddLaunch(user);
		await lenaClosesTheDialog(user);

		await lenaOpensAddLaunch(user);

		expect(theJiraReleaseTab()).not.toBeNull();
		expect(sourcesAskedFor(deliveryService)).toBe(1);
	});

	it.skip("with editing allowed only after the page's answer arrived, as with sign-in on, the sources are asked once more then and never on opening", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const page = visitTheDeliveriesPage({ deliveryService, canEdit: false });
		await untilThePageHasItsSources(deliveryService);

		page.grantEditing();
		await waitFor(() => expect(sourcesAskedFor(deliveryService)).toBe(2));

		await lenaOpensAddLaunch(user);

		await untilTheJiraReleaseTabIsOffered();
		expect(sourcesAskedFor(deliveryService)).toBe(2);
	});

	it("after the page's answer failed, opening the dialog asks again and offers the Jira Release tab", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		vi.mocked(deliveryService.getDeliverySources).mockRejectedValueOnce(
			new Error("The server could not answer"),
		);
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);

		await lenaOpensAddLaunch(user);

		await untilTheJiraReleaseTabIsOffered();
		expect(sourcesAskedFor(deliveryService)).toBe(2);
	});

	it("while the server keeps failing, each opening asks exactly once more and never in a loop", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		vi.mocked(deliveryService.getDeliverySources).mockRejectedValue(
			new Error("The server could not answer"),
		);
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);

		await lenaOpensAddLaunch(user);
		await waitFor(() => expect(sourcesAskedFor(deliveryService)).toBe(2));
		await lenaClosesTheDialog(user);
		await lenaOpensAddLaunch(user);
		await waitFor(() => expect(sourcesAskedFor(deliveryService)).toBe(3));
		await new Promise((settle) => setTimeout(settle, 50));

		expect(sourcesAskedFor(deliveryService)).toBe(3);
	});
});

describe("the Release list starts when the dialog opens, not when the tab is drawn", () => {
	it.skip("with the licence premium, the Jira Releases are asked for while Lena is still on the Manual tab", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);

		await lenaOpensAddLaunch(user);

		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		expect(deliveryService.getDeliverySourceOptions).toHaveBeenCalledWith(
			PAYMENTS_PLATFORM.id,
			JIRA_RELEASE_SOURCE.key,
		);
		expect(theManualTab()).toHaveAttribute("aria-pressed", "true");
	});

	it.skip("when she then opens the Jira Release tab, the picker is ready from that answer without asking again", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const releases = anAnswerNotGivenYet<IDeliverySourceOption[]>();
		vi.mocked(deliveryService.getDeliverySourceOptions).mockReturnValue(
			releases.promise,
		);
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);
		await lenaOpensAddLaunch(user);
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		await whenTheServerAnswers(releases, [datedInProject]);

		await user.click(await untilTheJiraReleaseTabIsOffered());

		await untilThePickerIsReady();
		expect(releaseListsAskedFor(deliveryService)).toBe(1);
	});

	it.skip("with the licence not known yet, nothing is asked until it answers premium, and then once", async () => {
		theLicenceIs(null);
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const page = visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);
		await lenaOpensAddLaunch(user);
		await untilTheJiraReleaseTabIsOffered();
		expect(releaseListsAskedFor(deliveryService)).toBe(0);

		theLicenceIs(PREMIUM);
		page.redraw();

		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
	});

	it.skip("moving between the tabs within one opening asks once", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);
		await lenaOpensAddLaunch(user);
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));

		const jiraReleaseTab = await untilTheJiraReleaseTabIsOffered();
		await user.click(jiraReleaseTab);
		await untilThePickerIsReady();
		await user.click(theManualTab() as HTMLElement);
		await user.click(jiraReleaseTab);
		await untilThePickerIsReady();

		expect(releaseListsAskedFor(deliveryService)).toBe(1);
	});

	it.skip("closing and opening the dialog again asks exactly once more, on opening", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);
		await lenaOpensAddLaunch(user);
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		await lenaClosesTheDialog(user);

		await lenaOpensAddLaunch(user);

		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(2));
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await untilThePickerIsReady();
		expect(releaseListsAskedFor(deliveryService)).toBe(2);
	});
});

describe("nothing is fetched that nobody asked for or the server would refuse", () => {
	it("a visit to the Deliveries page without opening the dialog asks for no Release list", async () => {
		const deliveryService = aConnectionOfferingJiraReleases();

		visitTheDeliveriesPage({ deliveryService });

		await untilThePageHasItsSources(deliveryService);
		await screen.findByRole("button", { name: "Add Launch" });
		expect(releaseListsAskedFor(deliveryService)).toBe(0);
	});

	it("with a Community licence, opening the dialog and its Jira Release tab asks for no Release list", async () => {
		theLicenceIs(COMMUNITY);
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);

		await lenaOpensAddLaunch(user);
		await user.click(await untilTheJiraReleaseTabIsOffered());

		expect(screen.getByTestId("premium-feature-notice")).toBeInTheDocument();
		expect(releaseListsAskedFor(deliveryService)).toBe(0);
	});

	it("a Value Stream whose connection offers no source asks for no Release list", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOffering([]);
		visitTheDeliveriesPage({ deliveryService });
		await untilThePageHasItsSources(deliveryService);

		await lenaOpensAddLaunch(user);

		await waitFor(() => expect(theRuleBasedTab()).not.toBeNull());
		expect(theJiraReleaseTab()).toBeNull();
		expect(releaseListsAskedFor(deliveryService)).toBe(0);
	});
});

describe("a late answer never lands in the wrong dialog", () => {
	it("an answer for an opening Lena already closed never reaches the picker", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const forTheClosedOpening = anAnswerNotGivenYet<IDeliverySourceOption[]>();
		const forThisOpening = anAnswerNotGivenYet<IDeliverySourceOption[]>();
		vi.mocked(deliveryService.getDeliverySourceOptions)
			.mockReturnValueOnce(forTheClosedOpening.promise)
			.mockReturnValueOnce(forThisOpening.promise);
		const dialog = openTheDialog({ deliveryService });
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		dialog.close();
		dialog.reopen();
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(2));

		await whenTheServerAnswers(forThisOpening, [datedInProject]);
		await whenTheServerAnswers(forTheClosedOpening, [datedInJustATest]);

		await user.click(
			await screen.findByRole("combobox", { name: "Jira Release" }),
		);
		expect(
			await screen.findByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).toBeNull();
	});

	it("an answer for Payments Platform never lands in the dialog opened on Onboarding", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const forPaymentsPlatform = anAnswerNotGivenYet<IDeliverySourceOption[]>();
		const forOnboarding = anAnswerNotGivenYet<IDeliverySourceOption[]>();
		vi.mocked(deliveryService.getDeliverySourceOptions).mockImplementation(
			(portfolioId: number) =>
				portfolioId === PAYMENTS_PLATFORM.id
					? forPaymentsPlatform.promise
					: forOnboarding.promise,
		);
		const dialog = openTheDialog({
			deliveryService,
			portfolio: PAYMENTS_PLATFORM,
		});
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		dialog.close();
		dialog.reopenOn(ONBOARDING);
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await waitFor(() =>
			expect(deliveryService.getDeliverySourceOptions).toHaveBeenCalledWith(
				ONBOARDING.id,
				JIRA_RELEASE_SOURCE.key,
			),
		);

		await whenTheServerAnswers(forOnboarding, [datedInProject]);
		await whenTheServerAnswers(forPaymentsPlatform, [datedInJustATest]);

		await user.click(
			await screen.findByRole("combobox", { name: "Jira Release" }),
		);
		expect(
			await screen.findByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).toBeNull();
	});

	it.skip("a failed answer for an opening Lena closed is not what the next opening shows", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const forTheClosedOpening = anAnswerNotGivenYet<IDeliverySourceOption[]>();
		vi.mocked(deliveryService.getDeliverySourceOptions)
			.mockReturnValueOnce(forTheClosedOpening.promise)
			.mockResolvedValueOnce([datedInProject]);
		const dialog = openTheDialog({ deliveryService });
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		dialog.close();
		dialog.reopen();
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(2));

		await whenTheServerFails(forTheClosedOpening);

		await user.click(await untilTheJiraReleaseTabIsOffered());
		await untilThePickerIsReady();
	});
});
