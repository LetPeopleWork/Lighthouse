import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import type { IOptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import type { ITerminologyService } from "../../../services/Api/TerminologyService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
	createMockEncryptionService,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockSettingsService,
	createMockSystemInfoService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import SystemSettingsTab from "./SystemSettingsTab";

/**
 * DISTILL acceptance scenarios (Story 5876) - the rendered half. US-01 AC-01.1, AC-01.2 and AC-01.11
 * are about what an administrator sees on Settings -> System, and US-02 AC-02.4 is about what they
 * cannot reach from there. The served half lives in the backend BehaviourSettings acceptance suite.
 */

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();
const mockOptionalFeatureService: IOptionalFeatureService =
	createMockOptionalFeatureService();
mockOptionalFeatureService.getAllFeatures = mockGetAllFeatures;
mockOptionalFeatureService.updateFeature = mockUpdateFeature;

const mockGetAllTerminology = vi.fn();
const mockTerminologyService: ITerminologyService =
	createMockTerminologyService();
mockTerminologyService.getAllTerminology = mockGetAllTerminology;

const mockGetLicenseStatus = vi.fn();
const mockLicensingService: ILicensingService = createMockLicensingService();
mockLicensingService.getLicenseStatus = mockGetLicenseStatus;

const mockBlackoutPeriodService = createMockBlackoutPeriodService();
const mockGetAllBlackoutPeriods = vi.fn();
mockBlackoutPeriodService.getAll = mockGetAllBlackoutPeriods;

/**
 * What the seeder stores. The word for a Feature is a token rather than a word, because the instance
 * decides that word and only the browser knows what it decided.
 */
const theOrderingSettingAsSeeded = {
	id: 2,
	key: "FeatureOrdering",
	name: "Let Lighthouse own the order of your {{features}}",
	description:
		"Turn this on to arrange your {{features}} yourself. Turning it off hands the order back to your work tracking system and keeps the places you chose.",
	enabled: false,
	isPremium: true,
	isPreview: false,
};

const theShippedNonPremiumSetting = {
	id: 1,
	key: "DeltaSync",
	name: "Faster Updates",
	description:
		"Fetch only the work items that changed since the last update instead of the whole query.",
	enabled: false,
	isPremium: false,
	isPreview: true,
};

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		settingsService: createMockSettingsService(),
		optionalFeatureService: mockOptionalFeatureService,
		terminologyService: mockTerminologyService,
		licensingService: mockLicensingService,
		blackoutPeriodService: mockBlackoutPeriodService,
		encryptionService: createMockEncryptionService(),
		systemInfoService: createMockSystemInfoService(),
	});

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockContext}>
				<TerminologyProvider>{children}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
};

const renderTheSystemSettings = () => {
	render(
		<MockApiServiceProvider>
			<SystemSettingsTab />
		</MockApiServiceProvider>,
	);
};

const givenTheInstanceCallsFeatures = (word: string, plural: string) => {
	mockGetAllTerminology.mockResolvedValue([
		{
			id: 1,
			key: "feature",
			defaultValue: "Feature",
			description: "Term used for a single feature",
			value: word,
		},
		{
			id: 2,
			key: "features",
			defaultValue: "Features",
			description: "Term used for multiple features",
			value: plural,
		},
	]);
};

const givenTheInstanceHasNoPremiumLicence = () => {
	mockGetLicenseStatus.mockResolvedValue({
		hasLicense: true,
		isValid: true,
		canUsePremiumFeatures: false,
	});
};

describe("Behaviour Settings", () => {
	beforeEach(() => {
		vi.resetAllMocks();

		mockGetAllBlackoutPeriods.mockResolvedValue([]);
		mockGetAllFeatures.mockResolvedValue([
			theShippedNonPremiumSetting,
			theOrderingSettingAsSeeded,
		]);
		mockGetLicenseStatus.mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});
		givenTheInstanceCallsFeatures("Feature", "Features");
	});

	// @AC-01.1 - which switches live where is a fact about release history until this ships. One
	// heading, one table, both rows under it.
	it("puts every instance-wide switch under one heading", async () => {
		renderTheSystemSettings();

		await waitFor(() => {
			expect(screen.getByText("Behaviour Settings")).toBeVisible();
		});

		expect(screen.getByTestId("feature-row-DeltaSync")).toBeVisible();
		expect(screen.getByTestId("feature-row-FeatureOrdering")).toBeVisible();
	});

	// @AC-01.1 - and the two places it used to live are gone. Without this the move is an addition and
	// the page has three answers to the same question instead of one.
	it("leaves no separate section behind", async () => {
		renderTheSystemSettings();

		await waitFor(() => {
			expect(screen.getByText("Behaviour Settings")).toBeVisible();
		});

		expect(screen.queryByText("Optional Features")).not.toBeInTheDocument();
		expect(screen.queryByText("Feature Order")).not.toBeInTheDocument();
		// The testids the standalone section actually renders. Naming one it does not renders the guard
		// unfailable, which is how a removal check quietly stops checking removal.
		expect(
			screen.queryByTestId("feature-ordering-toggle"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("feature-ordering-help-text"),
		).not.toBeInTheDocument();
	});

	// @AC-01.2 - the premium affordance the table already knows how to render, on the row that is now
	// the first premium one it has ever held.
	it("shows the ordering switch as unavailable on an instance without the licence", async () => {
		givenTheInstanceHasNoPremiumLicence();

		renderTheSystemSettings();

		await waitFor(() => {
			expect(
				screen.getByTestId("FeatureOrdering-toggle").querySelector("input"),
			).toBeDisabled();
		});
	});

	// @AC-01.11 - the criterion this store was once rejected on. The seeded string names a token; the
	// cell renders the instance's own word.
	it.skip("reads the row in the instance's own word for a Feature", async () => {
		givenTheInstanceCallsFeatures("Deliverable", "Deliverables");

		renderTheSystemSettings();

		await waitFor(() => {
			expect(
				screen.getByText(/Let Lighthouse own the order of your Deliverables/),
			).toBeVisible();
		});

		expect(
			screen.getByText(/arrange your Deliverables yourself/),
		).toBeVisible();
		expect(screen.queryByText(/\{\{features\}\}/)).not.toBeInTheDocument();
	});

	// @AC-01.11 - a token nobody defined is left standing, braces and all. Resolving it to the bare key
	// would read as ordinary prose and ship a typo nobody notices; dropping it would delete a word from
	// a sentence and read as a bug in the copy.
	it("leaves a token nobody defined exactly where it is", async () => {
		mockGetAllFeatures.mockResolvedValue([
			{
				...theOrderingSettingAsSeeded,
				description: "Arrange your {{fetaures}} yourself.",
			},
		]);

		renderTheSystemSettings();

		await waitFor(() => {
			expect(
				screen.getByText(/Arrange your \{\{fetaures\}\} yourself\./),
			).toBeVisible();
		});
	});

	// @AC-01.1 - the two rows are switched one at a time. The mock gives both the identity the seeder
	// really writes: the store keys these rows by their key, nothing generates the number, so every
	// seeded row carries zero. A table that matches its optimistic update on that number moves every
	// switch on the page at once, and no amount of fixing the server changes that.
	it("switches one setting without touching the other", async () => {
		mockGetAllFeatures.mockResolvedValue([
			{ ...theShippedNonPremiumSetting, id: 0 },
			{ ...theOrderingSettingAsSeeded, id: 0 },
		]);

		renderTheSystemSettings();

		const fasterUpdates = await waitFor(
			() =>
				screen
					.getByTestId("DeltaSync-toggle")
					.querySelector("input") as HTMLInputElement,
		);

		await userEvent.click(fasterUpdates);

		expect(
			screen.getByTestId("FeatureOrdering-toggle").querySelector("input"),
		).not.toBeChecked();
	});

	// @AC-02.4 - when the write is refused, the switch goes back to what is actually in force. Slice 01
	// turns this endpoint's refusal from a 200 carrying the old value into a 403, so the rollback stops
	// being decorative and starts being the only thing between an administrator and a switch that shows
	// a setting they do not have.
	it("puts the switch back when the write is refused", async () => {
		mockGetAllFeatures.mockResolvedValue([theShippedNonPremiumSetting]);
		mockUpdateFeature.mockRejectedValue(new Error("refused"));

		renderTheSystemSettings();

		const fasterUpdates = await waitFor(
			() =>
				screen
					.getByTestId("DeltaSync-toggle")
					.querySelector("input") as HTMLInputElement,
		);

		await userEvent.click(fasterUpdates);

		await waitFor(() => {
			expect(
				screen.getByTestId("DeltaSync-toggle").querySelector("input"),
			).not.toBeChecked();
		});
	});

	// @AC-01.9 - Faster Updates keeps its name, its help text and its preview badge. It is not premium
	// and the only thing this story changes for it is the heading above the table.
	it("changes nothing about the setting that was already in the list", async () => {
		renderTheSystemSettings();

		await waitFor(() => {
			expect(screen.getByText("Faster Updates")).toBeVisible();
		});

		expect(screen.getByTestId("DeltaSync-preview-indicator")).toBeVisible();
		expect(
			screen.getByTestId("DeltaSync-toggle").querySelector("input"),
		).not.toBeDisabled();
	});

	// @AC-02.4 - the refusal slice 01 ships is reachable only by calling the API directly. Green
	// already: the table has disabled premium switches since the affordance was built. It is asserted
	// here so the move cannot quietly start producing errors in front of a user.
	it("never lets an unlicensed administrator reach the refusal", async () => {
		givenTheInstanceHasNoPremiumLicence();
		mockGetAllFeatures.mockResolvedValue([theOrderingSettingAsSeeded]);

		renderTheSystemSettings();

		const toggle = await waitFor(() => {
			const input = screen
				.getByTestId("FeatureOrdering-toggle")
				.querySelector("input");
			expect(input).toBeDisabled();
			return input as HTMLInputElement;
		});

		// userEvent, not fireEvent: fireEvent dispatches straight at the element and a disabled control
		// answers it, so the assertion below would pass on a control a real administrator can operate.
		await userEvent.click(toggle, { pointerEventsCheck: 0 });

		expect(mockUpdateFeature).not.toHaveBeenCalled();
	});
});
