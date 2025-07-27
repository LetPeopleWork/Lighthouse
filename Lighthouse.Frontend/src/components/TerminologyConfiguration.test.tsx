import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../services/TerminologyContext";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { TerminologyConfiguration } from "./TerminologyConfiguration";

// Mock the terminology service
const mockTerminologyService = {
	getAllTerminology: vi.fn(),
	updateTerminology: vi.fn(),
};

// Mock the API service context
const mockApiServiceContext = createMockApiServiceContext({
	terminologyService: mockTerminologyService,
});

const renderWithProviders = (component: React.ReactElement) => {
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<TerminologyProvider>{component}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("TerminologyConfiguration", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should display loading state initially", () => {
		// Return a promise that doesn't resolve immediately
		mockTerminologyService.getAllTerminology.mockReturnValue(
			new Promise(() => {}), // Never resolves
		);

		renderWithProviders(<TerminologyConfiguration />);

		expect(
			screen.getByText(/loading terminology configuration/i),
		).toBeInTheDocument();
	});

	it("should load and display terminology from the service", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Task",
				description: "Individual work item",
				value: "Task",
			},
			{
				id: 2,
				key: "workItems",
				defaultValue: "Tasks",
				description: "Multiple work items",
				value: "Tasks",
			},
			{
				id: 3,
				key: "customTerm",
				defaultValue: "Custom Default",
				description: "Custom terminology",
				value: "Custom Value",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Task")).toBeInTheDocument();
			expect(screen.getByLabelText("Tasks")).toBeInTheDocument();
			expect(screen.getByLabelText("Custom Default")).toBeInTheDocument();
		});

		// Check that values are displayed correctly
		expect(screen.getByDisplayValue("Task")).toBeInTheDocument();
		expect(screen.getByDisplayValue("Tasks")).toBeInTheDocument();
		expect(screen.getByDisplayValue("Custom Value")).toBeInTheDocument();

		expect(mockTerminologyService.getAllTerminology).toHaveBeenCalled();
	});

	it("should allow editing terminology values", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const input = screen.getByLabelText("Work Item");
		fireEvent.change(input, { target: { value: "Task" } });

		expect(screen.getByDisplayValue("Task")).toBeInTheDocument();
	});

	it("should save terminology when save button is clicked", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);
		mockTerminologyService.updateTerminology.mockResolvedValue(undefined);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const input = screen.getByLabelText("Work Item");
		fireEvent.change(input, { target: { value: "Task" } });

		const saveButton = screen.getByRole("button", {
			name: /save configuration/i,
		});
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(mockTerminologyService.updateTerminology).toHaveBeenCalledWith([
				{
					id: 1,
					key: "workItem",
					defaultValue: "Work Item",
					description: "Individual work item",
					value: "Task",
				},
			]);
		});
	});

	it("should display success message after saving", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);
		mockTerminologyService.updateTerminology.mockResolvedValue(undefined);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const saveButton = screen.getByRole("button", {
			name: /save configuration/i,
		});
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(
				screen.getByText(/terminology configuration updated successfully/i),
			).toBeInTheDocument();
		});
	});

	it("should display error message when loading fails", async () => {
		mockTerminologyService.getAllTerminology.mockRejectedValue(
			new Error("Network error"),
		);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(
				screen.getByText(/failed to load terminology configuration/i),
			).toBeInTheDocument();
		});
	});

	it("should display error message when saving fails", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);
		mockTerminologyService.updateTerminology.mockRejectedValue(
			new Error("Network error"),
		);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const saveButton = screen.getByRole("button", {
			name: /save configuration/i,
		});
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(
				screen.getByText(/failed to save terminology configuration/i),
			).toBeInTheDocument();
		});
	});

	it("should call onClose when cancel button is clicked", async () => {
		const mockOnClose = vi.fn();
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);

		renderWithProviders(<TerminologyConfiguration onClose={mockOnClose} />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const cancelButton = screen.getByRole("button", { name: /cancel/i });
		fireEvent.click(cancelButton);

		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});
});
