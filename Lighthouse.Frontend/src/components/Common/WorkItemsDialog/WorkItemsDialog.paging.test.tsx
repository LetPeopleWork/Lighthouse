import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	aWorkItem,
	openTheDialog,
	rowOrder,
} from "../../../tests/WorkItemsDialogTestKit";

/**
 * A chart's Work Items dialog is a list like any other: behind a point with 243 closed Work Items it
 * pages, and its column filter is how a coach narrows it, since the dialog has no search of its own.
 */

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({ getTerm: (key: string) => key }),
}));

const RENDER_HEAVY = 20000;

const reference = (position: number) =>
	`ST-${String(position).padStart(3, "0")}`;

/** 243 closed Work Items; the first 64 were Resolved, the rest Closed. */
const twoHundredFortyThreeClosed = () =>
	Array.from({ length: 243 }, (_, index) =>
		aWorkItem({
			referenceId: reference(index + 1),
			state: index < 64 ? "Resolved" : "Closed",
		}),
	);

const sorted = (references: string[]) =>
	[...references].sort((a, b) => a.localeCompare(b));

beforeEach(() => {
	localStorage.clear();
});

describe("A Work Items dialog with more than 100 Work Items", () => {
	it(
		"Priya, on the second page of 243, filters to 64 and sees all of them with no footer",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "closedItems",
				items: twoHundredFortyThreeClosed(),
			});
			expect(await screen.findByText("1–100 of 243")).toBeInTheDocument();

			await user.click(screen.getByRole("button", { name: "Go to next page" }));
			await screen.findByText("101–200 of 243");

			await user.click(screen.getByLabelText("State column menu"));
			await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
			await user.type(
				await screen.findByRole("textbox", { name: "Value" }),
				"Resolved",
			);

			await waitFor(() =>
				expect(sorted(rowOrder())).toEqual(
					Array.from({ length: 64 }, (_, index) => reference(index + 1)),
				),
			);
			expect(screen.queryByText(/^\d+–\d+ of \d+$/)).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: "Go to next page" }),
			).not.toBeInTheDocument();
		},
		RENDER_HEAVY,
	);
});
