import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeItemRow } from "../../../models/Metrics/CumulativeStateTimeItems";
import CumulativeStateTimeDrillDownDialog from "./CumulativeStateTimeDrillDownDialog";

function getMockItemRow(
	overrides?: Partial<ICumulativeStateTimeItemRow>,
): ICumulativeStateTimeItemRow {
	return {
		referenceId: "ITEM-1",
		parentReferenceId: null,
		title: "Implement drill-down",
		type: "User Story",
		state: "Review",
		daysContributed: 4,
		url: "https://example.com/work-item/1",
		...overrides,
	};
}

const items: ICumulativeStateTimeItemRow[] = [
	getMockItemRow({ referenceId: "LOW-1", daysContributed: 2, url: null }),
	getMockItemRow({
		referenceId: "HIGH-1",
		daysContributed: 12,
		url: "https://example.com/work-item/high",
	}),
	getMockItemRow({ referenceId: "MID-1", daysContributed: 7, url: null }),
];

function renderDialog(
	overrides?: Partial<
		React.ComponentProps<typeof CumulativeStateTimeDrillDownDialog>
	>,
) {
	const props: React.ComponentProps<typeof CumulativeStateTimeDrillDownDialog> =
		{
			open: true,
			state: "Review",
			items,
			onClose: vi.fn(),
			...overrides,
		};
	const utils = render(<CumulativeStateTimeDrillDownDialog {...props} />);
	return { ...utils, props };
}

function dataRowReferenceIdsInOrder(): string[] {
	const rows = screen.getAllByRole("row");
	return rows
		.map((row) => within(row).queryByText(/-\d+$/)?.textContent ?? null)
		.filter((text): text is string => text !== null);
}

describe("CumulativeStateTimeDrillDownDialog", () => {
	it("does not render the dialog when closed", () => {
		renderDialog({ open: false });

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("renders an ARIA dialog labelled by the contributing-state title when open", () => {
		renderDialog({ state: "Review" });

		const dialog = screen.getByRole("dialog");
		expect(dialog).toHaveAccessibleName("Items contributing to Review");
		expect(
			screen.getByText("Items contributing to Review"),
		).toBeInTheDocument();
	});

	it("lists every contributing item row with its days contributed", () => {
		renderDialog();

		expect(screen.getByText("HIGH-1")).toBeInTheDocument();
		expect(screen.getByText("MID-1")).toBeInTheDocument();
		expect(screen.getByText("LOW-1")).toBeInTheDocument();
		expect(screen.getByText("12")).toBeInTheDocument();
	});

	it("sorts rows by days contributed descending by default", () => {
		renderDialog();

		const referenceIds = dataRowReferenceIdsInOrder();
		expect(referenceIds).toEqual(["HIGH-1", "MID-1", "LOW-1"]);
	});

	it("exposes a sortable days-contributed column header sorted descending by default", () => {
		renderDialog();

		const daysHeader = screen
			.getByText("Days Contributed")
			.closest('[role="columnheader"]');

		expect(daysHeader).toHaveClass("MuiDataGrid-columnHeader--sortable");
		expect(daysHeader).toHaveAttribute("aria-sort", "descending");
	});

	it("renders the Work Item ID as a link when a url is present", () => {
		renderDialog();

		const link = screen.getByRole("link", { name: "HIGH-1" });
		expect(link).toHaveAttribute("href", "https://example.com/work-item/high");
	});

	it("activates the Work Item ID link on Enter", () => {
		renderDialog();

		const link = screen.getByRole("link", { name: "HIGH-1" });
		link.focus();
		expect(link).toHaveFocus();
		fireEvent.keyDown(link, { key: "Enter", code: "Enter" });

		expect(link).toHaveAttribute("href", "https://example.com/work-item/high");
	});

	it("renders the Work Item ID as plain text when no url is present", () => {
		renderDialog();

		expect(screen.queryByRole("link", { name: "MID-1" })).toBeNull();
		expect(screen.getByText("MID-1")).toBeInTheDocument();
	});

	it("closes when Escape is pressed", () => {
		const { props } = renderDialog();

		fireEvent.keyDown(screen.getByRole("dialog"), {
			key: "Escape",
			code: "Escape",
		});

		expect(props.onClose).toHaveBeenCalledTimes(1);
	});

	it("closes when the close control is clicked", () => {
		const { props } = renderDialog();

		fireEvent.click(screen.getByRole("button", { name: /close/i }));

		expect(props.onClose).toHaveBeenCalledTimes(1);
	});

	it("shows the empty message when no items contributed to the state", () => {
		renderDialog({ items: [] });

		expect(
			screen.getByText(
				"No items contributed to this state in the selected window.",
			),
		).toBeInTheDocument();
		expect(screen.queryByText("Days Contributed")).toBeNull();
	});
});
