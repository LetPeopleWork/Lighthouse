import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ArchiveConfirmationDialog from "./ArchiveConfirmationDialog";

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({ delivery: "Commitment", deliveries: "Commitments" })[key] ?? key,
	}),
}));

const renderDialog = (overrides?: {
	onConfirm?: () => void;
	onCancel?: () => void;
}) =>
	render(
		<ArchiveConfirmationDialog
			open={true}
			itemName="Phoenix Release"
			onConfirm={overrides?.onConfirm ?? vi.fn()}
			onCancel={overrides?.onCancel ?? vi.fn()}
		/>,
	);

describe("ArchiveConfirmationDialog", () => {
	it("names the Delivery it is about to retire", () => {
		renderDialog();

		expect(screen.getAllByText(/Phoenix Release/).length).toBeGreaterThan(0);
	});

	it("says the three things archiving does: it delists, it pins the numbers, it can be undone", () => {
		renderDialog();

		const body = document.body.textContent ?? "";

		expect(body).toMatch(/out of the active/i);
		expect(body).toMatch(/numbers it shows right now/i);
		expect(body).toMatch(/bring it back/i);
	});

	it("says archiving is not deleting, and that deleting stays available on it", () => {
		renderDialog();

		const body = document.body.textContent ?? "";

		expect(body).toMatch(/not the same as deleting/i);
		expect(body).toMatch(/can still be deleted/i);
	});

	// Archiving and deleting both remain, and delete still takes the written-down numbers with it.
	// A reader who leaves this dialog believing an archived Delivery is out of harm's way is being
	// set up to archive one instead of backing it up, so the promise this must never make is a
	// promise of safety.
	it.each([
		"safe",
		"protect",
		"secure",
		"permanent",
		"forever",
		"backup",
		"back up",
		"preserv",
		"cannot be lost",
		"cannot be deleted",
		"cannot be removed",
		"read-only",
	])("never promises the Delivery is %s", (forbidden) => {
		renderDialog();

		const body = (document.body.textContent ?? "").toLowerCase();

		expect(body).not.toContain(forbidden);
	});

	it("uses the tenant's word for a Delivery rather than the word Delivery", () => {
		renderDialog();

		const body = document.body.textContent ?? "";

		expect(body).toMatch(/Commitment/);
		expect(body).not.toMatch(/Delivery/);
	});

	it("does nothing until the reader chooses", async () => {
		const onConfirm = vi.fn();
		const onCancel = vi.fn();
		renderDialog({ onConfirm, onCancel });

		expect(onConfirm).not.toHaveBeenCalled();

		await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

		expect(onCancel).toHaveBeenCalledTimes(1);
		expect(onConfirm).not.toHaveBeenCalled();
	});

	it("archives when the reader confirms", async () => {
		const onConfirm = vi.fn();
		renderDialog({ onConfirm });

		await userEvent.click(screen.getByRole("button", { name: "Archive" }));

		expect(onConfirm).toHaveBeenCalledTimes(1);
	});
});
