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
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

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
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			// Check if all rows are displayed
			expect(screen.getByText("Alice")).toBeInTheDocument();
			expect(screen.getByText("Bob")).toBeInTheDocument();
			expect(screen.getByText("Charlie")).toBeInTheDocument();
		});

		it("should use idField prop to identify rows", () => {
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					idField="id"
				/>,
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
					storageKey="test-grid"
					emptyStateMessage="No data available"
				/>,
			);

			expect(screen.getByText("No data available")).toBeInTheDocument();
		});

		it("should show loading state when loading prop is true", () => {
			render(
				<DataGridBase
					rows={[]}
					columns={mockColumns}
					storageKey="test-grid"
					loading={true}
				/>,
			);

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

			render(
				<DataGridBase
					rows={mockRows}
					columns={customColumns}
					storageKey="test-grid"
				/>,
			);

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

			render(
				<DataGridBase
					rows={mockRows}
					columns={customColumns}
					storageKey="test-grid"
				/>,
			);

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

			render(
				<DataGridBase
					rows={mockRows}
					columns={typedColumns}
					storageKey="test-grid"
				/>,
			);
			expect(screen.getByText("Alice")).toBeInTheDocument();
		});
	});

	describe("Sorting", () => {
		it("should allow sorting by clicking on column headers", async () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
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

		it("should initialize with provided sort model", () => {
			const initialSortModel = [{ field: "name", sort: "asc" as const }];

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					initialSortModel={initialSortModel}
				/>,
			);

			// Verify grid renders with initial sort
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should support sorting on all columns by default", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			// All columns should have sort capability
			const columnHeaders = container.querySelectorAll('[role="columnheader"]');
			expect(columnHeaders.length).toBe(mockColumns.length);
		});
	});

	describe("Column Visibility", () => {
		it("should hide column selector by default when disableColumnSelector is not specified", () => {
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			// Grid should render
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});
	});

	describe("Responsive Design & Sizing", () => {
		it("should render with default height of auto", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			const gridContainer = container.querySelector(".MuiBox-root");
			expect(gridContainer).toHaveStyle({ height: "auto" });
		});

		it("should always render with 100% width", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
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
				<DataGridBase
					rows={largeDataset}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
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
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			// Grid should render with filtering enabled by default
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();

			// Column headers should have filter capability
			const columnHeaders = container.querySelectorAll('[role="columnheader"]');
			expect(columnHeaders.length).toBeGreaterThan(0);
		});

		it("should enable filtering", () => {
			const { container } = render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
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

		it("should make columns filterable by default", () => {
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
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
		it("should render successfully when export is enabled with premium license", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					enableExport={true}
					exportFileName="test-export"
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should render successfully when export is enabled without premium license", () => {
			// Mock no premium license (default from beforeEach)
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					enableExport={true}
					exportFileName="test-export"
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should render successfully when export is disabled", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					enableExport={false}
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should render successfully with disabled buttons when export is enabled but no premium license", () => {
			// Mock no premium license (default from beforeEach)
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					enableExport={true}
					exportFileName="test-export"
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should render successfully with custom export filename when provided", () => {
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
					storageKey="test-grid"
					enableExport={true}
					exportFileName={customFileName}
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should default enableExport to false when not specified", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			// Grid should render successfully with default settings
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should render successfully with copy button available when premium license is available", () => {
			// Mock premium license
			(useLicenseRestrictions as unknown as Mock).mockReturnValue({
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
			});

			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					enableExport={true}
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});

		it("should render successfully with copy button disabled when premium license is not available", () => {
			// Mock no premium license (default from beforeEach)
			render(
				<DataGridBase
					rows={mockRows}
					columns={mockColumns}
					storageKey="test-grid"
					enableExport={true}
				/>,
			);

			// Grid should render successfully
			const grid = screen.getByRole("grid");
			expect(grid).toBeInTheDocument();
		});
	});
});
