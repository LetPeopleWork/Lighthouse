import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ValidationActions from "./ValidationActions";

describe("ValidationActions", () => {
	const defaultProps = {
		onCancel: vi.fn(),
		onValidate: vi.fn(),
		onSave: vi.fn(),
		inputsValid: true,
		validationFailedMessage: "Validation Failed Message",
	};

	beforeEach(() => {
		vi.clearAllMocks();
		vi.useFakeTimers();
	});

	it("renders all buttons when validation is required", () => {
		render(<ValidationActions {...defaultProps} />);

		expect(screen.getByText("Validate")).toBeInTheDocument();
		expect(screen.getByText("Save")).toBeInTheDocument();
	});

	it("only renders save button when validation is not required", () => {
		render(<ValidationActions {...defaultProps} onValidate={undefined} />);

		expect(screen.queryByText("Validate")).not.toBeInTheDocument();
		expect(screen.getByText("Save")).toBeInTheDocument();
	});

	it("disables validate button when form is invalid", () => {
		render(<ValidationActions {...defaultProps} inputsValid={false} />);

		const validateButton = screen.getByText("Validate");
		expect(validateButton).toBeDisabled();
	});

	it("disables save button when validation state is not success and form is valid", () => {
		render(<ValidationActions {...defaultProps} />);

		const saveButton = screen.getByText("Save");
		expect(saveButton).toBeDisabled();
	});

	it("calls onSave when save button is clicked", () => {
		render(<ValidationActions {...defaultProps} onValidate={undefined} />);

		const saveButton = screen.getByText("Save");
		fireEvent.click(saveButton);

		expect(defaultProps.onSave).toHaveBeenCalled();
	});

	it("calls onValidate when validate button is clicked", () => {
		render(<ValidationActions {...defaultProps} />);

		const validateButton = screen.getByText("Validate");
		fireEvent.click(validateButton);

		expect(defaultProps.onValidate).toHaveBeenCalled();
	});

	it("updates validation state on form validity changes", () => {
		const { rerender } = render(<ValidationActions {...defaultProps} />);

		// Initially form is valid
		expect(screen.getByText("Validate")).not.toBeDisabled();

		// Make form invalid
		rerender(<ValidationActions {...defaultProps} inputsValid={false} />);
		expect(screen.getByText("Validate")).toBeDisabled();
	});

	it("disables save button when disableSave prop is true", () => {
		render(
			<ValidationActions
				{...defaultProps}
				onValidate={undefined}
				disableSave={true}
			/>,
		);

		const saveButton = screen.getByText("Save");
		expect(saveButton).toBeDisabled();
	});

	it("shows tooltip when saveTooltip prop is provided", () => {
		const tooltipText = "License restriction tooltip";
		render(
			<ValidationActions
				{...defaultProps}
				onValidate={undefined}
				saveTooltip={tooltipText}
			/>,
		);

		// Check that the tooltip is set as aria-label
		expect(screen.getByLabelText(tooltipText)).toBeInTheDocument();
	});

	it("prioritizes disableSave over validation state", () => {
		render(
			<ValidationActions
				{...defaultProps}
				onValidate={undefined}
				disableSave={true}
				inputsValid={true}
			/>,
		);

		const saveButton = screen.getByText("Save");
		expect(saveButton).toBeDisabled();
	});
});
