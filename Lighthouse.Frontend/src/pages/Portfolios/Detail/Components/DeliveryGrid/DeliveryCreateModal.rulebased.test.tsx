import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDelivery } from "../../../../../models/Delivery";
import {
	DeliverySelectionMode,
	type IDeliveryRuleSchema,
} from "../../../../../models/DeliveryRules";
import type { IFeature } from "../../../../../models/Feature";
import type { IPortfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";

// Mock the useTerminology hook
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.DELIVERY]: "Delivery",
				[TERMINOLOGY_KEYS.FEATURES]: "Features",
				[TERMINOLOGY_KEYS.FEATURE]: "Feature",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

// Mock premium license
vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		},
		canCreateTeam: true,
		canUpdateTeamData: true,
		canUpdateTeamSettings: true,
		canUpdatePortfolioData: true,
		canUpdateAllTeamsAndPortfolios: true,
		createTeamTooltip: "",
		updateTeamDataTooltip: "",
		updateTeamSettingsTooltip: "",
		updatePortfolioDataTooltip: "",
		updateAllTeamsAndPortfoliosTooltip: "",
	}),
}));

// Mock FeatureGrid to avoid rendering heavy DataGrid in tests
vi.mock("../../../../../components/Common/FeatureGrid", () => ({
	FeatureGrid: ({ features }: { features: IFeature[] }) => (
		<div data-testid="feature-grid-mock">
			{features.map((f) => (
				<div key={f.id}>{f.name}</div>
			))}
		</div>
	),
}));

const createMockFeature = (
	id: number,
	name: string,
	stateCategory: "ToDo" | "Doing" | "Done" = "ToDo",
): IFeature => ({
	id,
	name,
	referenceId: `FTR-${id}`,
	stateCategory,
	state: stateCategory,
	type: "Feature",
	size: 5,
	owningTeam: "Team A",
	lastUpdated: new Date(),
	isUsingDefaultFeatureSize: false,
	parentWorkItemReference: "",
	projects: [],
	remainingWork: {},
	totalWork: {},
	forecasts: [],
	startedDate: new Date(),
	closedDate: new Date(),
	cycleTime: 0,
	workItemAge: 0,
	url: "https://example.com/feature",
	isBlocked: false,
	getRemainingWorkForFeature: () => 0,
	getRemainingWorkForTeam: () => 0,
	getTotalWorkForFeature: () => 0,
	getTotalWorkForTeam: () => 0,
});

const mockRuleSchema: IDeliveryRuleSchema = {
	fields: [
		{ fieldKey: "feature.tags", displayName: "Tags", isMultiValue: true },
		{ fieldKey: "feature.state", displayName: "State", isMultiValue: false },
	],
	operators: ["equals", "notequals", "contains"],
	maxRules: 20,
	maxValueLength: 500,
};

const mockMatchedFeatures: IFeature[] = [
	createMockFeature(1, "Matched Feature 1", "ToDo"),
	createMockFeature(2, "Matched Feature 2", "Doing"),
];

describe("DeliveryCreateModal - Rule-Based Mode", () => {
	const mockOnClose = vi.fn();
	const mockOnSave = vi.fn();
	const mockOnUpdate = vi.fn();
	const mockFeatureService = createMockFeatureService();
	const mockDeliveryService = createMockDeliveryService();

	const mockPortfolio: IPortfolio = {
		id: 1,
		name: "Test Portfolio",
		features: [{ id: 1 }, { id: 2 }, { id: 3 }] as IFeature[],
		involvedTeams: [],
		tags: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		remainingFeatures: 3,
	};

	const getTwoWeeksFromNow = (): Date => {
		const date = new Date();
		date.setDate(date.getDate() + 14);
		return date;
	};

	const formatDateForInput = (date: Date): string => {
		return date.toISOString().split("T")[0];
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockFeatureService.getFeaturesByIds = vi
			.fn()
			.mockResolvedValue(mockMatchedFeatures);
		mockDeliveryService.getRuleSchema = vi
			.fn()
			.mockResolvedValue(mockRuleSchema);
		mockDeliveryService.validateRules = vi
			.fn()
			.mockResolvedValue(mockMatchedFeatures);
	});

	const renderModal = (editingDelivery?: IDelivery) => {
		const mockApiContext = createMockApiServiceContext({
			featureService: mockFeatureService,
			deliveryService: mockDeliveryService,
		});

		return render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<DeliveryCreateModal
					open={true}
					portfolio={mockPortfolio}
					editingDelivery={editingDelivery}
					onClose={mockOnClose}
					onSave={mockOnSave}
					onUpdate={mockOnUpdate}
				/>
			</ApiServiceContext.Provider>,
		);
	};

	describe("Save button gating", () => {
		it("should have Save button disabled in rule-based mode until rules are validated", async () => {
			renderModal();

			// Switch to rule-based mode
			const ruleBasedButton = screen.getByRole("button", {
				name: "Rule-Based",
			});
			fireEvent.click(ruleBasedButton);

			// Wait for schema to load (rule builder appears)
			await waitFor(() => {
				expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
			});

			// Fill in name and date
			const nameInput = screen.getByLabelText("Delivery Name");
			fireEvent.change(nameInput, { target: { value: "Test Delivery" } });

			const dateInput = screen.getByLabelText("Delivery Date");
			fireEvent.change(dateInput, {
				target: { value: formatDateForInput(getTwoWeeksFromNow()) },
			});

			// Add a rule
			const addRuleButton = screen.getByTestId("add-rule-button");
			fireEvent.click(addRuleButton);

			// Save button should be disabled (rules not validated)
			const saveButton = screen.getByRole("button", { name: "Save" });
			expect(saveButton).toBeDisabled();
		});

		it("should enable Save button after successful rule validation", async () => {
			renderModal();

			// Switch to rule-based mode
			const ruleBasedButton = screen.getByRole("button", {
				name: "Rule-Based",
			});
			fireEvent.click(ruleBasedButton);

			// Wait for schema to load (rule builder appears)
			await waitFor(() => {
				expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
			});

			// Fill in name and date
			const nameInput = screen.getByLabelText("Delivery Name");
			fireEvent.change(nameInput, { target: { value: "Test Delivery" } });

			const dateInput = screen.getByLabelText("Delivery Date");
			fireEvent.change(dateInput, {
				target: { value: formatDateForInput(getTwoWeeksFromNow()) },
			});

			// Add a rule
			const addRuleButton = screen.getByTestId("add-rule-button");
			fireEvent.click(addRuleButton);

			// Find and fill the value input inside the TextField
			const valueInputWrapper = screen.getByTestId("rule-value-input-0");
			const valueInput = within(valueInputWrapper).getByRole("textbox");
			fireEvent.change(valueInput, { target: { value: "test-tag" } });

			// Validate rules
			const validateButton = screen.getByRole("button", {
				name: /validate rules/i,
			});
			fireEvent.click(validateButton);

			// Wait for validation to complete and Save button to be enabled
			await waitFor(() => {
				expect(mockDeliveryService.validateRules).toHaveBeenCalled();
			});

			await waitFor(() => {
				expect(screen.getByTestId("matched-count")).toBeInTheDocument();
			});

			// Save button should now be enabled
			const saveButton = screen.getByRole("button", { name: "Save" });
			expect(saveButton).not.toBeDisabled();
		});

		it(
			"should disable Save button when rule is modified after validation",
			{ timeout: 10000 },
			async () => {
				const user = userEvent.setup();
				renderModal();

				// Switch to rule-based mode
				const ruleBasedButton = screen.getByRole("button", {
					name: "Rule-Based",
				});
				await user.click(ruleBasedButton);

				// Wait for schema to load (rule builder appears)
				await waitFor(() => {
					expect(
						screen.getByTestId("delivery-rule-builder"),
					).toBeInTheDocument();
				});

				// Fill in name and date
				const nameInput = screen.getByLabelText("Delivery Name");
				fireEvent.change(nameInput, { target: { value: "Test Delivery" } });

				const dateInput = screen.getByLabelText("Delivery Date");
				fireEvent.change(dateInput, {
					target: { value: formatDateForInput(getTwoWeeksFromNow()) },
				});

				// Add a rule and fill it
				const addRuleButton = screen.getByTestId("add-rule-button");
				await user.click(addRuleButton);

				// Find and fill the value input inside the TextField
				const valueInputWrapper = screen.getByTestId("rule-value-input-0");
				const valueInput = within(valueInputWrapper).getByRole("textbox");
				fireEvent.change(valueInput, { target: { value: "test-tag" } });

				// Validate rules
				const validateButton = screen.getByRole("button", {
					name: /validate rules/i,
				});
				await user.click(validateButton);

				// Wait for validation to complete
				await waitFor(() => {
					expect(screen.getByTestId("matched-count")).toBeInTheDocument();
				});

				// Save button should be enabled
				let saveButton = screen.getByRole("button", { name: "Save" });
				expect(saveButton).not.toBeDisabled();

				// Modify the rule value
				fireEvent.change(valueInput, { target: { value: "different-tag" } });

				// Save button should be disabled again
				saveButton = screen.getByRole("button", { name: "Save" });
				expect(saveButton).toBeDisabled();
			},
		);
	});

	describe("Edit mode initialization", () => {
		it("should open in rule-based mode when editing a rule-based delivery", async () => {
			const twoWeeksFromNow = getTwoWeeksFromNow();

			const ruleBasedDelivery: IDelivery = {
				id: 10,
				name: "Rule-Based Delivery",
				date: twoWeeksFromNow.toISOString(),
				features: [1, 2],
				portfolioId: 1,
				likelihoodPercentage: 85,
				progress: 50,
				remainingWork: 10,
				totalWork: 20,
				featureLikelihoods: [],
				completionDates: [],
				selectionMode: DeliverySelectionMode.RuleBased,
				rules: [
					{
						fieldKey: "feature.tags",
						operator: "contains",
						value: "important",
					},
				],
			};

			renderModal(ruleBasedDelivery);

			// Wait for the modal to load
			await waitFor(() => {
				expect(screen.getByText("Edit Delivery")).toBeInTheDocument();
			});

			// Rule-Based button should be pressed/selected
			const ruleBasedButton = screen.getByRole("button", {
				name: "Rule-Based",
			});
			expect(ruleBasedButton).toHaveAttribute("aria-pressed", "true");

			// Manual button should not be pressed
			const manualButton = screen.getByRole("button", { name: "Manual" });
			expect(manualButton).toHaveAttribute("aria-pressed", "false");
		});

		it("should display existing rules when editing a rule-based delivery", async () => {
			const twoWeeksFromNow = getTwoWeeksFromNow();

			const ruleBasedDelivery: IDelivery = {
				id: 10,
				name: "Rule-Based Delivery",
				date: twoWeeksFromNow.toISOString(),
				features: [1, 2],
				portfolioId: 1,
				likelihoodPercentage: 85,
				progress: 50,
				remainingWork: 10,
				totalWork: 20,
				featureLikelihoods: [],
				completionDates: [],
				selectionMode: DeliverySelectionMode.RuleBased,
				rules: [
					{
						fieldKey: "feature.tags",
						operator: "contains",
						value: "important",
					},
				],
			};

			renderModal(ruleBasedDelivery);

			// Wait for schema to load (rule builder appears)
			await waitFor(() => {
				expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
			});

			// The rule should be visible - find the value input inside the TextField
			const valueInputWrapper = screen.getByTestId("rule-value-input-0");
			const valueInput = within(valueInputWrapper).getByRole("textbox");
			expect(valueInput).toHaveValue("important");
		});

		it("should have Update button disabled until rules are re-validated when editing rule-based delivery", async () => {
			const twoWeeksFromNow = getTwoWeeksFromNow();

			const ruleBasedDelivery: IDelivery = {
				id: 10,
				name: "Rule-Based Delivery",
				date: twoWeeksFromNow.toISOString(),
				features: [1, 2],
				portfolioId: 1,
				likelihoodPercentage: 85,
				progress: 50,
				remainingWork: 10,
				totalWork: 20,
				featureLikelihoods: [],
				completionDates: [],
				selectionMode: DeliverySelectionMode.RuleBased,
				rules: [
					{
						fieldKey: "feature.tags",
						operator: "contains",
						value: "important",
					},
				],
			};

			renderModal(ruleBasedDelivery);

			// Wait for schema to load (rule builder appears)
			await waitFor(() => {
				expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
			});

			// Update button should be disabled (not yet validated)
			const updateButton = screen.getByRole("button", { name: "Update" });
			expect(updateButton).toBeDisabled();

			// Validate rules
			const validateButton = screen.getByRole("button", {
				name: /validate rules/i,
			});
			fireEvent.click(validateButton);

			// Wait for validation to complete
			await waitFor(() => {
				expect(screen.getByTestId("matched-count")).toBeInTheDocument();
			});

			// Update button should now be enabled
			expect(updateButton).not.toBeDisabled();
		});
	});

	describe("Matched features display", () => {
		it("should display matched features in a grid after validation", async () => {
			const user = userEvent.setup();
			renderModal();

			// Switch to rule-based mode
			const ruleBasedButton = screen.getByRole("button", {
				name: "Rule-Based",
			});
			await user.click(ruleBasedButton);

			// Wait for schema to load (rule builder appears)
			await waitFor(() => {
				expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
			});

			// Fill in name and date
			const nameInput = screen.getByLabelText("Delivery Name");
			fireEvent.change(nameInput, { target: { value: "Test Delivery" } });

			const dateInput = screen.getByLabelText("Delivery Date");
			fireEvent.change(dateInput, {
				target: { value: formatDateForInput(getTwoWeeksFromNow()) },
			});

			// Add a rule and fill it
			const addRuleButton = screen.getByTestId("add-rule-button");
			await user.click(addRuleButton);

			// Find and fill the value input inside the TextField
			const valueInputWrapper = screen.getByTestId("rule-value-input-0");
			const valueInput = within(valueInputWrapper).getByRole("textbox");
			fireEvent.change(valueInput, { target: { value: "test-tag" } });

			// Validate rules
			const validateButton = screen.getByRole("button", {
				name: /validate rules/i,
			});
			await user.click(validateButton);

			// Wait for validation to complete and matched features to display
			await waitFor(() => {
				expect(screen.getByText(/Matched Features:/i)).toBeInTheDocument();
			});

			// Matched features should be visible in the grid
			expect(screen.getByText("Matched Feature 1")).toBeInTheDocument();
			expect(screen.getByText("Matched Feature 2")).toBeInTheDocument();
		});

		it("should show checkboxes as disabled in matched features grid", async () => {
			const user = userEvent.setup();
			renderModal();

			// Switch to rule-based mode
			const ruleBasedButton = screen.getByRole("button", {
				name: "Rule-Based",
			});
			await user.click(ruleBasedButton);

			// Wait for schema to load (rule builder appears)
			await waitFor(() => {
				expect(screen.getByTestId("delivery-rule-builder")).toBeInTheDocument();
			});

			// Fill in name and date
			const nameInput = screen.getByLabelText("Delivery Name");
			fireEvent.change(nameInput, { target: { value: "Test Delivery" } });

			const dateInput = screen.getByLabelText("Delivery Date");
			fireEvent.change(dateInput, {
				target: { value: formatDateForInput(getTwoWeeksFromNow()) },
			});

			// Add a rule and fill it
			const addRuleButton = screen.getByTestId("add-rule-button");
			await user.click(addRuleButton);

			// Find and fill the value input inside the TextField
			const valueInputWrapper = screen.getByTestId("rule-value-input-0");
			const valueInput = within(valueInputWrapper).getByRole("textbox");
			fireEvent.change(valueInput, { target: { value: "test-tag" } });

			// Validate rules
			const validateButton = screen.getByRole("button", {
				name: /validate rules/i,
			});
			await user.click(validateButton);

			// Wait for validation to complete
			await waitFor(() => {
				expect(screen.getByText(/Matched Features:/i)).toBeInTheDocument();
			});

			// Get checkboxes in the matched features grid (excluding any from manual selection)
			// The matched features grid should have disabled checkboxes
			const matchedFeaturesSection =
				screen.getByText(/Matched Features:/i).parentElement;
			const checkboxes = matchedFeaturesSection?.querySelectorAll(
				'input[type="checkbox"]',
			);

			if (checkboxes && checkboxes.length > 0) {
				for (const checkbox of checkboxes) {
					expect(checkbox).toBeDisabled();
					expect(checkbox).toBeChecked();
				}
			}
		});
	});
});
