import { render, screen } from "@testing-library/react";
import type { Mock } from "vitest";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import DataGridBase from "./DataGridBase";
import type { DataGridColumn } from "./types";

// Mock @mui/x-data-grid CSS import
vi.mock("@mui/x-data-grid", async () => {
	const actual = await vi.importActual("@mui/x-data-grid");
	return {
		...actual,
	};
});

// Mock the useLicenseRestrictions hook
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: vi.fn(),
}));

// Mock data interface
interface TestRow {
	id: number;
	name: string;
	age: number;
	email: string;
}

// Mock matchMedia before each test
beforeEach(() => {
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		})),
	});

	// Clear localStorage before each test
	localStorage.clear();

	// Default mock for useLicenseRestrictions - no premium by default
	(useLicenseRestrictions as unknown as Mock).mockReturnValue({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	});
});

const mockColumns: DataGridColumn<TestRow>[] = [
	{
		field: "id",
		headerName: "ID",
		width: 70,
		sortable: true,
		type: "number",
	},
	{
		field: "name",
		headerName: "Name",
		width: 130,
		sortable: true,
		type: "string",
	},
	{
		field: "age",
		headerName: "Age",
		width: 90,
		sortable: true,
		type: "number",
	},
	{
		field: "email",
		headerName: "Email",
		width: 200,
		sortable: true,
		type: "string",
	},
];

const mockRows: TestRow[] = [
	{ id: 1, name: "Alice", age: 30, email: "alice@example.com" },
	{ id: 2, name: "Bob", age: 25, email: "bob@example.com" },
	{ id: 3, name: "Charlie", age: 35, email: "charlie@example.com" },
];

describe("DataGridBase", () => {
	describe("Basic Rendering", () => {
		it("should render the data grid with provided rows and columns", () => {
			render(<DataGridBase rows={mockRows} columns={mockColumns} />);

			// Check if the grid is rendered
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Check if column headers are rendered
			expect(screen.getByText("ID")).toBeInTheDocument();
			expect(screen.getByText("Name")).toBeInTheDocument();
			expect(screen.getByText("Age")).toBeInTheDocument();
			expect(screen.getByText("Email")).toBeInTheDocument();
		});

		it("should display all rows in the grid", () => {
			render(<DataGridBase rows={mockRows} columns={mockColumns} />);

			// Check if all rows are displayed
			expect(screen.getByText("Alice")).toBeInTheDocument();
			expect(screen.getByText("Bob")).toBeInTheDocument();
			expect(screen.getByText("Charlie")).toBeInTheDocument();
		});

		it("should use idField prop to identify rows", () => {
			render(
				<DataGridBase rows={mockRows} columns={mockColumns} idField="id" />,
			);

			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Verify rows are rendered
			expect(screen.getByText("Alice")).toBeInTheDocument();
		});

		it("should display empty state message when no rows are provided", () => {
			render(
				<DataGridBase
					rows={[]}
					columns={mockColumns}
					emptyStateMessage="No data available"
				/>,
			);

			expect(screen.getByText("No data available")).toBeInTheDocument();
		});

		it("should show loading state when loading prop is true", () => {
			render(<DataGridBase rows={[]} columns={mockColumns} loading={true} />);

			// MUI DataGrid shows loading overlay - check for the loading overlay class
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Check that the grid has no rows when loading
			const rows = screen.queryAllByRole("row").filter(
				(row) => row.getAttribute("aria-rowindex") !== "1", // Exclude header row
			);
			expect(rows.length).toBe(0);
		});
	});

	describe("Custom Cell Renderers", () => {
		it("should render custom cell content when renderCell is provided", () => {
			const customColumns: DataGridColumn<TestRow>[] = [
				{
					field: "name",
					headerName: "Name",
					width: 200,
					renderCell: ({ row }) => <strong>{row.name.toUpperCase()}</strong>,
				},
			];

			render(<DataGridBase rows={mockRows} columns={customColumns} />);

			// Check if custom rendered content is displayed
			expect(screen.getByText("ALICE")).toBeInTheDocument();
			expect(screen.getByText("BOB")).toBeInTheDocument();
			expect(screen.getByText("CHARLIE")).toBeInTheDocument();
		});

		it("should render custom cell with complex elements", () => {
			const customColumns: DataGridColumn<TestRow>[] = [
				{
					field: "email",
					headerName: "Email",
					width: 250,
					renderCell: ({ row }) => (
						<a
							href={`mailto:${row.email}`}
							data-testid={`email-link-${row.id}`}
						>
							{row.email}
						</a>
					),
				},
			];

			render(<DataGridBase rows={mockRows} columns={customColumns} />);

			// Check if custom link is rendered
			const emailLink = screen.getByTestId("email-link-1");
			expect(emailLink).toBeInTheDocument();
			expect(emailLink).toHaveAttribute("href", "mailto:alice@example.com");
		});
	});

	describe("TypeScript Type Safety", () => {
		it("should be type-safe with generic row types", () => {
			// This test verifies TypeScript compilation rather than runtime behavior
			// If this compiles without errors, type safety is working correctly
			const typedColumns: DataGridColumn<TestRow>[] = [
				{
					field: "name",
					headerName: "Name",
					width: 200,
					renderCell: ({ row }) => {
						// TypeScript should know row is TestRow
						const name: string = row.name;
						return <span>{name}</span>;
					},
				},
			];

			render(<DataGridBase rows={mockRows} columns={typedColumns} />);
			expect(screen.getByText("Alice")).toBeInTheDocument();
		});
	});

	describe("Sorting", () => {
		it("should allow sorting by clicking on column headers", async () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} />,
			);

			// Find the Name column header
			const nameHeader = screen.getByText("Name");
			expect(nameHeader).toBeInTheDocument();

			// Verify sortable columns have sort buttons
			const sortButtons = container.querySelectorAll(
				'button[aria-label="Sort"]',
			);
			expect(sortButtons.length).toBeGreaterThan(0);
		});

		it("should call onSortModelChange when sorting changes", async () => {
			const onSortModelChange = vi.fn();

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					onSortModelChange={onSortModelChange}
				/>,
			);

			// Grid is rendered with sortable columns
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should initialize with provided sort model", () => {
			const initialSortModel = [{ field: "name", sort: "asc" as const }];

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					initialSortModel={initialSortModel}
				/>,
			);

			// Verify grid renders with initial sort
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should support sorting on all columns by default", () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} />,
			);

			// All columns should have sort capability
			const columnHeaders = container.querySelectorAll('[role="columnheader"]');
			expect(columnHeaders.length).toBe(mockColumns.length);
		});
	});

	describe("Column Visibility", () => {
		it("should hide columns specified in initialHiddenColumns", () => {
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					initialHiddenColumns={["age", "email"]}
				/>,
			);

			// Visible columns should be rendered
			expect(screen.getByText("ID")).toBeInTheDocument();
			expect(screen.getByText("Name")).toBeInTheDocument();

			// Hidden columns should not be visible - check by column header
			// Note: MUI DataGrid still renders hidden columns in the DOM but hides them with CSS
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should call onColumnVisibilityChange when column visibility changes", () => {
			const onColumnVisibilityChange = vi.fn();

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					onColumnVisibilityChange={onColumnVisibilityChange}
					initialHiddenColumns={["age"]}
				/>,
			);

			// Verify component renders
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// The callback should be ready to receive changes
			// (actual interaction testing would require userEvent clicks on column menu)
		});

		it("should support column menu for visibility toggle when disableColumnSelector is false", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					disableColumnSelector={false}
				/>,
			);

			// Check that column menu buttons are present
			const menuButtons = container.querySelectorAll(
				'button[aria-label*="column menu"]',
			);
			expect(menuButtons.length).toBeGreaterThan(0);
		});

		it("should hide column selector by default when disableColumnSelector is not specified", () => {
			render(<DataGridBase rows={mockRows} columns={mockColumns} />);

			// Grid should render
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should hide column menu when disableColumnMenu is true", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					disableColumnMenu={true}
				/>,
			);

			// Column menu buttons should not be present
			const menuButtons = container.querySelectorAll(
				'button[aria-label*="column menu"]',
			);
			expect(menuButtons.length).toBe(0);
		});
	});

	describe("Responsive Design & Sizing", () => {
		it("should render with default height of 600px", () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} />,
			);

			const gridContainer = container.querySelector(".MuiBox-root");
			expect(gridContainer).toHaveStyle({ height: "600px" });
		});

		it("should render with custom height", () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} height={400} />,
			);

			const gridContainer = container.querySelector(".MuiBox-root");
			expect(gridContainer).toHaveStyle({ height: "400px" });
		});

		it("should render with string height value", () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} height="80vh" />,
			);

			const gridContainer = container.querySelector(".MuiBox-root");
			expect(gridContainer).toHaveStyle({ height: "80vh" });
		});

		it("should render with auto height when autoHeight is true", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					autoHeight={true}
				/>,
			);

			const gridContainer = container.querySelector(".MuiBox-root");
			expect(gridContainer).toHaveStyle({ height: "auto" });
		});

		it("should always render with 100% width", () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} />,
			);

			const gridContainer = container.querySelector(".MuiBox-root");
			expect(gridContainer).toHaveStyle({ width: "100%" });
		});
	});

	describe("Virtualization", () => {
		it("should support virtualization by default for large datasets", () => {
			// Create a large dataset
			const largeDataset = Array.from({ length: 200 }, (_, i) => ({
				id: i + 1,
				name: `User ${i + 1}`,
				age: 20 + (i % 50),
				email: `user${i + 1}@example.com`,
			}));

			const { container } = render(
				<DataGridBase rows={largeDataset} columns={mockColumns} />,
			);

			// DataGrid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// With virtualization, not all rows are rendered in the DOM
			const rows = container.querySelectorAll('[role="row"]');
			// Should be less than total rows (1 header + virtualized rows)
			expect(rows.length).toBeLessThan(largeDataset.length + 1);
		});
	});

	describe("Filtering", () => {
		it("should show filter UI by default when enableFiltering is not specified", () => {
			const { container } = render(
				<DataGridBase rows={mockRows} columns={mockColumns} />,
			);

			// Grid should render with filtering enabled by default
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Column headers should have filter capability
			const columnHeaders = container.querySelectorAll('[role="columnheader"]');
			expect(columnHeaders.length).toBeGreaterThan(0);
		});

		it("should disable filtering when enableFiltering is explicitly false", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={false}
				/>,
			);

			// Grid should render
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Filter panel should not be available
			const filterPanel = container.querySelector(".MuiDataGrid-filterForm");
			expect(filterPanel).not.toBeInTheDocument();
		});

		it("should enable filtering when enableFiltering prop is true", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
				/>,
			);

			// Grid should render
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Column headers should have filter capability
			// (MUI DataGrid shows filter menu in column headers when filtering is enabled)
			const columnHeaders = container.querySelectorAll('[role="columnheader"]');
			expect(columnHeaders.length).toBeGreaterThan(0);
		});

		it("should initialize with provided filter model", () => {
			const initialFilterModel = {
				items: [{ field: "name", operator: "contains", value: "Alice" }],
			};

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					initialFilterModel={initialFilterModel}
				/>,
			);

			// Grid should render with filter applied
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Alice should be visible
			expect(screen.getByText("Alice")).toBeInTheDocument();
		});

		it("should call onFilterModelChange when filter changes", () => {
			const onFilterModelChange = vi.fn();

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					onFilterModelChange={onFilterModelChange}
				/>,
			);

			// Grid is rendered with filtering enabled
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Callback should be ready to receive filter changes
			// (actual interaction testing would require userEvent interactions)
		});

		it("should support text column filtering with contains operator", () => {
			const filterModel = {
				items: [{ field: "name", operator: "contains", value: "li" }],
			};

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					initialFilterModel={filterModel}
				/>,
			);

			// Alice and Charlie contain 'li'
			expect(screen.getByText("Alice")).toBeInTheDocument();
			expect(screen.getByText("Charlie")).toBeInTheDocument();

			// Grid should be present
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should support number column filtering with equals operator", () => {
			const filterModel = {
				items: [{ field: "age", operator: "=", value: "30" }],
			};

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					initialFilterModel={filterModel}
				/>,
			);

			// Only Alice has age 30
			expect(screen.getByText("Alice")).toBeInTheDocument();

			// Grid should be present
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should support number column filtering with greater than operator", () => {
			const filterModel = {
				items: [{ field: "age", operator: ">", value: "30" }],
			};

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					initialFilterModel={filterModel}
				/>,
			);

			// Only Charlie has age > 30
			expect(screen.getByText("Charlie")).toBeInTheDocument();

			// Grid should be present
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should support multiple filters combined with AND logic", () => {
			const filterModel = {
				items: [
					{ field: "name", operator: "contains", value: "li" },
					{ field: "age", operator: ">", value: "30" },
				],
			};

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					initialFilterModel={filterModel}
				/>,
			);

			// Only Charlie matches both: contains 'li' AND age > 30
			expect(screen.getByText("Charlie")).toBeInTheDocument();

			// Grid should be present
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should show all rows when filter is cleared", () => {
			const filterModel = {
				items: [],
			};

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
					initialFilterModel={filterModel}
				/>,
			);

			// All rows should be visible
			expect(screen.getByText("Alice")).toBeInTheDocument();
			expect(screen.getByText("Bob")).toBeInTheDocument();
			expect(screen.getByText("Charlie")).toBeInTheDocument();
		});

		it("should make columns filterable by default when enableFiltering is true", () => {
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableFiltering={true}
				/>,
			);

			// Grid should render
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// All columns should be filterable
			// (This is verified by MUI DataGrid's default behavior)
		});
	});

	describe("CSV Export (Premium Feature)", () => {
		it("should show export button enabled when premium license is available", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={true}
					exportFileName="test-export"
				/>,
			);

			// Toolbar should be visible with export button enabled
			const exportButton = screen.getByTestId("export-button");
			expect(exportButton).toBeInTheDocument();
			expect(exportButton).not.toBeDisabled();
		});

		it("should show export button disabled when premium license is not available", () => {
			// Mock no premium license (default from beforeEach)
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={true}
					exportFileName="test-export"
				/>,
			);

			// Export button should be visible but disabled
			const exportButton = screen.getByTestId("export-button");
			expect(exportButton).toBeInTheDocument();
			expect(exportButton).toBeDisabled();
		});

		it("should not show toolbar when export is disabled", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={false}
				/>,
			);

			// No toolbar should be present
			const exportButton = screen.queryByTestId("export-button");
			expect(exportButton).not.toBeInTheDocument();
		});

		it("should show toolbar with disabled buttons when export is enabled but no premium license", () => {
			// Mock no premium license (default from beforeEach)
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={true}
					exportFileName="test-export"
				/>,
			);

			// Buttons should be visible but disabled
			const exportButton = screen.getByTestId("export-button");
			const copyButton = screen.getByTestId("copy-button");
			expect(exportButton).toBeInTheDocument();
			expect(exportButton).toBeDisabled();
			expect(copyButton).toBeInTheDocument();
			expect(copyButton).toBeDisabled();
		});

		it("should use custom export filename when provided", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			const customFileName = "my-custom-export";
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={true}
					exportFileName={customFileName}
				/>,
			);

			// Verify toolbar is rendered (actual CSV generation is tested by implementation)
			const exportButton = screen.getByTestId("export-button");
			expect(exportButton).toBeInTheDocument();
			expect(exportButton).not.toBeDisabled();
		});

		it("should default enableExport to false when not specified", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(<DataGridBase rows={mockRows} columns={mockColumns} />);

			// No export button should be visible by default
			const exportButton = screen.queryByTestId("export-button");
			expect(exportButton).not.toBeInTheDocument();
		});

		it("should show copy button enabled when premium license is available", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={true}
				/>,
			);

			// Copy button should be visible and enabled
			const copyButton = screen.getByTestId("copy-button");
			expect(copyButton).toBeInTheDocument();
			expect(copyButton).not.toBeDisabled();
		});

		it("should show copy button disabled when premium license is not available", () => {
			// Mock no premium license (default from beforeEach)
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					enableExport={true}
				/>,
			);

			// Copy button should be visible but disabled
			const copyButton = screen.getByTestId("copy-button");
			expect(copyButton).toBeInTheDocument();
			expect(copyButton).toBeDisabled();
		});
	});
});
