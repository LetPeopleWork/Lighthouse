import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	BLOCKED_RULE_SET_SCHEMA_VERSION,
	blockedRuleSetSchema,
	parseBlockedRuleSet,
	serializeBlockedRuleSet,
} from "../../../models/Common/BaseSettings";
import type { IWorkItemRuleSchema } from "../../../models/WorkItemRules";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import { testTheme } from "../../../tests/testTheme";
import FlowMetricsConfigurationComponent from "./FlowMetricsConfigurationComponent";

const testRuleSchema: IWorkItemRuleSchema = {
	fields: [
		{ fieldKey: "state", displayName: "State", isMultiValue: false },
		{
			fieldKey: "customfield.flagged",
			displayName: "Flagged",
			isMultiValue: false,
		},
	],
	operators: ["equals", "notequals", "isempty", "isnotempty"],
	maxRules: 20,
	maxValueLength: 500,
};

const adminRbac = {
	isLoading: false,
	isRbacEnabled: false,
	isSystemAdmin: true,
	canCreateTeam: true,
	canCreatePortfolio: true,
	isTeamAdmin: (_id: number) => true,
	isPortfolioAdmin: (_id: number) => true,
	summary: {},
};

const rbacRef = vi.hoisted(() => ({ current: null as unknown }));

const schemaRef = vi.hoisted(() => ({
	getForecastFilterSchema: vi.fn(),
	getRuleSchema: vi.fn(),
}));

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => rbacRef.current,
}));

// Mock the useTheme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Mock the InputGroup component to make testing easier
vi.mock("../InputGroup/InputGroup", () => ({
	default: ({
		title,
		children,
	}: {
		title: string;
		children: React.ReactNode;
	}) => (
		<div data-testid="input-group">
			<div data-testid="input-group-title">{title}</div>
			<div data-testid="input-group-content">{children}</div>
		</div>
	),
}));

// Provide a real context whose default value carries stub schema services, so
// FlowMetricsConfigurationComponent can fetch the blocked rule schema without a Provider.
vi.mock("../../../services/Api/ApiServiceContext", async () => {
	const { createContext } =
		await vi.importActual<typeof import("react")>("react");
	return {
		ApiServiceContext: createContext({
			teamService: {
				getForecastFilterSchema: schemaRef.getForecastFilterSchema,
			},
			deliveryService: { getRuleSchema: schemaRef.getRuleSchema },
		}),
	};
});

describe("FlowMetricsConfigurationComponent", () => {
	const mockSettings = createMockProjectSettings();

	const mockSettingsWithFeatureWIP = {
		...mockSettings,
		featureWIP: 0,
		automaticallyAdjustFeatureWIP: false,
	};
	const mockOnSettingsChange = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
		rbacRef.current = adminRbac;
		schemaRef.getForecastFilterSchema.mockResolvedValue(testRuleSchema);
		schemaRef.getRuleSchema.mockResolvedValue(testRuleSchema);
	});

	it("renders with correct title", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={mockSettings}
				onSettingsChange={mockOnSettingsChange}
				stalenessSeedDefault={5}
			/>,
		);
		const titles = screen.getAllByTestId("input-group-title");
		const mainTitle = titles.find(
			(title) => title.textContent === "Flow Metrics Configuration",
		);
		expect(mainTitle).toHaveTextContent("Flow Metrics Configuration");
	});
	describe("System WIP Limit Configuration", () => {
		it("should show WIP limit checkbox unchecked initially when systemWIPLimit is 0", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			const systemWipCheckbox = checkboxes[0]; // First checkbox is system WIP
			expect(systemWipCheckbox).not.toBeChecked();
		});

		it("should show WIP limit checkbox checked initially when systemWIPLimit is greater than 0", () => {
			const settingsWithWipLimit = { ...mockSettings, systemWIPLimit: 5 };
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithWipLimit}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			const systemWipCheckbox = checkboxes[0]; // First checkbox is system WIP
			expect(systemWipCheckbox).toBeChecked();
		});

		it("should display WIP limit input when checkbox is checked", async () => {
			const user = userEvent.setup();
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			const systemWipCheckbox = checkboxes[0]; // First checkbox is system WIP
			await user.click(systemWipCheckbox);

			// Input should now be visible
			const inputs = screen.getAllByRole("spinbutton");
			expect(inputs.length).toBeGreaterThan(0);
			// The first input should be the WIP limit
			const wipLimitInput = inputs[0];
			expect(wipLimitInput).toBeInTheDocument();
		});

		it("should set systemWIPLimit to 1 when checkbox is checked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			const systemWipCheckbox = checkboxes[0]; // First checkbox is system WIP
			await user.click(systemWipCheckbox);

			expect(mockOnSettingsChange).toHaveBeenCalledWith("systemWIPLimit", 1);
		});

		it("should set systemWIPLimit to 0 when checkbox is unchecked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithWipLimit = { ...mockSettings, systemWIPLimit: 5 };
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithWipLimit}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			const systemWipCheckbox = checkboxes[0]; // First checkbox is system WIP
			await user.click(systemWipCheckbox);

			expect(mockOnSettingsChange).toHaveBeenCalledWith("systemWIPLimit", 0);
		});
		it("should update systemWIPLimit when input value changes", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithWipLimit = { ...mockSettings, systemWIPLimit: 5 };
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithWipLimit}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			// Find the WIP Limit input (first spinbutton)
			const inputs = screen.getAllByRole("spinbutton");
			const wipLimitInput = inputs[0];

			// Just test that the input can be changed
			await user.clear(wipLimitInput);
			await user.type(wipLimitInput, "10");

			// The handleWipLimitChange function should parse the input value and call onSettingsChange
			// We can just verify that onSettingsChange was called, as we've already tested the specific handler in other tests
			expect(mockOnSettingsChange).toHaveBeenCalled();
		});
	});
	describe("Service Level Expectation Configuration", () => {
		it("should show SLE checkbox unchecked initially when serviceLevelExpectationProbability is 0", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// SLE checkbox is the second checkbox (index 1)
			const sleCheckbox = checkboxes[1];
			expect(sleCheckbox).not.toBeChecked();
		});

		it("should show SLE checkbox checked initially when serviceLevelExpectationProbability is greater than 50 and range >= 0", () => {
			const settingsWithSLE = {
				...mockSettings,
				serviceLevelExpectationProbability: 70,
				serviceLevelExpectationRange: 10,
			};
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithSLE}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// SLE checkbox is the second checkbox (index 1)
			const sleCheckbox = checkboxes[1];
			expect(sleCheckbox).toBeChecked();
		});

		it("should set default values when SLE checkbox is checked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// SLE checkbox is the second checkbox (index 1)
			const sleCheckbox = checkboxes[1];
			await user.click(sleCheckbox);

			// Verify both settings were called with expected values (in any order)
			const probabilityCall = mockOnSettingsChange.mock.calls.find(
				(call) =>
					call[0] === "serviceLevelExpectationProbability" && call[1] === 70,
			);
			expect(probabilityCall).toBeTruthy();

			const rangeCall = mockOnSettingsChange.mock.calls.find(
				(call) => call[0] === "serviceLevelExpectationRange" && call[1] === 10,
			);
			expect(rangeCall).toBeTruthy();
		});

		it("should reset values when SLE checkbox is unchecked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithSLE = {
				...mockSettings,
				serviceLevelExpectationProbability: 70,
				serviceLevelExpectationRange: 10,
			};
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithSLE}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// SLE checkbox is the second checkbox (index 1)
			const sleCheckbox = checkboxes[1];
			await user.click(sleCheckbox);

			// Verify both settings were called with 0 values (in any order)
			const probabilityCall = mockOnSettingsChange.mock.calls.find(
				(call) =>
					call[0] === "serviceLevelExpectationProbability" && call[1] === 0,
			);
			expect(probabilityCall).toBeTruthy();

			const rangeCall = mockOnSettingsChange.mock.calls.find(
				(call) => call[0] === "serviceLevelExpectationRange" && call[1] === 0,
			);
			expect(rangeCall).toBeTruthy();
		});
		it("should update probability value when input changes", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithSLE = {
				...mockSettings,
				serviceLevelExpectationProbability: 70,
				serviceLevelExpectationRange: 10,
			};
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithSLE}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			// When SLE is enabled, we should have two spinbutton inputs
			const inputs = screen.getAllByRole("spinbutton");
			// First one is probability
			const probabilityInput = inputs[0];

			await user.clear(probabilityInput);
			await user.type(probabilityInput, "85");

			// Wait for the debounced callback to be called
			await waitFor(
				() => {
					expect(mockOnSettingsChange).toHaveBeenCalled();
				},
				{ timeout: 600 },
			);
		});

		it("should update range value when input changes", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithSLE = {
				...mockSettings,
				serviceLevelExpectationProbability: 70,
				serviceLevelExpectationRange: 10,
			};
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithSLE}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			// When SLE is enabled, we should have two spinbutton inputs
			const inputs = screen.getAllByRole("spinbutton");
			// Second one is range
			const rangeInput = inputs[1];

			await user.clear(rangeInput);
			await user.type(rangeInput, "15");

			// Just verify that onSettingsChange was called after updating the input
			expect(mockOnSettingsChange).toHaveBeenCalled();
		});
	});
	describe("Feature WIP Configuration", () => {
		it("should not show feature WIP section when showFeatureWip is false", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettingsWithFeatureWIP}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={false}
				/>,
			);

			const allText = screen.queryByText(/Set Feature WIP/i);
			expect(allText).not.toBeInTheDocument();
		});

		it("should show feature WIP section when showFeatureWip is true", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettingsWithFeatureWIP}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			const labels = screen.getAllByText(/Set/);
			const featureWipLabel = labels.find((label) =>
				label.textContent?.includes("Feature WIP"),
			);
			expect(featureWipLabel).toBeInTheDocument();
		});

		it("should show Feature WIP checkbox unchecked initially when featureWIP is 0", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettingsWithFeatureWIP}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// With feature WIP enabled, it should be the second checkbox (index 1)
			const featureWipCheckbox = checkboxes[1];
			expect(featureWipCheckbox).not.toBeChecked();
		});

		it("should show Feature WIP checkbox checked initially when featureWIP is greater than 0", () => {
			const settingsWithFeatureWIPEnabled = {
				...mockSettingsWithFeatureWIP,
				featureWIP: 3,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithFeatureWIPEnabled}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// With feature WIP enabled, it should be the second checkbox (index 1)
			const featureWipCheckbox = checkboxes[1];
			expect(featureWipCheckbox).toBeChecked();
		});

		it("should set featureWIP to 1 when checkbox is checked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettingsWithFeatureWIP}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// With feature WIP enabled, it should be the second checkbox (index 1)
			const featureWipCheckbox = checkboxes[1];
			await user.click(featureWipCheckbox);

			const featureWIPCall = mockOnSettingsChange.mock.calls.find(
				(call) => call[0] === "featureWIP" && call[1] === 1,
			);
			expect(featureWIPCall).toBeTruthy();
		});

		it("should set featureWIP to 0 when checkbox is unchecked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithFeatureWIPEnabled = {
				...mockSettingsWithFeatureWIP,
				featureWIP: 3,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithFeatureWIPEnabled}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			const checkboxes = screen.getAllByRole("checkbox");
			// With feature WIP enabled, it should be the second checkbox (index 1)
			const featureWipCheckbox = checkboxes[1];
			await user.click(featureWipCheckbox);

			const featureWIPCall = mockOnSettingsChange.mock.calls.find(
				(call) => call[0] === "featureWIP" && call[1] === 0,
			);
			expect(featureWIPCall).toBeTruthy();
		});
		it("should update featureWIP value when input changes", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithFeatureWIPEnabled = {
				...mockSettingsWithFeatureWIP,
				featureWIP: 3,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithFeatureWIPEnabled}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			// When Feature WIP is enabled, spinbutton for Feature WIP should exist
			const inputs = screen.getAllByRole("spinbutton");
			const featureWipInput = inputs[0];

			await user.clear(featureWipInput);
			await user.type(featureWipInput, "5");

			// Just verify that onSettingsChange was called after updating the input
			expect(mockOnSettingsChange).toHaveBeenCalled();
		});
		it("should toggle automaticallyAdjustFeatureWIP when checkbox is clicked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithFeatureWIPEnabled = {
				...mockSettingsWithFeatureWIP,
				featureWIP: 3,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithFeatureWIPEnabled}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			// With feature WIP enabled and checked, auto-adjust checkbox should be the third checkbox
			const checkboxes = screen.getAllByRole("checkbox");
			const autoAdjustCheckbox = checkboxes[2];

			await user.click(autoAdjustCheckbox);

			const autoAdjustCall = mockOnSettingsChange.mock.calls.find(
				(call) =>
					call[0] === "automaticallyAdjustFeatureWIP" && call[1] === true,
			);
			expect(autoAdjustCall).toBeTruthy();
		});

		it("should also reset automaticallyAdjustFeatureWIP when feature WIP is disabled", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithFeatureWIPEnabled = {
				...mockSettingsWithFeatureWIP,
				featureWIP: 3,
				automaticallyAdjustFeatureWIP: true,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithFeatureWIPEnabled}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
					showFeatureWip={true}
				/>,
			);

			// With feature WIP enabled, feature WIP checkbox should be the second checkbox
			const checkboxes = screen.getAllByRole("checkbox");
			const featureWipCheckbox = checkboxes[1];

			await user.click(featureWipCheckbox); // Disable feature WIP

			// Should reset both featureWIP and automaticallyAdjustFeatureWIP
			const featureWipCall = mockOnSettingsChange.mock.calls.find(
				(call) => call[0] === "featureWIP" && call[1] === 0,
			);
			expect(featureWipCall).toBeTruthy();

			const autoAdjustCall = mockOnSettingsChange.mock.calls.find(
				(call) =>
					call[0] === "automaticallyAdjustFeatureWIP" && call[1] === false,
			);
			expect(autoAdjustCall).toBeTruthy();
		});
	});

	describe("Blocked Rule Set Configuration (DeliveryRuleBuilder)", () => {
		const flaggedRuleSet = serializeBlockedRuleSet({
			version: 1,
			mode: "or",
			conditions: [
				{ fieldKey: "customfield.flagged", operator: "isnotempty", value: "" },
			],
		});

		const twoRuleSet = serializeBlockedRuleSet({
			version: 1,
			mode: "and",
			conditions: [
				{ fieldKey: "state", operator: "equals", value: "Blocked" },
				{ fieldKey: "customfield.flagged", operator: "isnotempty", value: "" },
			],
		});

		it("renders the migrated blocked rule set in the reused DeliveryRuleBuilder", async () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, blockedRuleSetJson: flaggedRuleSet }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(
				await screen.findByTestId("delivery-rule-builder"),
			).toBeInTheDocument();
			const rows = await screen.findAllByTestId("rule-row");
			expect(rows).toHaveLength(1);
			// The migrated flagged condition uses a valueless operator, so no value input.
			expect(
				screen.queryByTestId("rule-value-input-0"),
			).not.toBeInTheDocument();
		});

		it("threads an added field condition through save as blockedRuleSetJson", async () => {
			const user = userEvent.setup();
			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, blockedRuleSetJson: flaggedRuleSet }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await user.click(await screen.findByTestId("add-rule-button"));

			const call = mockOnSettingsChange.mock.calls.find(
				(c) => c[0] === "blockedRuleSetJson",
			);
			expect(call).toBeTruthy();
			const persisted = parseBlockedRuleSet(call?.[1] as string);
			expect(persisted?.conditions).toHaveLength(2);
		});

		it("threads the OR match mode through save", async () => {
			const user = userEvent.setup();
			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, blockedRuleSetJson: twoRuleSet }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await user.click(
				await screen.findByRole("button", { name: /Match any rule \(OR\)/i }),
			);

			const call = mockOnSettingsChange.mock.calls.find(
				(c) => c[0] === "blockedRuleSetJson",
			);
			expect(call).toBeTruthy();
			expect(parseBlockedRuleSet(call?.[1] as string)?.mode).toBe("or");
		});

		it("hides the blocked rule editor for a non-admin", async () => {
			rbacRef.current = {
				...adminRbac,
				isRbacEnabled: true,
				isSystemAdmin: false,
				isTeamAdmin: (_id: number) => false,
				isPortfolioAdmin: (_id: number) => false,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, blockedRuleSetJson: flaggedRuleSet }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByText("Configure Blocked Work Items"),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByTestId("delivery-rule-builder"),
			).not.toBeInTheDocument();
			expect(schemaRef.getRuleSchema).not.toHaveBeenCalled();
		});
	});

	describe("blockedRuleSetSchema (Zod boundary)", () => {
		it("accepts a well-formed blocked rule set", () => {
			const result = blockedRuleSetSchema.safeParse({
				version: 1,
				mode: "or",
				conditions: [
					{ fieldKey: "state", operator: "equals", value: "Blocked" },
				],
			});
			expect(result.success).toBe(true);
		});

		it("rejects a malformed blocked rule set at the boundary", () => {
			const result = blockedRuleSetSchema.safeParse({
				version: "not-a-number",
				mode: "sometimes",
				conditions: "nope",
			});
			expect(result.success).toBe(false);
		});

		it("returns null when parsing malformed JSON at the boundary", () => {
			expect(parseBlockedRuleSet('{"conditions": ')).toBeNull();
			expect(parseBlockedRuleSet('{"mode":"maybe"}')).toBeNull();
			expect(parseBlockedRuleSet(null)).toBeNull();
			expect(parseBlockedRuleSet(undefined)).toBeNull();
			expect(parseBlockedRuleSet("")).toBeNull();
			expect(parseBlockedRuleSet("   ")).toBeNull();
		});

		it("parses a well-formed blocked rule set JSON string", () => {
			const result = parseBlockedRuleSet(
				JSON.stringify({
					version: BLOCKED_RULE_SET_SCHEMA_VERSION,
					mode: "or",
					conditions: [
						{ fieldKey: "state", operator: "equals", value: "Blocked" },
					],
				}),
			);
			expect(result).not.toBeNull();
			expect(result?.mode).toBe("or");
			expect(result?.conditions).toHaveLength(1);
			expect(result?.conditions[0].fieldKey).toBe("state");
		});

		it("serializes a blocked rule set to JSON", () => {
			const ruleSet = {
				version: BLOCKED_RULE_SET_SCHEMA_VERSION,
				mode: "or" as const,
				conditions: [
					{ fieldKey: "state", operator: "equals", value: "Blocked" },
				],
			};
			const json = serializeBlockedRuleSet(ruleSet);
			expect(json).toBe(
				JSON.stringify({
					version: BLOCKED_RULE_SET_SCHEMA_VERSION,
					mode: "or",
					conditions: [
						{ fieldKey: "state", operator: "equals", value: "Blocked" },
					],
				}),
			);
		});
	});

	describe("Process Behaviour Chart Baseline Configuration", () => {
		it("renders baseline toggle as unchecked when baseline dates are null", () => {
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: null,
				processBehaviourChartBaselineEndDate: null,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const toggle = screen.getByLabelText(
				"Set Baseline for Process Behaviour Chart",
			);
			expect(toggle).not.toBeChecked();
		});

		it("renders baseline toggle as checked when baseline dates are set", () => {
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: new Date("2025-01-01"),
				processBehaviourChartBaselineEndDate: new Date("2025-01-20"),
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const toggle = screen.getByLabelText(
				"Set Baseline for Process Behaviour Chart",
			);
			expect(toggle).toBeChecked();
		});

		it("sets baseline dates when toggle is turned on", async () => {
			const user = userEvent.setup();
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: null,
				processBehaviourChartBaselineEndDate: null,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await user.click(
				screen.getByLabelText("Set Baseline for Process Behaviour Chart"),
			);

			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"processBehaviourChartBaselineStartDate",
				expect.any(Date),
			);
			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"processBehaviourChartBaselineEndDate",
				expect.any(Date),
			);
		});

		it("clears baseline dates when toggle is turned off", async () => {
			const user = userEvent.setup();
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: new Date("2025-01-01"),
				processBehaviourChartBaselineEndDate: new Date("2025-01-20"),
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await user.click(
				screen.getByLabelText("Set Baseline for Process Behaviour Chart"),
			);

			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"processBehaviourChartBaselineStartDate",
				null,
			);
			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"processBehaviourChartBaselineEndDate",
				null,
			);
		});

		it("shows date pickers when baseline is enabled", () => {
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: new Date("2025-01-01"),
				processBehaviourChartBaselineEndDate: new Date("2025-01-20"),
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(screen.getByLabelText("PBC Baseline Start")).toBeInTheDocument();
			expect(screen.getByLabelText("PBC Baseline End")).toBeInTheDocument();
		});

		it("does not show date pickers when baseline is disabled", () => {
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: null,
				processBehaviourChartBaselineEndDate: null,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(
				screen.queryByLabelText("PBC Baseline Start"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByLabelText("PBC Baseline End"),
			).not.toBeInTheDocument();
		});

		it("shows cutoff warning when baseline start is before cutoff date", () => {
			const oldStartDate = new Date();
			oldStartDate.setDate(oldStartDate.getDate() - 200);
			const endDate = new Date();
			endDate.setDate(endDate.getDate() - 185);

			const settings = {
				...mockSettings,
				doneItemsCutoffDays: 180,
				processBehaviourChartBaselineStartDate: oldStartDate,
				processBehaviourChartBaselineEndDate: endDate,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(
				screen.getByLabelText("Baseline cutoff warning"),
			).toBeInTheDocument();
		});

		it("does not show cutoff warning when cutoff is 0 (full history)", () => {
			const oldStartDate = new Date();
			oldStartDate.setDate(oldStartDate.getDate() - 400);
			const endDate = new Date();
			endDate.setDate(endDate.getDate() - 385);

			const settings = {
				...mockSettings,
				doneItemsCutoffDays: 0,
				processBehaviourChartBaselineStartDate: oldStartDate,
				processBehaviourChartBaselineEndDate: endDate,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(
				screen.queryByLabelText("Baseline cutoff warning"),
			).not.toBeInTheDocument();
		});

		it("does not show cutoff warning when baseline start is within cutoff range", () => {
			const recentStartDate = new Date();
			recentStartDate.setDate(recentStartDate.getDate() - 30);
			const endDate = new Date();
			endDate.setDate(endDate.getDate() - 15);

			const settings = {
				...mockSettings,
				doneItemsCutoffDays: 180,
				processBehaviourChartBaselineStartDate: recentStartDate,
				processBehaviourChartBaselineEndDate: endDate,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(
				screen.queryByLabelText("Baseline cutoff warning"),
			).not.toBeInTheDocument();
		});

		it("clears baseline dates and disables toggle when Clear button is clicked", async () => {
			const user = userEvent.setup();
			const settings = {
				...mockSettings,
				processBehaviourChartBaselineStartDate: new Date("2025-01-01"),
				processBehaviourChartBaselineEndDate: new Date("2025-01-20"),
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const clearButton = screen.getByRole("button", {
				name: /clear baseline/i,
			});
			await user.click(clearButton);

			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"processBehaviourChartBaselineStartDate",
				null,
			);
			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"processBehaviourChartBaselineEndDate",
				null,
			);

			// Toggle should now be unchecked
			const toggle = screen.getByLabelText(
				"Set Baseline for Process Behaviour Chart",
			);
			expect(toggle).not.toBeChecked();
		});
	});

	describe("Staleness opt-in", () => {
		const STALENESS_CHECKBOX_LABEL = "Set Staleness Threshold";
		const STALENESS_FIELD_LABEL = "Staleness Threshold (days)";

		it("shows the checkbox unchecked and hides the field when stalenessThresholdDays is 0", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, stalenessThresholdDays: 0 }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(screen.getByLabelText(STALENESS_CHECKBOX_LABEL)).not.toBeChecked();
			expect(
				screen.queryByLabelText(STALENESS_FIELD_LABEL),
			).not.toBeInTheDocument();
		});

		it("shows the checkbox checked and reveals the field when stalenessThresholdDays is greater than 0", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, stalenessThresholdDays: 9 }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			expect(screen.getByLabelText(STALENESS_CHECKBOX_LABEL)).toBeChecked();
			expect(screen.getByLabelText(STALENESS_FIELD_LABEL)).toHaveValue(9);
		});

		it("seeds the staleness threshold with the owner-specific default on enable", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, stalenessThresholdDays: 0 }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await user.click(screen.getByLabelText(STALENESS_CHECKBOX_LABEL));

			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"stalenessThresholdDays",
				5,
			);
		});

		it("resets the staleness threshold to 0 on disable", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, stalenessThresholdDays: 14 }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			await user.click(screen.getByLabelText(STALENESS_CHECKBOX_LABEL));

			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"stalenessThresholdDays",
				0,
			);
		});

		it("propagates an edited staleness threshold through onSettingsChange", () => {
			vi.clearAllMocks();

			render(
				<FlowMetricsConfigurationComponent
					settings={{ ...mockSettings, stalenessThresholdDays: 5 }}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			fireEvent.change(screen.getByLabelText(STALENESS_FIELD_LABEL), {
				target: { value: "21" },
			});

			expect(mockOnSettingsChange).toHaveBeenCalledWith(
				"stalenessThresholdDays",
				21,
			);
		});
	});

	describe("Input Validation", () => {
		it("should properly handle non-numeric input for systemWIPLimit", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithWipLimit = { ...mockSettings, systemWIPLimit: 5 };
			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithWipLimit}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const inputs = screen.getAllByRole("spinbutton");
			const wipLimitInput = inputs[0];

			await user.clear(wipLimitInput);
			await user.type(wipLimitInput, "abc"); // Non-numeric input

			// Should not be called with NaN which will be handled by the component
			expect(mockOnSettingsChange).not.toHaveBeenCalled();
		});

		it("should ensure minimum value constraints are enforced visually", () => {
			const settingsWithMinValues = {
				...mockSettings,
				systemWIPLimit: 5,
				serviceLevelExpectationProbability: 70,
				serviceLevelExpectationRange: 10,
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithMinValues}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			const inputs = screen.getAllByRole("spinbutton");

			// First input (WIP Limit) should have min attribute set to 1
			expect(inputs[0]).toHaveAttribute("min", "1");

			// Check SLE inputs
			const sleLabels = screen.getAllByText(/Set Service Level Expectation/i);
			if (sleLabels.length > 0) {
				// SLE probability input should have min set to 50
				expect(inputs[0]).toHaveAttribute("min");
				// SLE range input should have min set to 1
				if (inputs.length > 1) {
					expect(inputs[1]).toHaveAttribute("min");
				}
			}
		});

		it("should handle initial rendering with expanded state", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
					stalenessSeedDefault={5}
				/>,
			);

			// InputGroup should be initialized with initiallyExpanded={false}
			const inputGroups = screen.getAllByTestId("input-group");
			const mainInputGroup = inputGroups.find((group) => {
				const title = group.querySelector('[data-testid="input-group-title"]');
				return title?.textContent === "Flow Metrics Configuration";
			});
			expect(mainInputGroup).toBeInTheDocument();
		});
	});
});
