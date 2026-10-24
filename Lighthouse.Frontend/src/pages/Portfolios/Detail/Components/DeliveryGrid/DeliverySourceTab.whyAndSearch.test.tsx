import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import {
	RenamedTerms,
	renderWithTerms,
} from "../../../../../tests/FieldTipsTestKit";
import {
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";
import {
	allOptions,
	DeliverySourceTabAsHosted,
	datelessOption,
	JIRA_RELEASE_SOURCE,
	mockPortfolio,
	openSourceList,
	retiredOption,
	THE_DATELESS_RELEASE,
} from "./DeliverySourceTabFixture";

/**
 * Dana, delivery-forecaster, adds a Delivery that follows a Jira Release, on a Portfolio whose connection
 * holds hundreds of them. The empty picker invites typing ("Type to search Jira Releases"), and a Release
 * without a date says what to do: "No date in Jira: set one there to follow it". A source the picker cannot
 * name a tracker for speaks of the tenant's own word for the work tracking system. A dateless Release still
 * cannot be picked; a Release gone from the board still reads "No longer available".
 */

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		},
		canUpdateAllTeamsAndPortfolios: true,
	}),
}));

const THE_SEARCH_INVITATION = "Type to search Jira Releases";
// The whole row, anchored at both ends, so a second caption beside the advice or a clause after it fails.
const THE_DATELESS_ROW_IN_JIRA =
	/^Release 45 \(Project X\)\s*No date in Jira: set one there to follow it$/;
const THE_DATELESS_ROW_IN_TRACKER =
	/^Release 45 \(Project X\)\s*No date in Tracker: set one there to follow it$/;

const aBoardListing = (options: IDeliverySourceOption[]) => {
	const deliveryService = createMockDeliveryService();
	deliveryService.getDeliverySources = vi
		.fn()
		.mockResolvedValue([JIRA_RELEASE_SOURCE]);
	deliveryService.getDeliverySourceOptions = vi.fn().mockResolvedValue(options);
	return deliveryService;
};

/** The source tab as the create form hosts it, under a tenant that renamed its work tracking system "Tracker". */
const givenDanaOpensTheSourceTab = async (
	options: IDeliverySourceOption[],
	source = JIRA_RELEASE_SOURCE,
) => {
	const onOptionPicked = vi.fn();
	const deliveryService = aBoardListing(options);
	await renderWithTerms(
		<DeliverySourceTabAsHosted
			portfolioId={1}
			sourceKey={source.key}
			sourceName={source.displayName}
			featuresTerm="Bets"
			portfolioTerm="Program"
			currentSelection={null}
			onOptionPicked={onOptionPicked}
			publishForecast={false}
			onPublishForecastChange={vi.fn()}
		/>,
		RenamedTerms,
		{ deliveryService },
	);
	return { onOptionPicked, deliveryService };
};

const thePicker = (name = JIRA_RELEASE_SOURCE.displayName) =>
	screen.findByRole("combobox", { name });

beforeEach(() => {
	vi.clearAllMocks();
});

describe("Story 6355: the Release picker says why and invites searching", () => {
	// @driving_port @contract-shape:pure-function
	it("on Add Delivery's Jira Release tab, the empty picker reads Type to search Jira Releases", async () => {
		const user = userEvent.setup();
		await renderWithTerms(
			<DeliveryCreateModal
				open={true}
				portfolio={mockPortfolio}
				onClose={vi.fn()}
				onSave={vi.fn()}
			/>,
			RenamedTerms,
			{
				deliveryService: aBoardListing(allOptions),
				featureService: createMockFeatureService(),
			},
		);

		await user.click(
			await screen.findByRole("button", {
				name: JIRA_RELEASE_SOURCE.displayName,
			}),
		);

		expect(await thePicker()).toHaveAttribute(
			"placeholder",
			THE_SEARCH_INVITATION,
		);
	});

	// @contract-shape:pure-function
	it("a Release without a date says it has no date in Jira and to set one there", async () => {
		const user = userEvent.setup();
		await givenDanaOpensTheSourceTab(allOptions);

		await openSourceList(user);

		const dateless = screen.getByRole("option", { name: THE_DATELESS_RELEASE });
		expect(dateless).toHaveTextContent(THE_DATELESS_ROW_IN_JIRA);
	});

	// @edge @contract-shape:pure-function
	it("a source the picker cannot name a tracker for speaks of the tenant's own word for it", async () => {
		const user = userEvent.setup();
		await givenDanaOpensTheSourceTab([datelessOption], {
			key: "some-other-release",
			displayName: "Milestone",
		});

		const picker = await thePicker("Milestone");
		expect(picker).toHaveAttribute("placeholder", "Type to search Milestones");
		await user.click(picker);

		expect(
			screen.getByRole("option", { name: THE_DATELESS_RELEASE }),
		).toHaveTextContent(THE_DATELESS_ROW_IN_TRACKER);
	});
});

describe("Story 6355: what stays as it is", () => {
	// @error @active-guard @contract-shape:unbounded-preservation
	it("a Release without a date still cannot be picked", async () => {
		const user = userEvent.setup();
		const { onOptionPicked, deliveryService } =
			await givenDanaOpensTheSourceTab([datelessOption]);

		await openSourceList(user);
		const dateless = screen.getByRole("option", { name: THE_DATELESS_RELEASE });
		fireEvent.click(dateless);

		expect(dateless).toHaveAttribute("aria-disabled", "true");
		expect(onOptionPicked).not.toHaveBeenCalled();
		expect(deliveryService.previewDeliverySource).not.toHaveBeenCalled();
		expect(await thePicker()).toHaveValue("");
	});

	// @error @active-guard @contract-shape:unbounded-preservation
	it("a Release gone from the board still reads No longer available, never the missing-date advice", async () => {
		const user = userEvent.setup();
		await givenDanaOpensTheSourceTab([retiredOption]);

		await openSourceList(user);

		const gone = screen.getByRole("option", { name: /^Release 40 \(/ });
		expect(gone).toHaveTextContent("No longer available");
		expect(gone).not.toHaveTextContent(/No date in/);
		expect(gone).toHaveAttribute("aria-disabled", "true");
	});
});
