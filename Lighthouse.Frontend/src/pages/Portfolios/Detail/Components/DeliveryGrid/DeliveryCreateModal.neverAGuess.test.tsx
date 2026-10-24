import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IDeliverySource,
	IDeliverySourceOption,
	IDeliverySourcePreview,
} from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { ApiError } from "../../../../../services/Api/ApiError";
import {
	AMBER_NOTICE,
	aConnectionOfferingJiraReleases,
	aLaunchFollowingAJiraRelease,
	anAnswerNotGivenYet,
	COMMUNITY,
	COPY,
	openTheDialog,
	PREMIUM,
	releaseListsAskedFor,
	SPINNER_ROLE,
	sourcesAskedFor,
	theBusyTab,
	theJiraReleaseTab,
	theManualFeatureList,
	theManualTab,
	theNoticeSaying,
	thePremiumNotice,
	theReleasePicker,
	theRuleBasedTab,
	theSourceTabBody,
	theSourceTabBodyStatus,
	untilTheJiraReleaseTabIsOffered,
	untilThePickerIsReady,
	whenTheServerAnswers,
	whenTheServerFails,
} from "./DeliveryDialogTestKit";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	datedInProject,
	JIRA_RELEASE_SOURCE,
	RELEASE_44_IN_PROJECT_X,
} from "./DeliverySourceTabFixture";

/**
 * While the dialog does not know yet, it says it is loading; when it could not find out, it says so
 * and how to retry. Never "no Jira Release here", never "premium needed" on a
 * licence nobody has read yet.
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

const withSourcesStillOnTheirWay = () => {
	const deliveryService = aConnectionOfferingJiraReleases();
	const sources = anAnswerNotGivenYet<IDeliverySource[]>();
	vi.mocked(deliveryService.getDeliverySources).mockReturnValue(
		sources.promise,
	);
	return { deliveryService, sources };
};

const withTheReleaseListStillOnItsWay = () => {
	const deliveryService = aConnectionOfferingJiraReleases();
	const releases = anAnswerNotGivenYet<IDeliverySourceOption[]>();
	vi.mocked(deliveryService.getDeliverySourceOptions).mockReturnValue(
		releases.promise,
	);
	return { deliveryService, releases };
};

describe("Lena opens Add Launch while the ways to pick are still loading", () => {
	it("shows a busy tab in place of the ones still loading, and the Jira Release tab once they answer", async () => {
		const { deliveryService, sources } = withSourcesStillOnTheirWay();

		openTheDialog({ deliveryService });

		const busyTab = await waitFor(() => {
			const tab = theBusyTab();
			expect(tab).not.toBeNull();
			return tab as HTMLElement;
		});
		expect(busyTab).toBeDisabled();
		expect(busyTab).toHaveAttribute("aria-busy", "true");
		expect(theManualTab()).toBeEnabled();
		expect(theRuleBasedTab()).toBeEnabled();
		expect(theJiraReleaseTab()).toBeNull();
		expect(thePremiumNotice()).toBeNull();

		await whenTheServerAnswers(sources, [JIRA_RELEASE_SOURCE]);

		await untilTheJiraReleaseTabIsOffered();
		expect(theBusyTab()).toBeNull();
	});
});

describe("Lena opens a premium tab before the licence has answered", () => {
	beforeEach(() => theLicenceIs(null));

	it("the Jira Release tab says it is getting the Releases, never that they are premium", async () => {
		const user = userEvent.setup();
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });

		await user.click(await untilTheJiraReleaseTabIsOffered());

		await waitFor(() => {
			expect(theSourceTabBodyStatus()).toBe("loading");
		});
		const body = theSourceTabBody() as HTMLElement;
		expect(within(body).getByRole(SPINNER_ROLE)).toBeInTheDocument();
		expect(within(body).getByText(COPY.gettingTheReleases)).toBeInTheDocument();
		expect(thePremiumNotice()).toBeNull();
		expect(screen.queryByText(COPY.jiraReleaseIsPremium)).toBeNull();
	});

	it("Rule-Based stays open to her and shows a loading look, with no lock and no premium tooltip", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		openTheDialog({ deliveryService });

		const ruleBased = theRuleBasedTab() as HTMLElement;
		expect(ruleBased).toBeEnabled();
		await user.hover(ruleBased);
		await expect(
			screen.findByRole("tooltip", {}, { timeout: 300 }),
		).rejects.toThrow();

		await user.click(ruleBased);

		expect(screen.getByRole(SPINNER_ROLE)).toBeInTheDocument();
		expect(thePremiumNotice()).toBeNull();
		expect(deliveryService.getRuleSchema).not.toHaveBeenCalled();
	});

	it("Save stays off while the Rule-Based body is still loading", async () => {
		const user = userEvent.setup();
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });
		const ruleBased = theRuleBasedTab() as HTMLElement;
		expect(ruleBased).toBeEnabled();

		await user.click(ruleBased);

		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
	});

	it("once the licence answers not premium, the tab she chose stays chosen and shows the premium notice, and Rule-Based locks", async () => {
		const user = userEvent.setup();
		const dialog = openTheDialog({
			deliveryService: aConnectionOfferingJiraReleases(),
		});
		const jiraReleaseTab = await untilTheJiraReleaseTabIsOffered();
		await user.click(jiraReleaseTab);
		expect(thePremiumNotice()).toBeNull();

		theLicenceIs(COMMUNITY);
		dialog.redraw();

		await waitFor(() => {
			expect(thePremiumNotice()).toHaveTextContent(COPY.jiraReleaseIsPremium);
		});
		expect(theJiraReleaseTab()).toHaveAttribute("aria-pressed", "true");
		expect(theRuleBasedTab()).toBeDisabled();
	});

	it("a licence check that never answers keeps the loading look, and the Manual tab stays open to her", async () => {
		const user = userEvent.setup();
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });
		const jiraReleaseTab = await untilTheJiraReleaseTabIsOffered();

		await user.click(jiraReleaseTab);
		expect(theSourceTabBodyStatus()).toBe("loading");
		expect(thePremiumNotice()).toBeNull();

		await user.click(theManualTab() as HTMLElement);
		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");

		expect(screen.getByText(COPY.selectDeliverables)).toBeInTheDocument();
		expect(screen.getByLabelText("Launch Name")).toHaveValue("Autumn launch");
		expect(screen.getByLabelText("Launch Date")).toBeEnabled();
	});
});

describe("once the licence is known", () => {
	it("a Community licence shows today's premium notice, lock and tooltip", async () => {
		theLicenceIs(COMMUNITY);
		const user = userEvent.setup();
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });

		await user.click(await untilTheJiraReleaseTabIsOffered());

		expect(thePremiumNotice()).toHaveTextContent(COPY.jiraReleaseIsPremium);
		const ruleBased = theRuleBasedTab() as HTMLElement;
		expect(ruleBased).toBeDisabled();
		await user.hover(ruleBased.parentElement as HTMLElement);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			COPY.ruleBasedIsPremium,
		);
	});

	it("a premium licence shows the Release picker, ready", async () => {
		const user = userEvent.setup();
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });

		await user.click(await untilTheJiraReleaseTabIsOffered());

		await untilThePickerIsReady();
		expect(thePremiumNotice()).toBeNull();
	});
});

describe("when the ways to pick could not be checked", () => {
	it("the dialog says so in an amber notice (the warning colour, which only its class carries) with no button, and Manual and Rule-Based still work", async () => {
		const { deliveryService, sources } = withSourcesStillOnTheirWay();
		openTheDialog({ deliveryService });

		await whenTheServerFails(sources);

		const notice = await waitFor(() =>
			theNoticeSaying(COPY.sourcesCouldNotBeChecked),
		);
		expect(notice).toHaveClass(AMBER_NOTICE);
		expect(within(notice).queryByRole("button")).toBeNull();
		expect(theJiraReleaseTab()).toBeNull();
		expect(theBusyTab()).toBeNull();
		expect(theManualTab()).toBeEnabled();
		expect(theRuleBasedTab()).toBeEnabled();
	});

	it("reopening the dialog asks again, and the Jira Release tab appears once they answer", async () => {
		const { deliveryService, sources } = withSourcesStillOnTheirWay();
		const dialog = openTheDialog({ deliveryService });
		await whenTheServerFails(sources);
		dialog.close();
		vi.mocked(deliveryService.getDeliverySources).mockResolvedValue([
			JIRA_RELEASE_SOURCE,
		]);

		dialog.reopen();

		await untilTheJiraReleaseTabIsOffered();
		expect(sourcesAskedFor(deliveryService)).toBe(2);
	});
});

describe("when the Release list could not be loaded", () => {
	it.skip("the tab says so and tells her to reopen the dialog, with no button", async () => {
		const user = userEvent.setup();
		const { deliveryService, releases } = withTheReleaseListStillOnItsWay();
		openTheDialog({ deliveryService });
		await user.click(await untilTheJiraReleaseTabIsOffered());

		await whenTheServerFails(releases);

		await waitFor(() => {
			expect(theSourceTabBodyStatus()).toBe("error");
		});
		const notice = theNoticeSaying(COPY.releasesCouldNotBeLoaded);
		expect(within(notice).queryByRole("button")).toBeNull();
		expect(theReleasePicker()).toBeNull();
	});

	it.skip("a list the server refuses ends in the same look", async () => {
		const user = userEvent.setup();
		const { deliveryService, releases } = withTheReleaseListStillOnItsWay();
		openTheDialog({ deliveryService });
		await user.click(await untilTheJiraReleaseTabIsOffered());

		await whenTheServerFails(releases, new ApiError(403, "Forbidden"));

		await waitFor(() => {
			expect(theSourceTabBodyStatus()).toBe("error");
		});
		theNoticeSaying(COPY.releasesCouldNotBeLoaded);
	});

	it("reopening the dialog asks for the list again and shows it", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		vi.mocked(deliveryService.getDeliverySourceOptions).mockRejectedValueOnce(
			new Error("Jira is down"),
		);
		const dialog = openTheDialog({ deliveryService });
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await waitFor(() => expect(releaseListsAskedFor(deliveryService)).toBe(1));
		dialog.close();

		dialog.reopen();
		await user.click(await untilTheJiraReleaseTabIsOffered());

		await waitFor(() => expect(theReleasePicker()).not.toBeNull());
		expect(releaseListsAskedFor(deliveryService)).toBe(2);
	});
});

describe("Lena edits PAY 2026.10, which follows the Jira Release of that name", () => {
	it("while the ways to pick load, the dialog shows a loading look, never the Manual tab's list, and Update stays off", async () => {
		const { deliveryService } = withSourcesStillOnTheirWay();

		openTheDialog({
			deliveryService,
			editingDelivery: aLaunchFollowingAJiraRelease(),
		});

		await waitFor(() => expect(theBusyTab()).not.toBeNull());
		expect(screen.getAllByRole(SPINNER_ROLE).length).toBeGreaterThan(0);
		expect(theManualFeatureList()).toBeNull();
		expect(screen.queryByText(COPY.selectDeliverables)).toBeNull();
		expect(screen.getByRole("button", { name: "Update" })).toBeDisabled();
	});

	it("when the ways to pick could not be checked, the dialog shows the notice alone and Update stays off", async () => {
		const { deliveryService, sources } = withSourcesStillOnTheirWay();
		openTheDialog({
			deliveryService,
			editingDelivery: aLaunchFollowingAJiraRelease(),
		});

		await whenTheServerFails(sources);

		await waitFor(() => theNoticeSaying(COPY.sourcesCouldNotBeChecked));
		expect(theManualFeatureList()).toBeNull();
		expect(screen.queryByText(COPY.selectDeliverables)).toBeNull();
		expect(screen.getByRole("button", { name: "Update" })).toBeDisabled();
	});
});

describe("nothing under the form reads as the dialog's answer while it does not know yet", () => {
	it("while the ways to pick are still loading, no 'name is required' stands under the form, and Save stays off", async () => {
		const { deliveryService } = withSourcesStillOnTheirWay();
		openTheDialog({ deliveryService });

		await waitFor(() => expect(sourcesAskedFor(deliveryService)).toBe(1));

		expect(screen.queryByText(COPY.nameIsRequired)).toBeNull();
		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
	});

	it("while the licence has not answered, no 'name is required' stands under the form, and Save stays off", async () => {
		theLicenceIs(null);
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });

		await untilTheJiraReleaseTabIsOffered();

		expect(screen.queryByText(COPY.nameIsRequired)).toBeNull();
		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
	});

	it("once both have answered, the message stands under the form as it does today", async () => {
		openTheDialog({ deliveryService: aConnectionOfferingJiraReleases() });

		await untilTheJiraReleaseTabIsOffered();

		expect(await screen.findByText(COPY.nameIsRequired)).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
	});
});

describe("Lena picks a Release", () => {
	it.skip("a small spinner shows under the picker while the preview is on its way, and goes once it arrives", async () => {
		const user = userEvent.setup();
		const deliveryService = aConnectionOfferingJiraReleases();
		const preview = anAnswerNotGivenYet<IDeliverySourcePreview>();
		vi.mocked(deliveryService.previewDeliverySource).mockReturnValue(
			preview.promise,
		);
		openTheDialog({ deliveryService });
		await user.click(await untilTheJiraReleaseTabIsOffered());
		await untilThePickerIsReady();

		await user.click(theReleasePicker() as HTMLElement);
		await user.click(
			await screen.findByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		const body = theSourceTabBody() as HTMLElement;
		expect(within(body).getByRole(SPINNER_ROLE)).toBeInTheDocument();

		await whenTheServerAnswers(preview, {
			name: datedInProject.name,
			date: datedInProject.date as Date,
			features: [],
			emptyBecause: "NothingTaggedAgainstTheSource",
		} as IDeliverySourcePreview);

		await waitFor(() =>
			expect(within(body).queryByRole(SPINNER_ROLE)).toBeNull(),
		);
		expect(screen.getByTestId("delivery-source-preview")).toBeInTheDocument();
	});
});
