import { render, screen, waitFor, within } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliveryNote } from "../../../../../models/Delivery/DeliveryNote";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../../../tests/MockApiServiceProvider";
import DeliveryNotesPanel from "./DeliveryNotesPanel";

const note = (overrides: Partial<IDeliveryNote> = {}): IDeliveryNote => ({
	id: 1,
	deliveryId: 42,
	text: "Two Features added after the steering review",
	createdAt: "2026-08-21T10:00:00Z",
	createdOn: "2026-08-21",
	lastEditedAt: null,
	lastEditedOn: null,
	authorDisplayName: "Anoop Kumar",
	...overrides,
});

const mockDeliveryService = createMockDeliveryService();

const renderPanel = (canWrite = true) => {
	const context = createMockApiServiceContext({
		deliveryService: mockDeliveryService,
	});

	return render(
		<ApiServiceContext.Provider value={context}>
			<DeliveryNotesPanel deliveryId={42} canWrite={canWrite} />
		</ApiServiceContext.Provider>,
	);
};

describe("DeliveryNotesPanel", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([]);
	});

	it("lists the notes a Delivery already has", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([
			note({ id: 2, text: "newer" }),
			note({ id: 1, text: "older" }),
		]);

		renderPanel();

		await waitFor(() =>
			expect(screen.getAllByTestId("delivery-note")).toHaveLength(2),
		);
		const rendered = screen.getAllByTestId("delivery-note");
		expect(within(rendered[0]).getByText("newer")).toBeInTheDocument();
		expect(within(rendered[1]).getByText("older")).toBeInTheDocument();
	});

	it("shows the day a note was written and who wrote it", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([note()]);

		renderPanel();

		await screen.findByTestId("delivery-note");
		expect(screen.getByText(/2026-08-21/)).toBeInTheDocument();
		expect(screen.getByText(/Anoop Kumar/)).toBeInTheDocument();
	});

	it("shows a note nobody signed without an author and without a placeholder", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([
			note({ authorDisplayName: null }),
		]);

		renderPanel();

		await screen.findByTestId("delivery-note");
		const rendered = screen.getByTestId("delivery-note");
		expect(within(rendered).getByText("2026-08-21")).toBeInTheDocument();
		expect(rendered.textContent).not.toMatch(/unknown|anonymous|—|by\s*$/i);
	});

	it("writes a note and puts it at the top without a reload", async () => {
		vi.mocked(mockDeliveryService.addNote).mockResolvedValue(
			note({ id: 9, text: "Vendor slipped a week" }),
		);

		renderPanel();

		await userEvent.type(
			await screen.findByTestId("note-input"),
			"Vendor slipped a week",
		);
		await userEvent.click(screen.getByTestId("save-note-button"));

		await waitFor(() =>
			expect(mockDeliveryService.addNote).toHaveBeenCalledWith(
				42,
				"Vendor slipped a week",
			),
		);
		expect(
			await screen.findByText("Vendor slipped a week"),
		).toBeInTheDocument();
	});

	it("refuses a note of nothing but blank space, and stores nothing", async () => {
		renderPanel();

		await userEvent.type(await screen.findByTestId("note-input"), "   ");
		await userEvent.click(screen.getByTestId("save-note-button"));

		expect(
			await screen.findByText("A note needs some text."),
		).toBeInTheDocument();
		expect(mockDeliveryService.addNote).not.toHaveBeenCalled();
	});

	it("shows a reader the notes but no way to add one", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([note()]);

		renderPanel(false);

		await screen.findByTestId("delivery-note");
		expect(screen.queryByTestId("note-input")).not.toBeInTheDocument();
		expect(screen.queryByTestId("save-note-button")).not.toBeInTheDocument();
	});

	it("shows what looks like markup as the characters somebody typed", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([
			note({ text: "<b>not bold</b>" }),
		]);

		renderPanel();

		const rendered = await screen.findByTestId("delivery-note");
		expect(within(rendered).getByText("<b>not bold</b>")).toBeInTheDocument();
		expect(rendered.querySelector("b")).toBeNull();
	});

	it("says so plainly when a Delivery has no notes yet", async () => {
		renderPanel();

		expect(await screen.findByText("No notes yet.")).toBeInTheDocument();
	});
});
