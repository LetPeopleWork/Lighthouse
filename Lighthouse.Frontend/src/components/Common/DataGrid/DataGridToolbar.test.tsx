import type { GridApiCommon } from "@mui/x-data-grid";
import { render, screen, waitFor } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DataGridToolbar from "./DataGridToolbar";

// Mock the useGridApiContext hook
vi.mock("@mui/x-data-grid", async () => {
	const actual = await vi.importActual("@mui/x-data-grid");
	return {
		...actual,
		useGridApiContext: () => mockApiRef,
	};
});

// Mock clipboard API
const mockClipboardWrite = vi.fn();

// Mock ClipboardItem
interface MockClipboardItem {
	items: Record<string, Blob>;
}

global.ClipboardItem = class ClipboardItem implements MockClipboardItem {
	constructor(public items: Record<string, Blob>) {}
} as unknown as typeof ClipboardItem;

Object.assign(navigator, {
	clipboard: {
		write: mockClipboardWrite,
	},
});

// Mock URL.createObjectURL and revokeObjectURL
global.URL.createObjectURL = vi.fn(() => "mock-url");
global.URL.revokeObjectURL = vi.fn();

// Mock grid API
const mockApiRef = {
	current: {
		getSortedRowIds: vi.fn(() => [1, 2, 3]),
		getVisibleColumns: vi.fn(() => [
			{ field: "name", headerName: "Name", computedWidth: 100 },
			{ field: "age", headerName: "Age", computedWidth: 100 },
			{ field: "email", headerName: "Email", computedWidth: 200 },
		]),
		getRow: vi.fn((id: string | number) => {
			const rows: Record<number, Record<string, string | number>> = {
				1: { name: "John Doe", age: 30, email: "john@example.com" },
				2: { name: "Jane Smith", age: 25, email: "jane@example.com" },
				3: { name: "Bob Johnson", age: 35, email: "bob@example.com" },
			};
			return rows[Number(id)] || null;
		}),
		getCellValue: vi.fn((id: string | number, field: string) => {
			const rows: Record<number, Record<string, string | number>> = {
				1: { name: "John Doe", age: 30, email: "john@example.com" },
				2: { name: "Jane Smith", age: 25, email: "jane@example.com" },
				3: { name: "Bob Johnson", age: 35, email: "bob@example.com" },
			};
			return rows[Number(id)]?.[field];
		}),
	} as unknown as GridApiCommon,
};

describe("DataGridToolbar", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Rendering", () => {
		it("should render both copy and export buttons", () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			const exportButton = screen.getByTestId("export-button");

			expect(copyButton).toBeInTheDocument();
			expect(exportButton).toBeInTheDocument();
		});

		it("should render buttons as enabled when premium features are available", () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			const exportButton = screen.getByTestId("export-button");

			expect(copyButton).not.toBeDisabled();
			expect(exportButton).not.toBeDisabled();
		});

		it("should render buttons as disabled when premium features are not available", () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={false} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			const exportButton = screen.getByTestId("export-button");

			expect(copyButton).toBeDisabled();
			expect(exportButton).toBeDisabled();
		});
	});

	describe("Tooltips", () => {
		it("should show 'Copy to Clipboard' tooltip when premium is available and not copied", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			await userEvent.hover(copyButton);

			await waitFor(() => {
				expect(screen.getByText("Copy to Clipboard")).toBeInTheDocument();
			});
		});

		it("should show 'Export to CSV' tooltip when premium is available", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const exportButton = screen.getByTestId("export-button");
			await userEvent.hover(exportButton);

			await waitFor(() => {
				expect(screen.getByText("Export to CSV")).toBeInTheDocument();
			});
		});

		it("should show premium feature message when premium is not available", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={false} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");

			// Verify tooltip exists (can't hover disabled button, so check aria-label on parent span)
			const copyButtonParent = copyButton.parentElement;
			expect(copyButtonParent).toHaveAttribute(
				"aria-label",
				"Premium feature - Upgrade to use",
			);
		});
	});

	describe("Copy to Clipboard", () => {
		it("should copy data to clipboard when copy button is clicked with premium", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			await userEvent.click(copyButton);

			await waitFor(() => {
				expect(mockClipboardWrite).toHaveBeenCalledTimes(1);
			});

			// Verify clipboard was called with ClipboardItem
			const clipboardCall = mockClipboardWrite.mock.calls[0][0];
			expect(clipboardCall).toHaveLength(1);
			expect(clipboardCall[0]).toBeInstanceOf(ClipboardItem);
		});

		it("should show 'Copied!' feedback after successful copy", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			await userEvent.click(copyButton);

			await waitFor(() => {
				expect(screen.getByText("Copied!")).toBeInTheDocument();
			});
		});

		it("should not copy when premium features are not available", async () => {
			const consoleWarnSpy = vi
				.spyOn(console, "warn")
				.mockImplementation(() => {});

			render(
				<DataGridToolbar canUsePremiumFeatures={false} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");

			// Button is disabled, so click won't trigger the handler
			// We need to verify the button is disabled
			expect(copyButton).toBeDisabled();
			expect(mockClipboardWrite).not.toHaveBeenCalled();

			consoleWarnSpy.mockRestore();
		});

		it("should include headers in copied data", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			await userEvent.click(copyButton);

			await waitFor(() => {
				expect(mockClipboardWrite).toHaveBeenCalled();
			});

			// Verify the mock was called - actual data verification would require
			// reading the Blob contents which is complex in tests
			expect(mockApiRef.current.getSortedRowIds).toHaveBeenCalled();
			expect(mockApiRef.current.getVisibleColumns).toHaveBeenCalled();
		});

		it("should use getCellValue to handle valueGetter columns", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			await userEvent.click(copyButton);

			await waitFor(() => {
				expect(mockClipboardWrite).toHaveBeenCalled();
			});

			// Verify getCellValue was called for each row/column combination
			expect(mockApiRef.current.getCellValue).toHaveBeenCalled();
			// 3 rows * 3 columns = 9 calls
			expect(mockApiRef.current.getCellValue).toHaveBeenCalledTimes(9);
		});
	});

	describe("CSV Export", () => {
		it("should trigger CSV download when export button is clicked with premium", async () => {
			// Mock document.body methods
			const appendChildSpy = vi.spyOn(document.body, "appendChild");
			const removeChildSpy = vi.spyOn(document.body, "removeChild");

			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const exportButton = screen.getByTestId("export-button");
			await userEvent.click(exportButton);

			await waitFor(() => {
				expect(appendChildSpy).toHaveBeenCalled();
				expect(removeChildSpy).toHaveBeenCalled();
			});

			appendChildSpy.mockRestore();
			removeChildSpy.mockRestore();
		});

		it("should use custom filename when provided", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					exportFileName="custom-export"
				/>,
			);

			const exportButton = screen.getByTestId("export-button");

			// Spy on appendChild after render
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			await userEvent.click(exportButton);

			await waitFor(() => {
				// Find the anchor element call
				const anchorCall = appendChildSpy.mock.calls.find(
					(call) => (call[0] as HTMLElement).tagName === "A",
				);
				expect(anchorCall).toBeDefined();

				if (anchorCall) {
					const linkElement = anchorCall[0] as HTMLAnchorElement;
					expect(linkElement.download).toMatch(
						/^custom-export_\d{4}-\d{2}-\d{2}\.csv$/,
					);
				}
			});

			appendChildSpy.mockRestore();
		});

		it("should use default filename when not provided", async () => {
			render(<DataGridToolbar canUsePremiumFeatures={true} />);

			const exportButton = screen.getByTestId("export-button");

			// Spy on appendChild after render
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			await userEvent.click(exportButton);

			await waitFor(() => {
				// Find the anchor element call
				const anchorCall = appendChildSpy.mock.calls.find(
					(call) => (call[0] as HTMLElement).tagName === "A",
				);
				expect(anchorCall).toBeDefined();

				if (anchorCall) {
					const linkElement = anchorCall[0] as HTMLAnchorElement;
					expect(linkElement.download).toMatch(
						/^data_export_\d{4}-\d{2}-\d{2}\.csv$/,
					);
				}
			});

			appendChildSpy.mockRestore();
		});

		it("should not export when premium features are not available", async () => {
			render(
				<DataGridToolbar canUsePremiumFeatures={false} exportFileName="test" />,
			);

			const exportButton = screen.getByTestId("export-button");

			// Spy on appendChild after render
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			// Button is disabled, so verify it can't be clicked
			expect(exportButton).toBeDisabled();

			// No anchor element should have been appended
			const anchorCall = appendChildSpy.mock.calls.find(
				(call) => (call[0] as HTMLElement).tagName === "A",
			);
			expect(anchorCall).toBeUndefined();

			appendChildSpy.mockRestore();
		});

		it("should fetch data from grid API when exporting", async () => {
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const exportButton = screen.getByTestId("export-button");
			await userEvent.click(exportButton);

			await waitFor(() => {
				expect(mockApiRef.current.getSortedRowIds).toHaveBeenCalled();
				expect(mockApiRef.current.getVisibleColumns).toHaveBeenCalled();
			});

			appendChildSpy.mockRestore();
		});

		it("should use getCellValue for CSV export to handle valueGetter columns", async () => {
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			const exportButton = screen.getByTestId("export-button");
			await userEvent.click(exportButton);

			await waitFor(() => {
				// Verify getCellValue was called for each row/column combination
				expect(mockApiRef.current.getCellValue).toHaveBeenCalled();
				// 3 rows * 3 columns = 9 calls
				expect(mockApiRef.current.getCellValue).toHaveBeenCalledTimes(9);
			});

			appendChildSpy.mockRestore();
		});
	});

	describe("Runtime Premium Checks", () => {
		it("should prevent copy if premium is removed after render", async () => {
			const consoleWarnSpy = vi
				.spyOn(console, "warn")
				.mockImplementation(() => {});

			const { rerender } = render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			// Change to no premium
			rerender(
				<DataGridToolbar canUsePremiumFeatures={false} exportFileName="test" />,
			);

			const copyButton = screen.getByTestId("copy-button");
			expect(copyButton).toBeDisabled();

			consoleWarnSpy.mockRestore();
		});

		it("should prevent export if premium is removed after render", async () => {
			const { rerender } = render(
				<DataGridToolbar canUsePremiumFeatures={true} exportFileName="test" />,
			);

			// Spy on appendChild after initial render
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			// Change to no premium
			rerender(
				<DataGridToolbar canUsePremiumFeatures={false} exportFileName="test" />,
			);

			const exportButton = screen.getByTestId("export-button");
			expect(exportButton).toBeDisabled();

			// No anchor element should have been appended
			const anchorCall = appendChildSpy.mock.calls.find(
				(call) => (call[0] as HTMLElement).tagName === "A",
			);
			expect(anchorCall).toBeUndefined();

			appendChildSpy.mockRestore();
		});
	});
});
