import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import type { ILicenseStatus } from "../../../../../models/ILicenseStatus";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import type { Team } from "../../../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { IApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockLicensingService,
	createMockPortfolioService,
	createMockTeamService,
} from "../../../../../tests/MockApiServiceProvider";
import {
	aConnectionOfferingJiraReleases,
	anAnswerNotGivenYet,
	COPY,
	openTheDialog,
	PREMIUM,
	SPINNER_ROLE,
	theManualTab,
	thePremiumNotice,
	theRuleBasedTab,
	theSourceTabBodyStatus,
	untilTheJiraReleaseTabIsOffered,
} from "./DeliveryDialogTestKit";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";

/**
 * The licence as the app really reads it: three requests at once, for the licence, the teams and the
 * portfolios. Until a later change gives that check a failure look of its own, a check that never
 * answers, or fails on any of the three, leaves the premium tabs loading. That is the accepted limit;
 * what it must never do is tell Lena she needs a premium licence she may well have.
 */

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
	vi.spyOn(console, "error").mockImplementation(() => undefined);
	clearDeliverySourceOptionsCache();
});

type Failing = "nothing" | "licence" | "teams" | "portfolio";

/** The three answers the licence check waits for, with the one named failing or all of them silent. */
const aLicenceCheckWhere = (failing: Failing): Partial<IApiServiceContext> => {
	const licensingService = createMockLicensingService();
	const teamService = createMockTeamService();
	const portfolioService = createMockPortfolioService();

	if (failing === "nothing") {
		vi.mocked(licensingService.getLicenseStatus).mockReturnValue(
			anAnswerNotGivenYet<ILicenseStatus>().promise,
		);
		vi.mocked(teamService.getTeams).mockReturnValue(
			anAnswerNotGivenYet<Team[]>().promise,
		);
		vi.mocked(portfolioService.getPortfolios).mockReturnValue(
			anAnswerNotGivenYet<Portfolio[]>().promise,
		);
		return { licensingService, teamService, portfolioService };
	}

	const refused = new Error("The server could not answer");
	vi.mocked(licensingService.getLicenseStatus).mockImplementation(() =>
		failing === "licence" ? Promise.reject(refused) : Promise.resolve(PREMIUM),
	);
	vi.mocked(teamService.getTeams).mockImplementation(() =>
		failing === "teams" ? Promise.reject(refused) : Promise.resolve([]),
	);
	vi.mocked(portfolioService.getPortfolios).mockImplementation(() =>
		failing === "portfolio" ? Promise.reject(refused) : Promise.resolve([]),
	);
	return { licensingService, teamService, portfolioService };
};

describe("a licence check that never gives its answer", () => {
	it.each<[string, Failing]>([
		["never answers", "nothing"],
		["fails on the licence", "licence"],
		["fails on the teams", "teams"],
		["fails on the portfolios", "portfolio"],
	])(
		"when it %s, the premium tabs stay loading, no premium notice is shown and Manual stays usable",
		async (_, failing) => {
			const user = userEvent.setup();
			const services = aLicenceCheckWhere(failing);
			openTheDialog({
				deliveryService: aConnectionOfferingJiraReleases(),
				otherServices: services,
			});

			await user.click(await untilTheJiraReleaseTabIsOffered());
			await waitFor(() =>
				expect(services.licensingService?.getLicenseStatus).toHaveBeenCalled(),
			);
			await new Promise((settle) => setTimeout(settle, 20));

			expect(theSourceTabBodyStatus()).toBe("loading");
			expect(thePremiumNotice()).toBeNull();
			expect(theRuleBasedTab()).toBeEnabled();
			await user.click(theRuleBasedTab() as HTMLElement);
			expect(screen.getByRole(SPINNER_ROLE)).toBeInTheDocument();
			expect(thePremiumNotice()).toBeNull();

			await user.click(theManualTab() as HTMLElement);
			expect(screen.getByText(COPY.selectDeliverables)).toBeInTheDocument();
		},
	);
});
