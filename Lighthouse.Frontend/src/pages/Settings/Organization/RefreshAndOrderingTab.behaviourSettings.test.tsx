import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockEncryptionService,
	createMockLicensingService,
	createMockOptionalFeatureService,
	createMockSettingsService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import RefreshAndOrderingTab from "./RefreshAndOrderingTab";

/**
 * What an administrator sees on Settings -> Organization -> Refresh & ordering, and what they cannot reach
 * from there. The served half of the same scenarios lives in the backend BehaviourSettings acceptance suite.
 */

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();
const mockGetAllTerminology = vi.fn();
const mockGetLicenseStatus = vi.fn();
const mockGetKeyState = vi.fn();

/**
 * What the seeder stores. The word for a Feature is a token rather than a word, because the instance
 * decides that word and only the browser knows what it decided.
 */
const theOrderingSettingAsSeeded = {
	id: 0,
	key: "FeatureOrdering",
	name: "Let Lighthouse own the order of your {{features}}",
	description:
		"Turn this on to arrange your {{features}} yourself. Turning it off hands the order back to your work tracking system and keeps the places you chose.",
	enabled: false,
	isPremium: true,
	isPreview: false,
};

const renderTheTab = () => {
	const optionalFeatureService = createMockOptionalFeatureService();
	optionalFeatureService.getAllFeatures = mockGetAllFeatures;
	optionalFeatureService.updateFeature = mockUpdateFeature;
	const terminologyService = createMockTerminologyService();
	terminologyService.getAllTerminology = mockGetAllTerminology;
	const licensingService = createMockLicensingService();
	licensingService.getLicenseStatus = mockGetLicenseStatus;
	const encryptionService = createMockEncryptionService();
	encryptionService.getKeyState = mockGetKeyState;

	renderWithQueryClient(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				settingsService: createMockSettingsService(),
				optionalFeatureService,
				terminologyService,
				licensingService,
				encryptionService,
			})}
		>
			<TerminologyProvider>
				<RefreshAndOrderingTab />
			</TerminologyProvider>
		</ApiServiceContext.Provider>,
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

const theOrderingSwitch = () =>
	screen.getByTestId("FeatureOrdering-toggle").querySelector("input");

describe("Behaviour Settings on Refresh & ordering", () => {
	beforeEach(() => {
		vi.resetAllMocks();

		mockGetAllFeatures.mockResolvedValue([theOrderingSettingAsSeeded]);
		mockGetLicenseStatus.mockResolvedValue({
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		});
		givenTheInstanceCallsFeatures("Feature", "Features");
	});

	it("holds the ordering row under one heading and leaves the old separate sections gone", async () => {
		renderTheTab();

		await waitFor(() => {
			expect(screen.getByTestId("feature-row-FeatureOrdering")).toBeVisible();
		});
		expect(screen.getByText("Behaviour Settings")).toBeVisible();
		expect(screen.queryByText("Optional Features")).not.toBeInTheDocument();
		expect(screen.queryByText("Feature Order")).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("feature-ordering-toggle"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("feature-ordering-help-text"),
		).not.toBeInTheDocument();
	});

	it("shows the ordering switch as unavailable on an instance without the licence, still badged Premium", async () => {
		givenTheInstanceHasNoPremiumLicence();

		renderTheTab();

		await waitFor(() => expect(theOrderingSwitch()).toBeDisabled());
		expect(
			screen.getByTestId("FeatureOrdering-premium-indicator"),
		).toHaveTextContent("Premium");
	});

	it("lets a licensed administrator operate the premium switch, badged Premium", async () => {
		renderTheTab();

		await waitFor(() => expect(theOrderingSwitch()).not.toBeDisabled());
		expect(
			screen.getByTestId("FeatureOrdering-premium-indicator"),
		).toBeVisible();
	});

	it("never lets an unlicensed administrator reach the refusal", async () => {
		givenTheInstanceHasNoPremiumLicence();

		renderTheTab();

		const toggle = await waitFor(() => {
			const input = theOrderingSwitch();
			expect(input).toBeDisabled();
			return input as HTMLInputElement;
		});

		// userEvent, not fireEvent: fireEvent dispatches straight at the element and a disabled control
		// answers it, so the assertion below would pass on a control a real administrator can operate.
		await userEvent.click(toggle, { pointerEventsCheck: 0 });

		expect(mockUpdateFeature).not.toHaveBeenCalled();
	});

	it("reads the row in the instance's own word for a Feature", async () => {
		givenTheInstanceCallsFeatures("Deliverable", "Deliverables");

		renderTheTab();

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

	it("leaves a token nobody defined exactly where it is", async () => {
		mockGetAllFeatures.mockResolvedValue([
			{
				...theOrderingSettingAsSeeded,
				description: "Arrange your {{fetaures}} yourself.",
			},
		]);

		renderTheTab();

		await waitFor(() => {
			expect(
				screen.getByText(/Arrange your \{\{fetaures\}\} yourself\./),
			).toBeVisible();
		});
	});

	it("does not repeat the encryption key, which belongs to the Encryption tab", async () => {
		renderTheTab();

		await waitFor(() => {
			expect(screen.getByTestId("feature-row-FeatureOrdering")).toBeVisible();
		});
		expect(
			screen.queryByTestId("encryption-key-state"),
		).not.toBeInTheDocument();
		expect(screen.queryByText(/encryption key/i)).not.toBeInTheDocument();
		expect(mockGetKeyState).not.toHaveBeenCalled();
	});
});
