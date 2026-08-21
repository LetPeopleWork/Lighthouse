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
		DataGrid: vi.fn(() => null), // Mock DataGrid component
	};
});

// Mock clipboard API
const mockClipboardWrite = vi.fn();

// Mock ClipboardItem
interface MockClipboardItem {
	items: Record<string, Blob>;
}

globalThis.ClipboardItem = class ClipboardItem implements MockClipboardItem {
	constructor(public items: Record<string, Blob>) {}
} as unknown as typeof ClipboardItem;

Object.assign(navigator, {
	clipboard: {
		write: mockClipboardWrite,
	},
});

// Mock URL.createObjectURL and revokeObjectURL
globalThis.URL.createObjectURL = vi.fn(() => "mock-url");
globalThis.URL.revokeObjectURL = vi.fn();

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
		showColumnMenu: vi.fn(),
	} as unknown as GridApiCommon,
};

describe("DataGridToolbar", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	describe("Rendering", () => {
		it("should render export buttons when export is enabled", () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const copyButton = screen.getByTestId("copy-button");
			const exportButton = screen.getByTestId("export-button");

			expect(copyButton).toBeInTheDocument();
			expect(exportButton).toBeInTheDocument();
		});

		it("should render buttons as enabled when premium features are available", () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const copyButton = screen.getByTestId("copy-button");
			const exportButton = screen.getByTestId("export-button");

			expect(copyButton).not.toBeDisabled();
			expect(exportButton).not.toBeDisabled();
		});

		it("should render export buttons as disabled when premium features are not available", () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={false}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const copyButton = screen.getByTestId("copy-button");
			const exportButton = screen.getByTestId("export-button");

			// Export buttons should be disabled without premium
			expect(copyButton).toBeDisabled();
			expect(exportButton).toBeDisabled();
		});

		it("should not render export buttons when enableExport is false", () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={false}
					exportFileName="test"
				/>,
			);

			// Export buttons should not be rendered
			expect(screen.queryByTestId("copy-button")).not.toBeInTheDocument();
			expect(screen.queryByTestId("export-button")).not.toBeInTheDocument();
		});

		it("should render reset layout button when handler passed", async () => {
			const onReset = vi.fn();
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
					onResetLayout={onReset}
				/>,
			);

			const resetButton = screen.getByTestId("reset-layout-button");
			expect(resetButton).toBeInTheDocument();

			await userEvent.click(resetButton);
			expect(onReset).toHaveBeenCalled();
		});
	});

	describe("Tooltips", () => {
		it("should show 'Copy to Clipboard' tooltip when premium is available and not copied", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const copyButton = screen.getByTestId("copy-button");
			await userEvent.hover(copyButton);

			await waitFor(() => {
				expect(screen.getByText("Copy to Clipboard")).toBeInTheDocument();
			});
		});

		it("should show 'Export to CSV' tooltip when premium is available", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const exportButton = screen.getByTestId("export-button");
			await userEvent.hover(exportButton);

			await waitFor(() => {
				expect(screen.getByText("Export to CSV")).toBeInTheDocument();
			});
		});

		it("should show premium feature message when premium is not available", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={false}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={false}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
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

			// Mock HTMLElement.remove() since we use link.remove() instead of removeChild
			const mockRemove = vi.fn();
			const originalCreateElement = document.createElement.bind(document);
			vi.spyOn(document, "createElement").mockImplementation((tagName) => {
				const element = originalCreateElement(tagName);
				if (tagName === "a") {
					element.remove = mockRemove;
				}
				return element;
			});

			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const exportButton = screen.getByTestId("export-button");
			await userEvent.click(exportButton);

			await waitFor(() => {
				expect(appendChildSpy).toHaveBeenCalled();
				expect(mockRemove).toHaveBeenCalled();
			});

			appendChildSpy.mockRestore();
			vi.mocked(document.createElement).mockRestore();
		});

		it("should use custom filename when provided", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
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
			render(
				<DataGridToolbar canUsePremiumFeatures={true} enableExport={true} />,
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
						/^data_export_\d{4}-\d{2}-\d{2}\.csv$/,
					);
				}
			});

			appendChildSpy.mockRestore();
		});

		it("should not export when premium features are not available", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={false}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
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
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			// Change to no premium
			rerender(
				<DataGridToolbar
					canUsePremiumFeatures={false}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			const copyButton = screen.getByTestId("copy-button");
			expect(copyButton).toBeDisabled();

			consoleWarnSpy.mockRestore();
		});

		it("should prevent export if premium is removed after render", async () => {
			const { rerender } = render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
				/>,
			);

			// Spy on appendChild after initial render
			const appendChildSpy = vi.spyOn(document.body, "appendChild");

			// Change to no premium
			rerender(
				<DataGridToolbar
					canUsePremiumFeatures={false}
					enableExport={true}
					exportFileName="test"
				/>,
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

	describe("Export header rows", () => {
		const HEADER_ROWS = [
			{ label: "Delivery", value: "Q3 Platform" },
			{ label: "Likelihood", value: "82%" },
		];

		// The existing suite stops at "an export happened". The header block only exists inside the
		// artifact, so these read the Blob the export actually produced.
		const capturedCsv = async (): Promise<string> => {
			const createObjectURL = globalThis.URL
				.createObjectURL as unknown as ReturnType<typeof vi.fn>;
			const calls = createObjectURL.mock.calls;
			const blob = calls[calls.length - 1]?.[0] as Blob;
			return await blob.text();
		};

		const capturedClipboard = async (
			flavour: "text/plain" | "text/html",
		): Promise<string> => {
			const calls = mockClipboardWrite.mock.calls;
			const item = calls[calls.length - 1]?.[0]?.[0] as {
				items: Record<string, Blob>;
			};
			return await item.items[flavour].text();
		};

		const exportCsvWith = async (
			rows?: readonly { label: string; value: string }[],
		) => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportFileName="test"
					exportHeaderRows={rows}
				/>,
			);
			await userEvent.click(screen.getByTestId("export-button"));
			await waitFor(() =>
				expect(globalThis.URL.createObjectURL).toHaveBeenCalled(),
			);
		};

		it("leads the CSV with the header block, then a blank line, then the grid", async () => {
			await exportCsvWith(HEADER_ROWS);

			const lines = (await capturedCsv()).replace("﻿", "").split("\n");

			expect(lines[0]).toBe("Delivery,Q3 Platform");
			expect(lines[1]).toBe("Likelihood,82%");
			expect(lines[2]).toBe("");
			expect(lines[3]).toBe("Name,Age,Email");
			expect(lines[4]).toBe("John Doe,30,john@example.com");
		});

		it("emits no header block and no blank line when a grid supplies none", async () => {
			await exportCsvWith(undefined);

			const lines = (await capturedCsv()).replace("﻿", "").split("\n");

			expect(lines[0]).toBe("Name,Age,Email");
			expect(lines).not.toContain("");
		});

		it("escapes a header value containing a comma, a quote or a line break", async () => {
			await exportCsvWith([
				{ label: "Delivery", value: 'Q3 "Platform", phase\none' },
			]);

			const csv = (await capturedCsv()).replace("﻿", "");

			expect(csv.startsWith('Delivery,"Q3 ""Platform"", phase\none"')).toBe(
				true,
			);
		});

		it("leads the pasted text with the same block, tab separated", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportHeaderRows={HEADER_ROWS}
				/>,
			);
			await userEvent.click(screen.getByTestId("copy-button"));
			await waitFor(() => expect(mockClipboardWrite).toHaveBeenCalled());

			const lines = (await capturedClipboard("text/plain")).split("\n");

			expect(lines[0]).toBe("Delivery\tQ3 Platform");
			expect(lines[1]).toBe("Likelihood\t82%");
			expect(lines[2]).toBe("");
			expect(lines[3]).toBe("Name\tAge\tEmail");
		});

		it("puts the block in the same pasted table, so it lands as one thing", async () => {
			render(
				<DataGridToolbar
					canUsePremiumFeatures={true}
					enableExport={true}
					exportHeaderRows={HEADER_ROWS}
				/>,
			);
			await userEvent.click(screen.getByTestId("copy-button"));
			await waitFor(() => expect(mockClipboardWrite).toHaveBeenCalled());

			const html = await capturedClipboard("text/html");

			expect(html).toContain('<td style="font-weight: bold;">Delivery</td>');
			expect(html).toContain("<td>Q3 Platform</td>");
			expect(html.indexOf("Q3 Platform")).toBeLessThan(html.indexOf("<thead>"));
			expect(html.match(/<table/g) ?? []).toHaveLength(1);
		});
	});
});
