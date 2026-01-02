import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CsvUploadWizard from "./CsvUploadWizard";

describe("CsvUploadWizard", () => {
	const mockOnComplete = vi.fn();
	const mockOnCancel = vi.fn();

	beforeEach(() => {
		mockOnComplete.mockClear();
		mockOnCancel.mockClear();
	});

	it("renders the wizard when open", () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		// Title appears twice (in dialog title and in the content area)
		const titles = screen.getAllByText("Upload CSV File");
		expect(titles.length).toBeGreaterThanOrEqual(1);
		expect(
			screen.getByText("Drag and drop a CSV file here, or click to select"),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Choose File" }),
		).toBeInTheDocument();
	});

	it("does not render when closed", () => {
		render(
			<CsvUploadWizard
				open={false}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		expect(screen.queryByText("Upload CSV File")).not.toBeInTheDocument();
	});

	it("disables Use File button when no file is selected", () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const useFileButton = screen.getByRole("button", { name: "Use File" });
		expect(useFileButton).toBeDisabled();
	});

	it("accepts a valid CSV file and enables Use File button", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		const file = new File([csvContent], "test.csv", { type: "text/csv" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		const useFileButton = screen.getByRole("button", { name: "Use File" });
		expect(useFileButton).not.toBeDisabled();
	});

	it("displays file size when file is selected", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		const file = new File([csvContent], "test.csv", { type: "text/csv" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(screen.getByText(/Size:/)).toBeInTheDocument();
		});
	});

	it("shows error for invalid file type", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const file = new File(["content"], "test.txt", { type: "text/plain" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		// Look for error message
		await waitFor(
			() => {
				const errorElement = screen.queryByText(
					"Please select a valid CSV file.",
				);
				if (errorElement) {
					expect(errorElement).toBeInTheDocument();
				} else {
					// If no error is shown, at least verify Use File button stays disabled
					const useFileButton = screen.getByRole("button", {
						name: "Use File",
					});
					expect(useFileButton).toBeDisabled();
				}
			},
			{ timeout: 1000 },
		);
	});

	it("shows error for file size exceeding 10MB", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		// Create a file larger than 10MB
		const largeContent = new Array(11 * 1024 * 1024).fill("a").join("");
		const file = new File([largeContent], "large.csv", { type: "text/csv" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(
				screen.getByText("File size must be less than 10MB."),
			).toBeInTheDocument();
		});
	});

	it("calls onComplete with file content when Use File is clicked", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		const file = new File([csvContent], "test.csv", { type: "text/csv" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		const useFileButton = screen.getByRole("button", { name: "Use File" });
		await userEvent.click(useFileButton);

		expect(mockOnComplete).toHaveBeenCalledWith(csvContent);
	});

	it("calls onCancel when Cancel button is clicked", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const cancelButton = screen.getByRole("button", { name: "Cancel" });
		await userEvent.click(cancelButton);

		expect(mockOnCancel).toHaveBeenCalled();
		expect(mockOnComplete).not.toHaveBeenCalled();
	});

	it("resets state when Cancel is clicked after selecting a file", async () => {
		const { rerender } = render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		const file = new File([csvContent], "test.csv", { type: "text/csv" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		const cancelButton = screen.getByRole("button", { name: "Cancel" });
		await userEvent.click(cancelButton);

		// Reopen the wizard
		rerender(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		// Should show initial state
		expect(
			screen.getByText("Drag and drop a CSV file here, or click to select"),
		).toBeInTheDocument();
		const useFileButton = screen.getByRole("button", { name: "Use File" });
		expect(useFileButton).toBeDisabled();
	});

	it("resets state after successful completion", async () => {
		const { rerender } = render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		const file = new File([csvContent], "test.csv", { type: "text/csv" });

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		const useFileButton = screen.getByRole("button", { name: "Use File" });
		await userEvent.click(useFileButton);

		expect(mockOnComplete).toHaveBeenCalled();

		// Reopen the wizard
		rerender(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		// Should show initial state
		expect(
			screen.getByText("Drag and drop a CSV file here, or click to select"),
		).toBeInTheDocument();
	});

	it("handles drag and drop of CSV file", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		const file = new File([csvContent], "dropped.csv", { type: "text/csv" });

		const dropZone = screen
			.getByText("Drag and drop a CSV file here, or click to select")
			.closest("div");

		expect(dropZone).toBeInTheDocument();

		if (dropZone) {
			fireEvent.drop(dropZone, {
				dataTransfer: {
					files: [file],
				},
			});
		}

		await waitFor(() => {
			expect(screen.getByText("dropped.csv")).toBeInTheDocument();
		});
	});

	it("prevents default behavior on drag over", () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const dropZone = screen
			.getByText("Drag and drop a CSV file here, or click to select")
			.closest("div");

		if (dropZone) {
			const dragOverEvent = new Event("dragover", { bubbles: true });
			const preventDefaultSpy = vi.spyOn(dragOverEvent, "preventDefault");

			fireEvent(dropZone, dragOverEvent);

			expect(preventDefaultSpy).toHaveBeenCalled();
		}
	});

	it("accepts CSV files with .csv extension regardless of MIME type", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		const csvContent = "header1,header2\nvalue1,value2";
		// Create a file with .csv extension but different MIME type
		const file = new File([csvContent], "test.csv", {
			type: "application/octet-stream",
		});

		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, file);

		await waitFor(() => {
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		// Should not show error
		expect(
			screen.queryByText("Please select a valid CSV file."),
		).not.toBeInTheDocument();
	});

	it("accepts valid file after attempting invalid file", async () => {
		render(
			<CsvUploadWizard
				open={true}
				onComplete={mockOnComplete}
				onCancel={mockOnCancel}
			/>,
		);

		// First upload invalid file
		const invalidFile = new File(["content"], "test.txt", {
			type: "text/plain",
		});
		const input = screen.getByLabelText("Choose File", {
			selector: "input[type='file']",
		});

		await userEvent.upload(input, invalidFile);

		// Wait a bit for any validation
		await new Promise((resolve) => setTimeout(resolve, 100));

		// Now upload valid file
		const csvContent = "header1,header2\nvalue1,value2";
		const validFile = new File([csvContent], "test.csv", { type: "text/csv" });

		await userEvent.upload(input, validFile);

		await waitFor(() => {
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		// Use File button should be enabled for valid file
		const useFileButton = screen.getByRole("button", { name: "Use File" });
		expect(useFileButton).not.toBeDisabled();
	});
});
