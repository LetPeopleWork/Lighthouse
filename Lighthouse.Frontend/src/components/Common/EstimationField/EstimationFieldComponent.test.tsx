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

	it("renders the estimation unit text field", () => {
		const mockOnUnitChange = vi.fn();
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={null}
				onEstimationFieldChange={mockOnChange}
				estimationUnit={null}
				onEstimationUnitChange={mockOnUnitChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(screen.getByLabelText("Estimation Unit")).toBeInTheDocument();
	});

	it("calls onEstimationUnitChange when unit text is entered", () => {
		const mockOnUnitChange = vi.fn();
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={null}
				onEstimationFieldChange={mockOnChange}
				estimationUnit=""
				onEstimationUnitChange={mockOnUnitChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		fireEvent.change(screen.getByLabelText("Estimation Unit"), {
			target: { value: "Points" },
		});

		expect(mockOnUnitChange).toHaveBeenCalledWith("Points");
	});

	it("displays the current estimation unit value", () => {
		const mockOnUnitChange = vi.fn();
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={1}
				onEstimationFieldChange={mockOnChange}
				estimationUnit="Days"
				onEstimationUnitChange={mockOnUnitChange}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(screen.getByLabelText("Estimation Unit")).toHaveValue("Days");
	});

	it("renders the non-numeric estimation toggle", () => {
		const mockOnToggle = vi.fn();
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={1}
				onEstimationFieldChange={mockOnChange}
				useNonNumericEstimation={false}
				onUseNonNumericEstimationChange={mockOnToggle}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(
			screen.getByLabelText("Use Non-Numeric Estimation"),
		).toBeInTheDocument();
	});

	it("calls onUseNonNumericEstimationChange when toggle is clicked", () => {
		const mockOnToggle = vi.fn();
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={1}
				onEstimationFieldChange={mockOnChange}
				useNonNumericEstimation={false}
				onUseNonNumericEstimationChange={mockOnToggle}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		fireEvent.click(screen.getByLabelText("Use Non-Numeric Estimation"));

		expect(mockOnToggle).toHaveBeenCalledWith(true);
	});

	it("shows category values list when non-numeric estimation is enabled", () => {
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={1}
				onEstimationFieldChange={mockOnChange}
				useNonNumericEstimation={true}
				onUseNonNumericEstimationChange={vi.fn()}
				estimationCategoryValues={["XS", "S", "M", "L", "XL"]}
				onAddCategoryValue={vi.fn()}
				onRemoveCategoryValue={vi.fn()}
				onReorderCategoryValues={vi.fn()}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(screen.getByText("Estimation Categories")).toBeInTheDocument();
		expect(screen.getByText("XS")).toBeInTheDocument();
		expect(screen.getByText("XL")).toBeInTheDocument();
	});

	it("does not show category values list when non-numeric estimation is disabled", () => {
		render(
			<EstimationFieldComponent
				estimationFieldDefinitionId={1}
				onEstimationFieldChange={mockOnChange}
				useNonNumericEstimation={false}
				onUseNonNumericEstimationChange={vi.fn()}
				estimationCategoryValues={[]}
				onAddCategoryValue={vi.fn()}
				onRemoveCategoryValue={vi.fn()}
				onReorderCategoryValues={vi.fn()}
				additionalFieldDefinitions={mockAdditionalFields}
			/>,
		);

		fireEvent.click(screen.getByLabelText("toggle"));
		expect(screen.queryByText("Estimation Categories")).not.toBeInTheDocument();
	});
});
