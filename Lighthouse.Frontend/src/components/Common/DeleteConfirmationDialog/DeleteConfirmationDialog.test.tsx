import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DeleteConfirmationDialog from "./DeleteConfirmationDialog";

describe("DeleteConfirmationDialog", () => {
	const defaultProps = {
		open: true,
		itemName: "Test Item",
		onClose: vi.fn(),
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders dialog with correct title and content", () => {
		render(<DeleteConfirmationDialog {...defaultProps} />);

		expect(screen.getByText("Confirm Delete")).toBeInTheDocument();
		expect(
			screen.getByText(
				`Do you really want to delete ${defaultProps.itemName}?`,
			),
		).toBeInTheDocument();
	});

	it("shows cancel and delete buttons", () => {
		render(<DeleteConfirmationDialog {...defaultProps} />);

		expect(screen.getByText("Cancel")).toBeInTheDocument();
		expect(screen.getByText("Delete")).toBeInTheDocument();
	});

	it("calls onClose with false when Cancel button is clicked", async () => {
		const user = userEvent.setup();
		render(<DeleteConfirmationDialog {...defaultProps} />);

		const cancelButton = screen.getByText("Cancel");
		await user.click(cancelButton);

		expect(defaultProps.onClose).toHaveBeenCalledWith(false);
	});

	it("calls onClose with true when Delete button is clicked", async () => {
		const user = userEvent.setup();
		render(<DeleteConfirmationDialog {...defaultProps} />);

		const deleteButton = screen.getByText("Delete");
		await user.click(deleteButton);

		expect(defaultProps.onClose).toHaveBeenCalledWith(true);
	});

	it("calls onClose with false when dialog is closed via Escape key", async () => {
		const user = userEvent.setup();
		render(<DeleteConfirmationDialog {...defaultProps} />);

		// Find the dialog (just for test readability, not actually using the reference)
		screen.getByRole("dialog");
		// Simulate Escape key press
		await user.keyboard("{Escape}");

		expect(defaultProps.onClose).toHaveBeenCalledWith(false);
	});

	it("is not visible when open prop is false", () => {
		render(<DeleteConfirmationDialog {...defaultProps} open={false} />);

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("shows different item name when provided", () => {
		const itemName = "Custom Item";
		render(<DeleteConfirmationDialog {...defaultProps} itemName={itemName} />);

		expect(
			screen.getByText(`Do you really want to delete ${itemName}?`),
		).toBeInTheDocument();
	});
});
