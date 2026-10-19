import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IEstimationDefault } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import EstimationDefaultEditor from "./EstimationDefaultEditor";

const storyPoints: IAdditionalFieldDefinition = {
	id: 5,
	displayName: "Story Points",
	reference: "customfield_10016",
};

const tShirtSize: IAdditionalFieldDefinition = {
	id: 6,
	displayName: "T-Shirt Size",
	reference: "customfield_10017",
};

const sizes: IEstimationDefault = {
	additionalFieldDefinitionId: 6,
	unit: "Sizes",
	useNonNumericEstimation: true,
	estimationCategoryValues: ["S", "M", "L"],
};

const points: IEstimationDefault = {
	additionalFieldDefinitionId: 5,
	unit: "Points",
	useNonNumericEstimation: false,
	estimationCategoryValues: [],
};

const renderEditor = (bundle: IEstimationDefault | null) => {
	const onChange = vi.fn();
	render(
		<EstimationDefaultEditor
			bundle={bundle}
			chosenField={bundle?.additionalFieldDefinitionId ?? null}
			additionalFields={[storyPoints, tShirtSize]}
			onChange={onChange}
		/>,
	);
	return onChange;
};

const pick = (option: string) => {
	fireEvent.mouseDown(screen.getByRole("combobox"));
	fireEvent.click(within(screen.getByRole("listbox")).getByText(option));
};

describe("EstimationDefaultEditor", () => {
	// @story-6313 @contract-shape:bounded-change
	it("without a default shows only the field, and no unit or non-numeric switch", () => {
		renderEditor(null);

		expect(screen.getByRole("combobox")).toBeInTheDocument();
		expect(screen.queryByLabelText("Estimation Unit")).not.toBeInTheDocument();
		expect(
			screen.queryByLabelText("Use Non-Numeric Estimation"),
		).not.toBeInTheDocument();
	});

	// @story-6313 @contract-shape:bounded-change
	it("picking a field where there was no default starts a numeric bundle without unit or categories", () => {
		const onChange = renderEditor(null);

		pick("Story Points");

		expect(onChange).toHaveBeenCalledWith({
			additionalFieldDefinitionId: 5,
			unit: null,
			useNonNumericEstimation: false,
			estimationCategoryValues: [],
		});
	});

	// @story-6313 @contract-shape:bounded-change
	it("picking None clears the whole default", () => {
		const onChange = renderEditor(sizes);

		pick("None");

		expect(onChange).toHaveBeenCalledWith(null);
	});

	// @story-6313 @contract-shape:bounded-change
	it("a default without a unit shows the unit empty", () => {
		renderEditor({ ...points, unit: null });

		expect(screen.getByLabelText("Estimation Unit")).toHaveValue("");
	});

	// @story-6313 @contract-shape:bounded-change
	it("switching to non-numeric keeps the field and unit", () => {
		const onChange = renderEditor(points);

		fireEvent.click(screen.getByLabelText("Use Non-Numeric Estimation"));

		expect(onChange).toHaveBeenCalledWith({
			...points,
			useNonNumericEstimation: true,
		});
	});

	// @story-6313 @contract-shape:bounded-change
	it("adding a category puts it after the ones the default holds", () => {
		const onChange = renderEditor(sizes);

		const input = screen.getByLabelText("New Estimation Category");
		fireEvent.change(input, { target: { value: "XL" } });
		fireEvent.keyDown(input, { key: "Enter" });

		expect(onChange).toHaveBeenCalledWith({
			...sizes,
			estimationCategoryValues: ["S", "M", "L", "XL"],
		});
	});

	// @story-6313 @contract-shape:bounded-change
	it("removing a category keeps the others in their order", () => {
		const onChange = renderEditor(sizes);

		const medium = screen
			.getByText("M")
			.closest(".MuiChip-root") as HTMLElement;
		fireEvent.click(within(medium).getByTestId("CancelIcon"));

		expect(onChange).toHaveBeenCalledWith({
			...sizes,
			estimationCategoryValues: ["S", "L"],
		});
	});

	// @story-6313 @contract-shape:bounded-change
	it("dragging a category to another place reorders the default's categories", () => {
		const onChange = renderEditor(sizes);

		fireEvent.dragStart(screen.getByText("S"), {
			dataTransfer: { effectAllowed: "move", setData: vi.fn() },
		});
		fireEvent.dragOver(screen.getByText("L"));
		fireEvent.drop(screen.getByText("L"), {
			dataTransfer: { dropEffect: "move", getData: vi.fn() },
		});

		expect(onChange).toHaveBeenCalledWith({
			...sizes,
			estimationCategoryValues: ["M", "L", "S"],
		});
	});
});
