import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import {
	aConnectionOffering,
	aFeatureServiceListingNothing,
	aPortfolioHolding,
	openTheDialog,
} from "./DeliveryDialogTestKit";

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: {
			hasLicense: false,
			isValid: false,
			canUsePremiumFeatures: false,
		},
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

// 22:30 in New York on 10 March 2027, when the UTC calendar has already moved on to the 11th.
const LATE_EVENING_IN_NEW_YORK = "2027-03-11T03:30:00Z";
const TODAY_IN_NEW_YORK = "2027-03-10";
const TOMORROW_IN_NEW_YORK = "2027-03-11";

const MUST_BE_IN_THE_FUTURE = "Launch date must be in the future";

const aDeliverableToTick = (): IFeature =>
	({
		id: 1,
		name: "Checkout",
		referenceId: "PAY-1",
		stateCategory: "ToDo",
	}) as unknown as IFeature;

const openWithANameAndADeliverableTicked = async () => {
	const featureService = aFeatureServiceListingNothing();
	const deliverable = aDeliverableToTick();
	vi.mocked(featureService.getFeaturesByIds).mockResolvedValue([deliverable]);
	openTheDialog({
		deliveryService: aConnectionOffering([]),
		featureService,
		portfolio: aPortfolioHolding(1, "Payments Platform", [deliverable]),
	});

	const user = userEvent.setup();
	await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
	await waitFor(() => {
		expect(screen.getByText("Checkout")).toBeInTheDocument();
	});
	await user.click(screen.getAllByRole("checkbox")[0]);

	return user;
};

const enterTheDate = async (
	user: ReturnType<typeof userEvent.setup>,
	day: string,
) => {
	const dateField = screen.getByLabelText("Launch Date");
	await user.clear(dateField);
	await user.type(dateField, day);
};

describe("the Add Launch dialog on a browser west of UTC, late in the evening", () => {
	beforeEach(() => {
		vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ["Date"] });
		vi.setSystemTime(new Date(LATE_EVENING_IN_NEW_YORK));
	});
	afterEach(() => {
		vi.useRealTimers();
	});

	it("runs on a clock behind UTC, or this file proves nothing", () => {
		expect(
			new Date().getTimezoneOffset(),
			"these checks only mean something west of UTC, where the offset is positive; fix the west-of-UTC project in vitest.config.ts so this file runs with a New York clock",
		).toBeGreaterThan(0);
	});

	it("lets Save through for tomorrow, with no date message", async () => {
		const user = await openWithANameAndADeliverableTicked();

		await enterTheDate(user, TOMORROW_IN_NEW_YORK);

		expect(
			screen.queryByText(MUST_BE_IN_THE_FUTURE),
			"tomorrow on the reader's calendar must not be called a date in the past; read the day as a local calendar day, not as UTC midnight",
		).not.toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Save" }),
			"Save must be open for tomorrow once the name and a Deliverable are filled in",
		).toBeEnabled();
	});

	it('says "Launch date must be in the future" and blocks Save for today', async () => {
		const user = await openWithANameAndADeliverableTicked();

		await enterTheDate(user, TODAY_IN_NEW_YORK);

		expect(
			screen.getByText(MUST_BE_IN_THE_FUTURE),
			"today must still be refused with the future-date message",
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Save" }),
			"Save must stay blocked for today",
		).toBeDisabled();
	});
});
