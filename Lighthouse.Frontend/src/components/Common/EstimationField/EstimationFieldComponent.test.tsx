import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import EstimationFieldComponent from "./EstimationFieldComponent";

describe("EstimationFieldComponent", () => {
	const mockAdditionalFields: IAdditionalFieldDefinition[] = [
		{ id: 1, displayName: "T-Shirt Size", reference: "custom.tshirtSize" },
		{ id: 2, displayName: "Story Points", reference: "custom.storyPoints" },
	];

	const mockOnChange = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders the Estimation Field selector", () => {
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={null}
				onEstimationFieldChange={mockOnChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(screen.getByRole("combobox")).toBeInTheDocument();
	});

	it("calls onChange with field id when a field is selected", () => {
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={null}
				onEstimationFieldChange={mockOnChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		const select = screen.getByRole("combobox");
		fireEvent.mouseDown(select);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("T-Shirt Size"));

		expect(mockOnChange).toHaveBeenCalledWith(1);
	});

	it("calls onChange with null when None is selected", () => {
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={1}
				onEstimationFieldChange={mockOnChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));

		const select = screen.getByRole("combobox");
		fireEvent.mouseDown(select);

		const listbox = within(screen.getByRole("listbox"));
		fireEvent.click(listbox.getByText("None"));

		expect(mockOnChange).toHaveBeenCalledWith(null);
	});

	it("displays the currently selected field", () => {
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={2}
				onEstimationFieldChange={mockOnChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(screen.getByText("Story Points")).toBeInTheDocument();
	});
});
