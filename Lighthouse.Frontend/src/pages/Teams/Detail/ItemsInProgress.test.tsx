import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import ItemsInProgress from "./ItemsInProgress";

describe("ItemsInProgress component", () => {
	const createMockWorkItem = (name: string): IWorkItem => ({
		id: Math.floor(Math.random() * 1000),
		workItemReference: Math.floor(Math.random() * 1000).toString(),
		url: "https://example.com/item1",
		name,
		startedDate: new Date("2023-01-15"),
		closedDate: new Date("2023-01-20"),
		state: "In Progress",
		type: "Task",
	});

	const mockItems: IWorkItem[] = [
		createMockWorkItem("Item 1"),
		createMockWorkItem("Item 2"),
	];

	it("should render with title and item count", () => {
		render(<ItemsInProgress title="Work Items" items={mockItems} />);

		expect(screen.getByText("Work Items")).toBeInTheDocument();
		expect(screen.getByText("2")).toBeInTheDocument();
	});

	it("should display goal chip when idealWip is provided", () => {
		render(
			<ItemsInProgress title="Work Items" items={mockItems} idealWip={3} />,
		);

		expect(screen.getByText("Goal: 3")).toBeInTheDocument();
	});

	it("should not display goal chip when idealWip is null", () => {
		render(<ItemsInProgress title="Work Items" items={mockItems} />);

		expect(screen.queryByText(/Goal:/)).not.toBeInTheDocument();
	});

	it("should apply success color to chip when count equals idealWip", () => {
		render(
            <ItemsInProgress title="Work Items" items={mockItems} idealWip={2} />
        );

		const chip = screen.getByText("Goal: 2");
		expect(chip).toBeInTheDocument();
		expect(chip.closest(".MuiChip-colorSuccess")).not.toBeNull();
	});

	it("should apply info color to chip when count is less than idealWip", () => {
		render(
			<ItemsInProgress title="Work Items" items={mockItems} idealWip={3} />,
		);

		const chip = screen.getByText("Goal: 3");
		expect(chip).toBeInTheDocument();
		expect(chip.closest(".MuiChip-colorInfo")).not.toBeNull();
	});

	it("should apply warning color to chip when count is greater than idealWip", () => {
		render(
			<ItemsInProgress title="Work Items" items={mockItems} idealWip={1} />,
		);

		const chip = screen.getByText("Goal: 1");
		expect(chip).toBeInTheDocument();
		expect(chip.closest(".MuiChip-colorWarning")).not.toBeNull();
	});

	it("should open dialog when card is clicked", () => {
		render(<ItemsInProgress title="Work Items" items={mockItems} />);

		const card = screen.getByText("Work Items").closest(".MuiCard-root");
		expect(card).toBeInTheDocument();

		if (card) {
			fireEvent.click(card);
		}

		expect(screen.getByRole("dialog")).toBeInTheDocument();
	});

	it("should display table with correct items in dialog", () => {
		render(<ItemsInProgress title="Work Items" items={mockItems} />);

		const card = screen.getByText("Work Items").closest(".MuiCard-root");
		if (card) {
			fireEvent.click(card);
		}

		expect(screen.getByText("Item 1")).toBeInTheDocument();
		expect(screen.getByText("Item 2")).toBeInTheDocument();

		// Check link is present for item with URL
		const link = screen.getByText("Item 1").closest("a");
		expect(link).toHaveAttribute("href", "https://example.com/item1");
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("should display message when no items are available", () => {
		render(<ItemsInProgress title="Work Items" items={[]} />);

		const card = screen.getByText("Work Items").closest(".MuiCard-root");
		if (card) {
			fireEvent.click(card);
		}

		expect(
			screen.getByText("No items currently in progress"),
		).toBeInTheDocument();
	});
});
