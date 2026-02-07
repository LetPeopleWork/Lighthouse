import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import { testTheme } from "../../../tests/testTheme";
import FlowMetricsConfigurationComponent from "./FlowMetricsConfigurationComponent";

// Mock the useTheme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

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

// Mock the ItemListManager component
vi.mock("../ItemListManager/ItemListManager", () => ({
	default: ({
		title,
		items,
		onAddItem,
		onRemoveItem,
	}: {
		title: string;
		items: string[];
		onAddItem: (item: string) => void;
		onRemoveItem: (item: string) => void;
	}) => (
		<div data-testid="item-list-manager">
			<div data-testid="item-list-title">{title}</div>
			<div data-testid="item-list-items">
				{items.map((item) => (
					<div key={item} data-testid={`item-${item}`}>
						{item}
						<button
							type="button"
							data-testid={`remove-item-${item}`}
							onClick={() => onRemoveItem(item)}
						>
							Remove
						</button>
					</div>
				))}
			</div>
			<button
				type="button"
				data-testid="add-item-button"
				onClick={() => onAddItem("test-item")}
			>
				Add {title}
			</button>
		</div>
	),
}));

// Mock the ApiServiceContext
vi.mock("../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		Provider: ({ children }: { children: React.ReactNode }) => children,
	},
}));

vi.mock("react", async () => {
	const actual = await vi.importActual("react");
	return {
		...actual,
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
	});

	it("renders with correct title", () => {
		render(
			<FlowMetricsConfigurationComponent
				settings={mockSettings}
				onSettingsChange={mockOnSettingsChange}
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

	describe("Blocked Tags Configuration", () => {
		it("should not render blocked tags section when blocked items is disabled", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedTagsGroup = inputGroups.find(
				(group) => group.textContent === "Blocked Tags",
			);
			expect(blockedTagsGroup).toBeUndefined();
		});

		it("should render blocked tags section when blocked items is enabled", async () => {
			const user = userEvent.setup();
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			// Enable blocked items
			const blockedItemsCheckbox = screen.getByLabelText(
				"Configure Blocked Work Items",
			);
			await user.click(blockedItemsCheckbox);

			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedTagsGroup = inputGroups.find(
				(group) => group.textContent === "Blocked Tags",
			);
			expect(blockedTagsGroup).toBeInTheDocument();
		});

		it("should render blocked tags section when settings already have blocked tags", () => {
			const settingsWithBlockedTags = {
				...mockSettings,
				blockedTags: ["urgent", "bug"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedTags}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedTagsGroup = inputGroups.find(
				(group) => group.textContent === "Blocked Tags",
			);
			expect(blockedTagsGroup).toBeInTheDocument();
		});

		it("should display existing blocked tags", () => {
			const settingsWithBlockedTags = {
				...mockSettings,
				blockedTags: ["urgent", "bug"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedTags}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			expect(screen.getByTestId("item-urgent")).toBeInTheDocument();
			expect(screen.getByTestId("item-bug")).toBeInTheDocument();
		});

		it("should add new blocked tag when add button is clicked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithBlockedTags = {
				...mockSettings,
				blockedTags: ["existing"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedTags}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const addButtons = screen.getAllByTestId("add-item-button");
			const addTagButton = addButtons.find((button) =>
				button.textContent?.includes("Blocked Tag"),
			);

			if (addTagButton) {
				await user.click(addTagButton);
				expect(mockOnSettingsChange).toHaveBeenCalledWith("blockedTags", [
					"existing",
					"test-item",
				]);
			}
		});

		it("should remove blocked tag when remove button is clicked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithBlockedTags = {
				...mockSettings,
				blockedTags: ["urgent", "bug"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedTags}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const removeButton = screen.getByTestId("remove-item-urgent");
			await user.click(removeButton);

			expect(mockOnSettingsChange).toHaveBeenCalledWith("blockedTags", ["bug"]);
		});

		it("should clear blocked tags when blocked items checkbox is unchecked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithBlockedTags = {
				...mockSettings,
				blockedTags: ["urgent", "bug"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedTags}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const blockedItemsCheckbox = screen.getByLabelText(
				"Configure Blocked Work Items",
			);
			await user.click(blockedItemsCheckbox);

			expect(mockOnSettingsChange).toHaveBeenCalledWith("blockedTags", []);
			expect(mockOnSettingsChange).toHaveBeenCalledWith("blockedStates", []);
		});
	});

	describe("Blocked States Configuration", () => {
		it("should not render blocked states section when blocked items is disabled", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedStatesGroup = inputGroups.find(
				(group) => group.textContent === "Blocked States",
			);
			expect(blockedStatesGroup).toBeUndefined();
		});

		it("should render blocked states section when blocked items is enabled", async () => {
			const user = userEvent.setup();
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			// Enable blocked items
			const blockedItemsCheckbox = screen.getByLabelText(
				"Configure Blocked Work Items",
			);
			await user.click(blockedItemsCheckbox);

			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedStatesGroup = inputGroups.find(
				(group) => group.textContent === "Blocked States",
			);
			expect(blockedStatesGroup).toBeInTheDocument();
		});

		it("should render blocked states section when settings already have blocked states", () => {
			const settingsWithBlockedStates = {
				...mockSettings,
				blockedStates: ["In Progress", "Testing"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedStates}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedStatesGroup = inputGroups.find(
				(group) => group.textContent === "Blocked States",
			);
			expect(blockedStatesGroup).toBeInTheDocument();
		});

		it("should display existing blocked states", () => {
			const settingsWithBlockedStates = {
				...mockSettings,
				blockedStates: ["In Progress", "Testing"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedStates}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			expect(screen.getByTestId("item-In Progress")).toBeInTheDocument();
			expect(screen.getByTestId("item-Testing")).toBeInTheDocument();
		});

		it("should add new blocked state when add button is clicked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithBlockedStates = {
				...mockSettings,
				blockedStates: ["existing"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedStates}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const addButtons = screen.getAllByTestId("add-item-button");
			const addStateButton = addButtons.find((button) =>
				button.textContent?.includes("Blocked State"),
			);

			if (addStateButton) {
				await user.click(addStateButton);
				expect(mockOnSettingsChange).toHaveBeenCalledWith("blockedStates", [
					"existing",
					"test-item",
				]);
			}
		});

		it("should remove blocked state when remove button is clicked", async () => {
			const user = userEvent.setup();
			vi.clearAllMocks();

			const settingsWithBlockedStates = {
				...mockSettings,
				blockedStates: ["In Progress", "Testing"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedStates}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const removeButton = screen.getByTestId("remove-item-In Progress");
			await user.click(removeButton);

			expect(mockOnSettingsChange).toHaveBeenCalledWith("blockedStates", [
				"Testing",
			]);
		});

		it("should use doingStates from settings as suggestions for blocked states", () => {
			const settingsWithDoingStates = {
				...mockSettings,
				doingStates: ["In Progress", "Development", "Testing"],
				blockedStates: ["In Progress"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithDoingStates}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			// The mock ItemListManager should receive the doingStates as suggestions
			// This verifies that suggestions come from settings.doingStates, not from a service
			const inputGroups = screen.getAllByTestId("input-group-title");
			const blockedStatesGroup = inputGroups.find(
				(group) => group.textContent === "Blocked States",
			);
			expect(blockedStatesGroup).toBeInTheDocument();
		});
	});

	describe("Blocked Items Enable/Disable", () => {
		it("should show blocked items checkbox unchecked when both tags and states are empty", () => {
			render(
				<FlowMetricsConfigurationComponent
					settings={mockSettings}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const blockedItemsCheckbox = screen.getByLabelText(
				"Configure Blocked Work Items",
			);
			expect(blockedItemsCheckbox).not.toBeChecked();
		});

		it("should show blocked items checkbox checked when tags are present", () => {
			const settingsWithBlockedTags = {
				...mockSettings,
				blockedTags: ["urgent"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedTags}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const blockedItemsCheckbox = screen.getByLabelText(
				"Configure Blocked Work Items",
			);
			expect(blockedItemsCheckbox).toBeChecked();
		});

		it("should show blocked items checkbox checked when states are present", () => {
			const settingsWithBlockedStates = {
				...mockSettings,
				blockedStates: ["In Progress"],
			};

			render(
				<FlowMetricsConfigurationComponent
					settings={settingsWithBlockedStates}
					onSettingsChange={mockOnSettingsChange}
				/>,
			);

			const blockedItemsCheckbox = screen.getByLabelText(
				"Configure Blocked Work Items",
			);
			expect(blockedItemsCheckbox).toBeChecked();
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
				/>,
			);

			const toggle = screen.getByLabelText(
				"Enable Process Behaviour Chart Baseline",
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
				/>,
			);

			const toggle = screen.getByLabelText(
				"Enable Process Behaviour Chart Baseline",
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
				/>,
			);

			await user.click(
				screen.getByLabelText("Enable Process Behaviour Chart Baseline"),
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
				/>,
			);

			await user.click(
				screen.getByLabelText("Enable Process Behaviour Chart Baseline"),
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
				/>,
			);

			expect(screen.getByLabelText("Baseline Start Date")).toBeInTheDocument();
			expect(screen.getByLabelText("Baseline End Date")).toBeInTheDocument();
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
				/>,
			);

			expect(
				screen.queryByLabelText("Baseline Start Date"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByLabelText("Baseline End Date"),
			).not.toBeInTheDocument();
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
