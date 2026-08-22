import { render, screen, waitFor } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliveryNote } from "../../../../../models/Delivery/DeliveryNote";
import { ApiError } from "../../../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../../../tests/MockApiServiceProvider";
import { DELIVERY_ARCHIVED_CODE } from "../../../../../utils/deliveries/deliveryArchivedRefusal";
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
	canModify: true,
	...overrides,
});

const mockDeliveryService = createMockDeliveryService();

const renderPanel = (props?: { isReadOnly?: boolean; canWrite?: boolean }) => {
	const context = createMockApiServiceContext({
		deliveryService: mockDeliveryService,
	});

	return render(
		<ApiServiceContext.Provider value={context}>
			<DeliveryNotesPanel
				deliveryId={42}
				canWrite={props?.canWrite ?? true}
				isReadOnly={props?.isReadOnly ?? false}
			/>
		</ApiServiceContext.Provider>,
	);
};

const archivedRefusal = () =>
	new ApiError(
		409,
		"Request failed with status code 409",
		undefined,
		undefined,
		DELIVERY_ARCHIVED_CODE,
	);

describe("DeliveryNotesPanel on an archived Delivery", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([]);
	});

	it("still lists the notes that were written before it closed", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([
			note({ id: 2, text: "newer" }),
			note({ id: 1, text: "older" }),
		]);

		renderPanel({ isReadOnly: true });

		await waitFor(() =>
			expect(screen.getAllByTestId("delivery-note")).toHaveLength(2),
		);
	});

	it("offers nowhere to type a new one", async () => {
		renderPanel({ isReadOnly: true });

		await waitFor(() =>
			expect(screen.getByTestId("delivery-notes-panel")).toBeInTheDocument(),
		);
		expect(screen.queryByTestId("note-input")).not.toBeInTheDocument();
		expect(screen.queryByTestId("save-note-button")).not.toBeInTheDocument();
	});

	it("offers no way to correct or withdraw a note, even to the person who wrote it", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([note()]);

		renderPanel({ isReadOnly: true });

		await screen.findByTestId("delivery-note");
		expect(screen.queryByTestId("edit-note-button")).not.toBeInTheDocument();
		expect(screen.queryByTestId("delete-note-button")).not.toBeInTheDocument();
	});

	it("says the Delivery is archived when the server refuses a new note for that reason", async () => {
		vi.mocked(mockDeliveryService.addNote).mockRejectedValue(archivedRefusal());

		renderPanel();

		await userEvent.type(await screen.findByTestId("note-input"), "late note");
		await userEvent.click(screen.getByTestId("save-note-button"));

		expect(
			await screen.findByText(/Delivery is archived/i),
		).toBeInTheDocument();
	});

	it("says the Delivery is archived when the server refuses a correction for that reason", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([note()]);
		vi.mocked(mockDeliveryService.updateNote).mockRejectedValue(
			archivedRefusal(),
		);

		renderPanel();

		await screen.findByTestId("delivery-note");
		await userEvent.click(screen.getByTestId("edit-note-button"));
		await userEvent.type(screen.getByTestId("edit-note-input"), " corrected");
		await userEvent.click(screen.getByTestId("save-edit-button"));

		expect(
			await screen.findByText(/Delivery is archived/i),
		).toBeInTheDocument();
	});

	it("says the Delivery is archived when the server refuses a withdrawal for that reason", async () => {
		vi.mocked(mockDeliveryService.getNotes).mockResolvedValue([note()]);
		vi.mocked(mockDeliveryService.deleteNote).mockRejectedValue(
			archivedRefusal(),
		);

		renderPanel();

		await screen.findByTestId("delivery-note");
		await userEvent.click(screen.getByTestId("delete-note-button"));

		expect(
			await screen.findByText(/Delivery is archived/i),
		).toBeInTheDocument();
	});

	it("keeps the ordinary failure wording for a refusal that has nothing to do with archiving", async () => {
		vi.mocked(mockDeliveryService.addNote).mockRejectedValue(
			new ApiError(500, "boom"),
		);

		renderPanel();

		await userEvent.type(await screen.findByTestId("note-input"), "late note");
		await userEvent.click(screen.getByTestId("save-note-button"));

		expect(
			await screen.findByText("The note could not be saved."),
		).toBeInTheDocument();
	});
});
