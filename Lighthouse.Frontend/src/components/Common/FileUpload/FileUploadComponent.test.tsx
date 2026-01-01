import { createTheme, ThemeProvider } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import FileUploadComponent from "./FileUploadComponent";

// Mock the terminology service
const mockTerminologyService = {
	getAllTerminology: vi.fn(),
	updateTerminology: vi.fn(),
};

// Mock the API service context
const mockApiServiceContext = createMockApiServiceContext({
	terminologyService: mockTerminologyService,
});

const renderWithProviders = (
	component: React.ReactElement,
	theme = createTheme(),
) => {
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<ThemeProvider theme={theme}>
					<TerminologyProvider>{component}</TerminologyProvider>
				</ThemeProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("FileUploadComponent", () => {
	const mockOnFileSelect = vi.fn();

	const mockFileConnection: IWorkTrackingSystemConnection = {
		id: 1,
		name: "Test File Connection",
		workTrackingSystem: "Csv",
		options: [],
		dataSourceType: "File",
		authenticationMethodKey: "none",
	};

	const mockQueryConnection: IWorkTrackingSystemConnection = {
		id: 2,
		name: "Test Query Connection",
		workTrackingSystem: "Jira",
		options: [],
		dataSourceType: "Query",
		authenticationMethodKey: "jira.cloud",
	};

	beforeEach(() => {
		vi.clearAllMocks();
		mockTerminologyService.getAllTerminology.mockResolvedValue([
			{ key: "workItem", value: "Work Item", defaultValue: "Work Item" },
			{ key: "workItems", value: "Work Items", defaultValue: "Work Items" },
		]);

		// Mock globalThis.alert
		vi.spyOn(globalThis, "alert").mockImplementation(() => {});
	});

	describe("Rendering Tests", () => {
		it("should render correctly when workTrackingSystemConnection is null", () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={null}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			// Component should not render anything when connection is null
			expect(screen.queryByText("Upload CSV File")).not.toBeInTheDocument();
		});

		it("should render correctly when workTrackingSystemConnection.dataSourceType is not File", () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockQueryConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			// Component should not render anything when dataSourceType is not "File"
			expect(screen.queryByText("Upload CSV File")).not.toBeInTheDocument();
		});

		it("should render correctly when workTrackingSystemConnection.dataSourceType is File", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			// Wait for terminology to load and component to render
			expect(await screen.findByText("Upload CSV File")).toBeInTheDocument();
			expect(
				screen.getByText("Drag and drop a CSV file here, or click to select"),
			).toBeInTheDocument();
			expect(screen.getByText("Choose File")).toBeInTheDocument();
		});

		it("should render with correct terminology (work items term)", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			// Wait for terminology to load
			expect(
				await screen.findByText("Work Items Data File"),
			).toBeInTheDocument();
		});
	});

	describe("File Selection Tests", () => {
		// Helper function to create test files
		const createTestFile = (name: string, size: number, type = "text/csv") => {
			const file = new File(["test content"], name, { type });
			Object.defineProperty(file, "size", { value: size });
			return file;
		};

		it("should handle file selection through input change", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const testFile = createTestFile("test.csv", 1000);

			// Simulate file selection
			Object.defineProperty(fileInput, "files", {
				value: [testFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(mockOnFileSelect).toHaveBeenCalledWith(testFile);
		});

		it("should validate CSV file type correctly", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const csvFile = createTestFile("test.csv", 1000, "text/csv");

			Object.defineProperty(fileInput, "files", {
				value: [csvFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(mockOnFileSelect).toHaveBeenCalledWith(csvFile);
		});

		it("should reject non-CSV files with appropriate error", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const txtFile = createTestFile("test.txt", 1000, "text/plain");

			Object.defineProperty(fileInput, "files", {
				value: [txtFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(globalThis.alert).toHaveBeenCalledWith(
				"Please select a valid CSV file.",
			);
			expect(mockOnFileSelect).not.toHaveBeenCalled();
		});

		it("should validate file size (reject files > 10MB)", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const largeFile = createTestFile("large.csv", 11 * 1024 * 1024); // 11MB

			Object.defineProperty(fileInput, "files", {
				value: [largeFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(globalThis.alert).toHaveBeenCalledWith(
				"File size must be less than 10MB.",
			);
			expect(mockOnFileSelect).not.toHaveBeenCalled();
		});

		it("should accept valid CSV files", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const validFile = createTestFile("valid.csv", 1000);

			Object.defineProperty(fileInput, "files", {
				value: [validFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(mockOnFileSelect).toHaveBeenCalledWith(validFile);
		});

		it("should clear file when null is passed", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");

			Object.defineProperty(fileInput, "files", {
				value: [],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(mockOnFileSelect).toHaveBeenCalledWith(null);
		});
	});

	describe("Drag and Drop Tests", () => {
		// Helper function to create test files
		const createTestFile = (name: string, size: number, type = "text/csv") => {
			const file = new File(["test content"], name, { type });
			Object.defineProperty(file, "size", { value: size });
			return file;
		};

		// Helper function to create drop event
		const createDropEvent = (files: File[]) => {
			const dropEvent = new Event("drop", { bubbles: true });
			Object.defineProperty(dropEvent, "dataTransfer", {
				value: {
					files: files,
				},
				writable: false,
			});
			return dropEvent;
		};

		// Helper function to create dragover event
		const createDragOverEvent = () => {
			return new Event("dragover", { bubbles: true });
		};

		it("should handle drag over event", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const dropZone = await screen
				.findByText("Upload CSV File")
				.then((el) => el.closest("div"));
			expect(dropZone).toBeInTheDocument();

			const dragOverEvent = createDragOverEvent();
			const preventDefaultSpy = vi.spyOn(dragOverEvent, "preventDefault");

			dropZone?.dispatchEvent(dragOverEvent);

			expect(preventDefaultSpy).toHaveBeenCalled();
		});

		it("should handle drop event with valid CSV file", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const dropZone = await screen
				.findByText("Upload CSV File")
				.then((el) => el.closest("div"));
			const testFile = createTestFile("test.csv", 1000);
			const dropEvent = createDropEvent([testFile]);
			const preventDefaultSpy = vi.spyOn(dropEvent, "preventDefault");

			dropZone?.dispatchEvent(dropEvent);

			expect(preventDefaultSpy).toHaveBeenCalled();
			expect(mockOnFileSelect).toHaveBeenCalledWith(testFile);
		});

		it("should handle drop event with invalid file type", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const dropZone = await screen
				.findByText("Upload CSV File")
				.then((el) => el.closest("div"));
			const txtFile = createTestFile("test.txt", 1000, "text/plain");
			const dropEvent = createDropEvent([txtFile]);

			dropZone?.dispatchEvent(dropEvent);

			expect(globalThis.alert).toHaveBeenCalledWith(
				"Please select a valid CSV file.",
			);
			expect(mockOnFileSelect).not.toHaveBeenCalled();
		});

		it("should handle drop event with oversized file", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const dropZone = await screen
				.findByText("Upload CSV File")
				.then((el) => el.closest("div"));
			const largeFile = createTestFile("large.csv", 11 * 1024 * 1024); // 11MB
			const dropEvent = createDropEvent([largeFile]);

			dropZone?.dispatchEvent(dropEvent);

			expect(globalThis.alert).toHaveBeenCalledWith(
				"File size must be less than 10MB.",
			);
			expect(mockOnFileSelect).not.toHaveBeenCalled();
		});
	});

	describe("File Information Display Tests", () => {
		const createTestFile = (name: string, size: number, type = "text/csv") => {
			const file = new File(["test content"], name, { type });
			Object.defineProperty(file, "size", { value: size });
			return file;
		};

		it("should display selected file information", async () => {
			const testFile = createTestFile("test-data.csv", 2048);

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={testFile}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Selected file:")).toBeInTheDocument();
			expect(screen.getByText("test-data.csv")).toBeInTheDocument();
		});

		it("should show file name and size correctly", async () => {
			const testFile = createTestFile("my-work-items.csv", 5120); // 5KB

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={testFile}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("my-work-items.csv")).toBeInTheDocument();
			expect(screen.getByText("Size: 5.0 KB")).toBeInTheDocument();
		});

		it("should format file size in KB", async () => {
			const testFile = createTestFile("data.csv", 1536); // 1.5KB

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={testFile}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Size: 1.5 KB")).toBeInTheDocument();
		});

		it("should not display file information when no file is selected", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.queryByText("Selected file:")).not.toBeInTheDocument();
		});
	});

	describe("Upload Progress Tests", () => {
		it("should display upload progress when uploadProgress is provided", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					uploadProgress={45}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Processing: 45%")).toBeInTheDocument();
		});

		it("should show progress bar with correct percentage", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					uploadProgress={75}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Processing: 75%")).toBeInTheDocument();

			// Check that a progress bar container exists (even if we can't easily test the exact width)
			const processingText = screen.getByText("Processing: 75%");
			const progressContainer = processingText.parentElement;
			expect(progressContainer).toBeInTheDocument();
		});

		it("should not display progress when uploadProgress is undefined", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					uploadProgress={undefined}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.queryByText(/Processing:/)).not.toBeInTheDocument();
		});

		it("should not display progress when uploadProgress is 0", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					uploadProgress={0}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.queryByText(/Processing:/)).not.toBeInTheDocument();
		});
	});

	describe("Validation Errors Tests", () => {
		it("should display validation errors when provided", async () => {
			const errors = [
				"Missing required column: ID",
				"Invalid date format in row 5",
			];

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					validationErrors={errors}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Validation Errors:")).toBeInTheDocument();
			expect(
				screen.getByText("• Missing required column: ID"),
			).toBeInTheDocument();
			expect(
				screen.getByText("• Invalid date format in row 5"),
			).toBeInTheDocument();
		});

		it("should limit display to 10 errors maximum", async () => {
			const errors = Array.from({ length: 15 }, (_, i) => `Error ${i + 1}`);

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					validationErrors={errors}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Validation Errors:")).toBeInTheDocument();

			// Should show first 10 errors
			expect(screen.getByText("• Error 1")).toBeInTheDocument();
			expect(screen.getByText("• Error 10")).toBeInTheDocument();

			// Should not show 11th error
			expect(screen.queryByText("• Error 11")).not.toBeInTheDocument();
		});

		it("should show count of additional errors when more than 10 exist", async () => {
			const errors = Array.from({ length: 15 }, (_, i) => `Error ${i + 1}`);

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					validationErrors={errors}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("... and 5 more errors")).toBeInTheDocument();
		});

		it("should not display validation errors section when no errors", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					validationErrors={[]}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.queryByText("Validation Errors:")).not.toBeInTheDocument();
		});

		it("should not display validation errors section when validationErrors is undefined", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.queryByText("Validation Errors:")).not.toBeInTheDocument();
		});
	});

	describe("Theme Tests", () => {
		it("should apply correct styling in light theme", async () => {
			const lightTheme = createTheme({ palette: { mode: "light" } });

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
				lightTheme,
			);

			await screen.findByText("Upload CSV File");
			// Verify the component renders without errors in light theme
			expect(screen.getByText("Choose File")).toBeInTheDocument();
		});

		it("should apply correct styling in dark theme", async () => {
			const darkTheme = createTheme({ palette: { mode: "dark" } });

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
				darkTheme,
			);

			await screen.findByText("Upload CSV File");
			// Verify the component renders without errors in dark theme
			expect(screen.getByText("Choose File")).toBeInTheDocument();
		});

		it("should handle file information display in dark theme", async () => {
			const darkTheme = createTheme({ palette: { mode: "dark" } });
			const testFile = new File(["test"], "test.csv", { type: "text/csv" });
			Object.defineProperty(testFile, "size", { value: 1024 });

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={testFile}
					onFileSelect={mockOnFileSelect}
				/>,
				darkTheme,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Selected file:")).toBeInTheDocument();
			expect(screen.getByText("test.csv")).toBeInTheDocument();
		});

		it("should handle validation errors display in dark theme", async () => {
			const darkTheme = createTheme({ palette: { mode: "dark" } });
			const errors = ["Test error"];

			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
					validationErrors={errors}
				/>,
				darkTheme,
			);

			await screen.findByText("Upload CSV File");
			expect(screen.getByText("Validation Errors:")).toBeInTheDocument();
			expect(screen.getByText("• Test error")).toBeInTheDocument();
		});
	});

	describe("Callback Tests", () => {
		const createTestFile = (name: string, size: number, type = "text/csv") => {
			const file = new File(["test content"], name, { type });
			Object.defineProperty(file, "size", { value: size });
			return file;
		};

		it("should call onFileSelect with file when valid file is selected", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const testFile = createTestFile("valid.csv", 1000);

			Object.defineProperty(fileInput, "files", {
				value: [testFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(mockOnFileSelect).toHaveBeenCalledWith(testFile);
			expect(mockOnFileSelect).toHaveBeenCalledTimes(1);
		});

		it("should call onFileSelect with null when file is cleared", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");

			Object.defineProperty(fileInput, "files", {
				value: [],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(mockOnFileSelect).toHaveBeenCalledWith(null);
		});

		it("should not call onFileSelect when invalid file is selected", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const invalidFile = createTestFile("invalid.txt", 1000, "text/plain");

			Object.defineProperty(fileInput, "files", {
				value: [invalidFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(globalThis.alert).toHaveBeenCalledWith(
				"Please select a valid CSV file.",
			);
			expect(mockOnFileSelect).not.toHaveBeenCalled();
		});

		it("should not call onFileSelect when oversized file is selected", async () => {
			renderWithProviders(
				<FileUploadComponent
					workTrackingSystemConnection={mockFileConnection}
					selectedFile={null}
					onFileSelect={mockOnFileSelect}
				/>,
			);

			const fileInput = screen.getByLabelText("Choose File");
			const largeFile = createTestFile("large.csv", 11 * 1024 * 1024); // 11MB

			Object.defineProperty(fileInput, "files", {
				value: [largeFile],
				writable: false,
			});

			const changeEvent = new Event("change", { bubbles: true });
			Object.defineProperty(changeEvent, "target", {
				value: fileInput,
				writable: false,
			});

			fileInput.dispatchEvent(changeEvent);

			expect(globalThis.alert).toHaveBeenCalledWith(
				"File size must be less than 10MB.",
			);
			expect(mockOnFileSelect).not.toHaveBeenCalled();
		});
	});
});
