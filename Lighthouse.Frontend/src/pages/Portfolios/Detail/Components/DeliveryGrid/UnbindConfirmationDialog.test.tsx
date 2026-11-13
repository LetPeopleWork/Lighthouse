import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import UnbindConfirmationDialog from "./UnbindConfirmationDialog";

function dialog(open: boolean, onConfirm = vi.fn(), onCancel = vi.fn()) {
	return (
		<UnbindConfirmationDialog
			open={open}
			sourceLabel="Jira filter"
			deliveryName="Q3 Launch"
			featuresTerm="Features"
			onConfirm={onConfirm}
			onCancel={onCancel}
		/>
	);
}

const confirmButton = () =>
	screen.getByText("Stop following").closest("button") as HTMLButtonElement;

describe("UnbindConfirmationDialog", () => {
	it("says what the Delivery keeps and what it stops taking, and offers both ways out", () => {
		const onConfirm = vi.fn();
		const onCancel = vi.fn();
		render(dialog(true, onConfirm, onCancel));

		expect(
			screen.getByRole("dialog", { name: "Stop following the Jira filter?" }),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				'"Q3 Launch" keeps the name, the date and the Features it has right now, and from then on they are yours to edit. It stops taking them from the Jira filter.',
			),
		).toBeInTheDocument();
		expect(confirmButton()).toBeEnabled();

		fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
		expect(onCancel).toHaveBeenCalledTimes(1);
		expect(onConfirm).not.toHaveBeenCalled();
	});

	it("confirms once however often the button is pressed while the dialog fades out", () => {
		const onConfirm = vi.fn();
		render(dialog(true, onConfirm));

		fireEvent.click(confirmButton());
		fireEvent.click(confirmButton());

		expect(onConfirm).toHaveBeenCalledTimes(1);
		expect(confirmButton()).toBeDisabled();
	});

	it("stays disabled while the dialog closes and is enabled again when it is opened afresh", () => {
		const onConfirm = vi.fn();
		const { rerender } = render(dialog(true, onConfirm));
		fireEvent.click(confirmButton());

		rerender(dialog(false, onConfirm));
		expect(confirmButton()).toBeDisabled();

		rerender(dialog(true, onConfirm));
		expect(confirmButton()).toBeEnabled();
	});
});
