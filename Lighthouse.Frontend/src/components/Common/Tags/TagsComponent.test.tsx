import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISuggestionService } from "../../../services/Api/SuggestionService";
import {
	createMockApiServiceContext,
	createMockSuggestionService,
} from "../../../tests/MockApiServiceProvider";
import TagsComponent from "./TagsComponent";

// Mock ItemListManager component
vi.mock("../ItemListManager/ItemListManager", () => ({
	__esModule: true,
	default: ({
		title,
		items,
		onAddItem,
		onRemoveItem,
		suggestions,
		isLoading,
	}: {
		title: string;
		items: string[];
		onAddItem: (item: string) => void;
		onRemoveItem: (item: string) => void;
		suggestions?: string[];
		isLoading?: boolean;
	}) => (
		<div data-testid="item-list-manager">
			<div>ItemListManager Component</div>
			<div data-testid="title">{title}</div>
			<div data-testid="items">{items.join(", ")}</div>
			<div data-testid="suggestions">{(suggestions ?? []).join(", ")}</div>
			<div data-testid="is-loading">{isLoading ? "true" : "false"}</div>
			<button type="button" onClick={() => onAddItem("New Tag")}>
				Add Item
			</button>
			<button type="button" onClick={() => onRemoveItem(items[0])}>
				Remove Item
			</button>
		</div>
	),
}));

describe("TagsComponent", () => {
	// Correctly type the mock tag service to support mockResolvedValue
	const mockGetTags = vi.fn();
	const mockSuggestionService: ISuggestionService =
		createMockSuggestionService();

	// Connect the mock function to the service
	mockSuggestionService.getTags = mockGetTags;

	const mockApiContext: IApiServiceContext = createMockApiServiceContext({
		suggestionService: mockSuggestionService,
	});

	const mockOnAddTag = vi.fn();
	const mockOnRemoveTag = vi.fn();
	const mockTags = ["Tag1", "Tag2"];
	const mockSuggestions = ["Tag1", "Tag2", "Tag3", "Tag4"];

	beforeEach(() => {
		vi.clearAllMocks();
		mockGetTags.mockResolvedValue(mockSuggestions);
	});

	it("renders the component with correct title", () => {
		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<TagsComponent
					tags={mockTags}
					onAddTag={mockOnAddTag}
					onRemoveTag={mockOnRemoveTag}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.getByText("ItemListManager Component")).toBeInTheDocument();
		expect(screen.getByTestId("title").textContent).toBe("Tag");
	});

	it("passes correct props to ItemListManager", () => {
		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<TagsComponent
					tags={mockTags}
					onAddTag={mockOnAddTag}
					onRemoveTag={mockOnRemoveTag}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(screen.getByTestId("items").textContent).toBe(mockTags.join(", "));
		// Initially isLoading should be true while fetching suggestions
		expect(screen.getByTestId("is-loading").textContent).toBe("true");
	});

	it("fetches tags on mount and updates suggestions", async () => {
		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<TagsComponent
					tags={mockTags}
					onAddTag={mockOnAddTag}
					onRemoveTag={mockOnRemoveTag}
				/>
			</ApiServiceContext.Provider>,
		);

		expect(mockGetTags).toHaveBeenCalledTimes(1);

		// Wait for suggestions to be loaded
		await waitFor(() => {
			expect(screen.getByTestId("is-loading").textContent).toBe("false");
		});

		// Check if suggestions were passed to ItemListManager
		await waitFor(() => {
			expect(screen.getByTestId("suggestions").textContent).toBe(
				mockSuggestions.join(", "),
			);
		});
	});

	it("handles add tag action", async () => {
		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<TagsComponent
					tags={mockTags}
					onAddTag={mockOnAddTag}
					onRemoveTag={mockOnRemoveTag}
				/>
			</ApiServiceContext.Provider>,
		);

		// Click the add button provided by our mock
		screen.getByText("Add Item").click();
		expect(mockOnAddTag).toHaveBeenCalledWith("New Tag");
	});

	it("handles remove tag action", async () => {
		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<TagsComponent
					tags={mockTags}
					onAddTag={mockOnAddTag}
					onRemoveTag={mockOnRemoveTag}
				/>
			</ApiServiceContext.Provider>,
		);

		// Click the remove button provided by our mock
		screen.getByText("Remove Item").click();
		expect(mockOnRemoveTag).toHaveBeenCalledWith(mockTags[0]);
	});

	it("handles error when fetching tags", async () => {
		// Mock console.error to prevent error messages in test output
		const consoleErrorSpy = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});

		// Make the tag service throw an error
		mockGetTags.mockRejectedValueOnce(new Error("Failed to fetch tags"));

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<TagsComponent
					tags={mockTags}
					onAddTag={mockOnAddTag}
					onRemoveTag={mockOnRemoveTag}
				/>
			</ApiServiceContext.Provider>,
		);

		// Wait for the loading state to be updated after the error
		await waitFor(() => {
			expect(screen.getByTestId("is-loading").textContent).toBe("false");
		});

		// Verify error was logged
		expect(consoleErrorSpy).toHaveBeenCalledWith(
			"Failed to fetch tags:",
			expect.any(Error),
		);

		// Restore console.error
		consoleErrorSpy.mockRestore();
	});
});
