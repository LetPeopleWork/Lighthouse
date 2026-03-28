import { fireEvent, render, screen } from "@testing-library/react";
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
		render(<StateMappingsEditor stateMappings={[]} onChange={mockOnChange} />);

		expect(screen.getByText("State Mappings")).toBeInTheDocument();
	});

	it("renders helper text explaining the purpose", () => {
		render(<StateMappingsEditor stateMappings={[]} onChange={mockOnChange} />);

		expect(
			screen.getByText(/group one or more provider states/i),
		).toBeInTheDocument();
	});

	it("renders existing mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress", "In Review"] },
			{ name: "Closed", states: ["Done", "Resolved"] },
		];

		render(
			<StateMappingsEditor stateMappings={mappings} onChange={mockOnChange} />,
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

		render(<StateMappingsEditor stateMappings={[]} onChange={mockOnChange} />);

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
			<StateMappingsEditor stateMappings={mappings} onChange={mockOnChange} />,
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
			<StateMappingsEditor stateMappings={mappings} onChange={mockOnChange} />,
		);

		const nameInput = screen.getByDisplayValue("Active");
		await user.clear(nameInput);

		// After clear, onChange should have been called with empty name
		expect(mockOnChange).toHaveBeenCalledWith([
			{ name: "", states: ["In Progress"] },
		]);
	});

	it("adds a source state to a mapping", async () => {
		const user = userEvent.setup();
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
		];

		render(
			<StateMappingsEditor stateMappings={mappings} onChange={mockOnChange} />,
		);

		const stateInput = screen.getByLabelText("New Source State");
		await user.type(stateInput, "In Review{Enter}");

		expect(mockOnChange).toHaveBeenCalledWith([
			{ name: "Active", states: ["In Progress", "In Review"] },
		]);
	});

	it("removes a source state from a mapping", async () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress", "In Review"] },
		];

		render(
			<StateMappingsEditor stateMappings={mappings} onChange={mockOnChange} />,
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
				onChange={mockOnChange}
				validationErrors={errors}
			/>,
		);

		expect(
			screen.getByText("Mapping name cannot be empty."),
		).toBeInTheDocument();
	});

	it("renders empty state with add button when no mappings exist", () => {
		render(<StateMappingsEditor stateMappings={[]} onChange={mockOnChange} />);

		expect(
			screen.getByRole("button", { name: /add state mapping/i }),
		).toBeInTheDocument();
	});
});
