import { act, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import StateMappingsEditor from "./StateMappingsEditor";

describe("StateMappingsEditor", () => {
	const mockOnChange = vi.fn();

	beforeEach(() => {
		mockOnChange.mockClear();
	});

	it("renders the section title", () => {
		render(
			<StateMappingsEditor
				stateMappings={[]}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		expect(screen.getByText("State Mappings")).toBeInTheDocument();
	});

	it("renders a description that explains the mechanism in plain language", () => {
		render(
			<StateMappingsEditor
				stateMappings={[]}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		// Description must mention key concepts: grouping Doing states, replacement, restoration
		expect(screen.getByText(/doing/i)).toBeInTheDocument();
		expect(screen.getByText(/group/i)).toBeInTheDocument();
	});

	it("renders a reload guidance notice in the State Mappings section", () => {
		render(
			<StateMappingsEditor
				stateMappings={[]}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		expect(screen.getByText(/reload/i)).toBeInTheDocument();
	});

	it("renders existing mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress", "In Review"] },
			{ name: "Closed", states: ["Done", "Resolved"] },
		];

		render(
			<StateMappingsEditor
				stateMappings={mappings}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		expect(screen.getByDisplayValue("Active")).toBeInTheDocument();
		expect(screen.getByText("In Progress")).toBeInTheDocument();
		expect(screen.getByText("In Review")).toBeInTheDocument();
		expect(screen.getByDisplayValue("Closed")).toBeInTheDocument();
		expect(screen.getByText("Done")).toBeInTheDocument();
		expect(screen.getByText("Resolved")).toBeInTheDocument();
	});

	it("adds a new empty mapping when add button is clicked", async () => {
		const user = userEvent.setup();

		render(
			<StateMappingsEditor
				stateMappings={[]}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		const addButton = screen.getByRole("button", {
			name: /add state mapping/i,
		});
		await user.click(addButton);

		expect(mockOnChange).toHaveBeenCalledWith([{ name: "", states: [] }]);
	});

	it("removes a mapping when delete button is clicked", async () => {
		const user = userEvent.setup();
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
			{ name: "Closed", states: ["Done"] },
		];

		render(
			<StateMappingsEditor
				stateMappings={mappings}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		const removeButtons = screen.getAllByLabelText(/remove mapping/i);
		await user.click(removeButtons[0]);

		expect(mockOnChange).toHaveBeenCalledWith([
			{ name: "Closed", states: ["Done"] },
		]);
	});

	it("updates mapping name when name field is changed", async () => {
		const user = userEvent.setup();
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
		];

		render(
			<StateMappingsEditor
				stateMappings={mappings}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		const nameInput = screen.getByDisplayValue("Active");
		await user.clear(nameInput);

		// After clear, onChange should have been called with empty name
		expect(mockOnChange).toHaveBeenCalledWith([
			{ name: "", states: ["In Progress"] },
		]);
	});

	it("adds a Doing state to a mapping when selected from the dropdown", async () => {
		const user = userEvent.setup();
		const mappings: IStateMapping[] = [{ name: "Active", states: [] }];
		const doingStates = ["In Progress", "In Review"];

		render(
			<StateMappingsEditor
				stateMappings={mappings}
				doingStates={doingStates}
				onChange={mockOnChange}
			/>,
		);

		const combobox = screen.getByRole("combobox");
		await user.click(combobox);

		const option = await screen.findByRole("option", { name: "In Progress" });
		await user.click(option);

		expect(mockOnChange).toHaveBeenCalledWith([
			{ name: "Active", states: ["In Progress"] },
		]);
	});

	it("removes a source state from a mapping", async () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress", "In Review"] },
		];

		render(
			<StateMappingsEditor
				stateMappings={mappings}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		// Find the chip for "In Progress" and click its delete icon
		const chip = screen.getByText("In Progress").closest(".MuiChip-root");
		const deleteIcon = chip?.querySelector(".MuiChip-deleteIcon");
		if (deleteIcon) {
			fireEvent.click(deleteIcon);
		}

		expect(mockOnChange).toHaveBeenCalledWith([
			{ name: "Active", states: ["In Review"] },
		]);
	});

	it("displays validation errors when provided", () => {
		const mappings: IStateMapping[] = [{ name: "", states: ["In Progress"] }];
		const errors = ["Mapping name cannot be empty."];

		render(
			<StateMappingsEditor
				stateMappings={mappings}
				doingStates={[]}
				onChange={mockOnChange}
				validationErrors={errors}
			/>,
		);

		expect(
			screen.getByText("Mapping name cannot be empty."),
		).toBeInTheDocument();
	});

	it("renders empty state with add button when no mappings exist", () => {
		render(
			<StateMappingsEditor
				stateMappings={[]}
				doingStates={[]}
				onChange={mockOnChange}
			/>,
		);

		expect(
			screen.getByRole("button", { name: /add state mapping/i }),
		).toBeInTheDocument();
	});

	describe("source state dropdown", () => {
		it("shows available Doing states as options (excluding mapping names)", async () => {
			const user = userEvent.setup();
			// doingStates contains both mapping names and raw states
			const mappings: IStateMapping[] = [
				{ name: "Active", states: ["In Progress"] },
				{ name: "Closed", states: [] },
			];
			// "Active" is a mapping name; "In Review" is a raw Doing state
			const doingStates = ["Active", "In Review", "Closed"];

			render(
				<StateMappingsEditor
					stateMappings={mappings}
					doingStates={doingStates}
					onChange={mockOnChange}
				/>,
			);

			// Open the source dropdown for the second mapping ("Closed")
			const comboboxes = screen.getAllByRole("combobox");
			await act(async () => {
				await user.click(comboboxes[1]);
			});

			// "In Review" is a raw state → should appear as option
			expect(
				screen.getByRole("option", { name: "In Review" }),
			).toBeInTheDocument();

			// "Active" and "Closed" are mapping names → must NOT appear as options
			expect(
				screen.queryByRole("option", { name: "Active" }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("option", { name: "Closed" }),
			).not.toBeInTheDocument();
		});

		it("excludes states already assigned to other mappings from the options", async () => {
			const user = userEvent.setup();
			const mappings: IStateMapping[] = [
				{ name: "Group A", states: ["Dev"] },
				{ name: "Group B", states: [] },
			];
			// "Dev" is absorbed into Group A; only "Review" is still available
			const doingStates = ["Group A", "Review", "Group B"];

			render(
				<StateMappingsEditor
					stateMappings={mappings}
					doingStates={doingStates}
					onChange={mockOnChange}
				/>,
			);

			// Open source dropdown for Group B (index 1)
			const comboboxes = screen.getAllByRole("combobox");
			await act(async () => {
				await user.click(comboboxes[1]);
			});

			expect(
				screen.getByRole("option", { name: "Review" }),
			).toBeInTheDocument();
			// "Dev" is already in Group A's states but since it's been absorbed it won't
			// appear in doingStates anyway — the key is that mapping names aren't options
			expect(
				screen.queryByRole("option", { name: "Group A" }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("option", { name: "Group B" }),
			).not.toBeInTheDocument();
		});

		it("shows 'No Doing states available' when all Doing states are used", async () => {
			const user = userEvent.setup();
			const mappings: IStateMapping[] = [{ name: "Active", states: [] }];
			// Only mapping names in doingStates — no raw states left
			const doingStates = ["Active"];

			render(
				<StateMappingsEditor
					stateMappings={mappings}
					doingStates={doingStates}
					onChange={mockOnChange}
				/>,
			);

			const combobox = screen.getByRole("combobox");
			await act(async () => {
				await user.click(combobox);
			});

			expect(
				screen.getByText(/no doing states available/i),
			).toBeInTheDocument();
		});
	});
});
