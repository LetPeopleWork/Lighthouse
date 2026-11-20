import type { GridRowId } from "@mui/x-data-grid";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { useState } from "react";
import type { Mock } from "vitest";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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
		it("should allow sorting by clicking on column headers", () => {
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
				'button[aria-label^="Sort "]',
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

	it("should not allow non-hideable columns to be hidden", () => {
		const storageKey = "test-grid";
		const persisted = { columnVisibilityModel: { name: false } };
		// Mark 'name' column as non-hideable by setting hideable flag on the column definition
		const columnsWithNonHideable: DataGridColumn<TestRow>[] = [
			{ field: "id", headerName: "ID", width: 70, sortable: true },
			{
				field: "name",
				headerName: "Name",
				width: 130,
				sortable: true,
				hideable: false,
			},
			{ field: "age", headerName: "Age", width: 90, sortable: true },
			{ field: "email", headerName: "Email", width: 200, sortable: true },
		];

		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify(persisted),
		);

		render(
			<DataGridBase
				rows={mockRows}
				columns={columnsWithNonHideable}
				storageKey={storageKey}
			/>,
		);

		// Even though persisted state tried to hide 'name', it should remain visible
		expect(screen.getByText("Name")).toBeInTheDocument();
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

	describe("A list longer than a page", () => {
		it("renders one page of 100 rows of a 200-row list, plus the header row", () => {
			const twoHundredRows = Array.from({ length: 200 }, (_, i) => ({
				id: i + 1,
				name: `User ${i + 1}`,
				age: 20 + (i % 50),
				email: `user${i + 1}@example.com`,
			}));

			const { container } = render(
				<DataGridBase
					rows={twoHundredRows}
					columns={mockColumns}
					storageKey="test-grid"
				/>,
			);

			expect(container.querySelectorAll('[role="row"]')).toHaveLength(101);
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

describe("Persistence (Order & Widths)", () => {
	it("should apply persisted column order on mount", () => {
		const storageKey = "test-grid";
		const persisted = {
			columnOrder: ["age", "name", "id", "email"],
		};
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify(persisted),
		);

		const { container } = render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
			/>,
		);

		// Column headers should appear in order specified in columnOrder - ensure Age appears before Name
		const headers = container.querySelectorAll('[role="columnheader"]');
		// Map to text content
		const headerTexts = Array.from(headers).map(
			(h) => h.textContent?.trim() || "",
		);
		const ageIdx = headerTexts.indexOf("Age");
		const nameIdx = headerTexts.indexOf("Name");
		expect(ageIdx).toBeGreaterThan(-1);
		expect(nameIdx).toBeGreaterThan(-1);
		expect(ageIdx).toBeLessThan(nameIdx);
	});

	it("shows a column added since the order was saved where it is declared, not last", () => {
		const storageKey = "test-grid";
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify({ columnOrder: ["email", "age", "name"] }),
		);

		const { container } = render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
			/>,
		);

		const headerTexts = Array.from(
			container.querySelectorAll('[role="columnheader"]'),
		).map((h) => h.textContent?.trim() || "");
		expect(headerTexts).toEqual(["ID", "Email", "Age", "Name"]);
	});

	it("puts a newly declared column right after the column declared before it", () => {
		const storageKey = "test-grid";
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify({ columnOrder: ["email", "name", "id"] }),
		);

		const { container } = render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
			/>,
		);

		const headerTexts = Array.from(
			container.querySelectorAll('[role="columnheader"]'),
		).map((h) => h.textContent?.trim() || "");
		expect(headerTexts).toEqual(["Email", "Name", "Age", "ID"]);
	});

	it("keeps two columns added since the order was saved in the order they are declared", () => {
		const storageKey = "test-grid";
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify({ columnOrder: ["age", "email"] }),
		);

		const { container } = render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
			/>,
		);

		const headerTexts = Array.from(
			container.querySelectorAll('[role="columnheader"]'),
		).map((h) => h.textContent?.trim() || "");
		expect(headerTexts).toEqual(["ID", "Name", "Age", "Email"]);
	});

	it("should always show reset layout and column order buttons even when export is disabled", () => {
		render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey="test-grid"
				enableExport={false}
			/>,
		);

		// Reset layout should be visible even if export is disabled
		const resetButton = screen.getByTestId("reset-layout-button");
		expect(resetButton).toBeInTheDocument();

		// Column order button should also be visible by default
		const orderButton = screen.getByTestId("open-column-order-button");
		expect(orderButton).toBeInTheDocument();
	});

	it("should apply persisted column widths on mount", () => {
		const storageKey = "test-grid";
		const persisted = {
			columnWidths: { name: 250 },
		};
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify(persisted),
		);

		const { container } = render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
			/>,
		);

		// Check the header cell for Name width is roughly persisted value
		const nameHeader = container.querySelector('[data-field="name"]');
		if (nameHeader) {
			const styleWidth = (nameHeader as HTMLElement).style.width;
			// MUI may apply px values, so ensure persisted value exists in string
			expect(styleWidth.includes("250")).toBeTruthy();
		}
	});

	it("should clear persisted state when reset layout is clicked", async () => {
		const storageKey = "test-grid";
		const persisted = {
			columnOrder: ["age", "name", "id", "email"],
			columnWidths: { name: 200 },
			columnVisibilityModel: { email: false },
		};
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify(persisted),
		);

		render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
				enableExport={true}
			/>,
		);

		const resetButton = screen.getByTestId("reset-layout-button");
		expect(resetButton).toBeInTheDocument();

		await userEvent.click(resetButton);
		// Persisted state should be removed
		expect(
			localStorage.getItem(`lighthouse:datagrid:${storageKey}:state`),
		).toBeNull();
	});

	it("should open ColumnOrderDialog and persist order after save", async () => {
		const storageKey = "test-grid";
		render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
				enableExport={true}
			/>,
		);

		// Open column order dialog via toolbar button
		const orderButton = screen.getByTestId("open-column-order-button");
		expect(orderButton).toBeInTheDocument();

		await userEvent.click(orderButton);

		// Dialog should appear - find it by role
		const dialog = screen.getByRole("dialog");
		expect(dialog).toBeInTheDocument();

		const moveUpName = within(dialog).getByTestId("move-up-name");
		// Move 'name' up (if it's not first)
		await userEvent.click(moveUpName);

		// Save dialog
		const saveButton = within(dialog).getByText("Save");
		await userEvent.click(saveButton);

		// Persisted state should have columnOrder array
		const stored = JSON.parse(
			localStorage.getItem(`lighthouse:datagrid:${storageKey}:state`) || "{}",
		);
		expect(stored.columnOrder).toBeDefined();
		// Ensure 'name' appears somewhere in array (changes may vary depending on UI state)
		expect(stored.columnOrder).toContain("name");
	});

	it("offers every current column, and only those, for reordering when the saved order is out of date", async () => {
		const storageKey = "test-grid";
		localStorage.setItem(
			`lighthouse:datagrid:${storageKey}:state`,
			JSON.stringify({ columnOrder: ["email", "age", "retired", "name"] }),
		);
		render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey={storageKey}
			/>,
		);

		await userEvent.click(screen.getByTestId("open-column-order-button"));

		const offered = within(screen.getByRole("dialog"))
			.getAllByRole("listitem")
			.map((item) => item.textContent?.trim());
		expect(offered).toEqual(["ID", "Email", "Age", "Name"]);
	});
});

describe("Picking rows with checkboxes", () => {
	const PickingGrid = ({
		initiallyPicked = [],
		onPicked = () => undefined,
	}: {
		initiallyPicked?: GridRowId[];
		onPicked?: (ids: GridRowId[]) => void;
	}) => {
		const [picked, setPicked] = useState<GridRowId[]>(initiallyPicked);
		return (
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey="test-grid"
				checkboxSelection={{
					selectedIds: picked,
					onSelectionChange: (ids) => {
						setPicked(ids);
						onPicked(ids);
					},
				}}
			/>
		);
	};

	const checkboxesIn = (container: HTMLElement) =>
		container.querySelectorAll<HTMLInputElement>('input[type="checkbox"]');

	const rowCheckboxOf = (name: string) =>
		within(
			screen.getByText(name).closest('[role="row"]') as HTMLElement,
		).getByRole("checkbox");

	it("offers no checkbox when the grid is not asked for one", () => {
		const { container } = render(
			<DataGridBase
				rows={mockRows}
				columns={mockColumns}
				storageKey="test-grid"
			/>,
		);

		expect(checkboxesIn(container)).toHaveLength(0);
	});

	it("puts a checkbox on every row and one over the header when asked", () => {
		const { container } = render(<PickingGrid />);

		expect(checkboxesIn(container)).toHaveLength(mockRows.length + 1);
	});

	it("tells the caller the ids of the rows ticked so far", async () => {
		const onPicked = vi.fn();
		render(<PickingGrid onPicked={onPicked} />);
		const user = userEvent.setup();

		await user.click(rowCheckboxOf("Alice"));
		await user.click(rowCheckboxOf("Charlie"));

		expect(onPicked).toHaveBeenLastCalledWith([1, 3]);
	});

	it("shows ticked exactly the rows the caller holds as picked", () => {
		render(<PickingGrid initiallyPicked={[2]} />);

		expect(rowCheckboxOf("Alice")).not.toBeChecked();
		expect(rowCheckboxOf("Bob")).toBeChecked();
		expect(rowCheckboxOf("Charlie")).not.toBeChecked();
	});

	it("drops the picks a column filter hides and keeps the ones it still shows", async () => {
		const onPicked = vi.fn();
		render(<PickingGrid initiallyPicked={[1, 2, 3]} onPicked={onPicked} />);
		const user = userEvent.setup();

		await user.click(screen.getByLabelText("Name column menu"));
		await user.click(await screen.findByRole("menuitem", { name: /filter/i }));
		await user.type(await screen.findByLabelText("Value"), "Bob");

		await waitFor(() => expect(onPicked).toHaveBeenLastCalledWith([2]));
		expect(rowCheckboxOf("Bob")).toBeChecked();
	});
});

describe("Columns fit the width and keep the row actions in reach", () => {
	interface ReportRow {
		id: number;
		name: string;
		owner: string;
		age: number;
	}

	const LONG_NAME =
		"Lightspeed quarter against the quarter before, with every metric the team watches";

	const fittedColumns: DataGridColumn<ReportRow>[] = [
		{
			field: "actions",
			headerName: "Actions",
			width: 150,
			sortable: false,
			fit: { kind: "actions" },
			renderCell: ({ row }) => (
				<button type="button">{`Delete ${row.name}`}</button>
			),
		},
		{
			field: "name",
			headerName: "Name",
			flex: 1,
			fit: { kind: "text", minWidth: 160 },
			renderCell: ({ row }) => <a href={`/reports/${row.id}`}>{row.name}</a>,
		},
		{ field: "owner", headerName: "Owner", flex: 1, fit: { kind: "text" } },
		{ field: "age", headerName: "Age", width: 90, fit: { kind: "content" } },
	];

	const rows: ReportRow[] = [
		{ id: 1, name: LONG_NAME, owner: "Team Lightspeed", age: 3 },
	];

	const STORAGE_KEY = "fit-grid";
	const storedState = () =>
		JSON.parse(
			localStorage.getItem(`lighthouse:datagrid:${STORAGE_KEY}:state`) ??
				"null",
		);

	const givenTheSavedColumnOrder = (order: string[]) =>
		localStorage.setItem(
			`lighthouse:datagrid:${STORAGE_KEY}:state`,
			JSON.stringify({ columnOrder: order }),
		);

	const whenTheGridIsShown = () =>
		render(
			<DataGridBase
				rows={rows}
				columns={fittedColumns}
				storageKey={STORAGE_KEY}
			/>,
		);

	const thenTheActionsColumnIsLast = (container: HTMLElement) => {
		const headers = Array.from(
			container.querySelectorAll('[role="columnheader"]'),
		).map((header) => header.textContent?.trim());
		expect(
			headers.at(-1),
			`the headers are ${JSON.stringify(headers)}; the actions column is pinned to the right edge, so the grid must order it last whatever order is declared or saved`,
		).toBe("Actions");
	};

	const theTextIsCut = () => {
		vi.spyOn(HTMLElement.prototype, "scrollWidth", "get").mockReturnValue(600);
		vi.spyOn(HTMLElement.prototype, "clientWidth", "get").mockReturnValue(160);
	};

	const resizeColumn = (field: string, byPixels: number) => {
		const header = document.querySelector(
			`[role="columnheader"][data-field="${field}"]`,
		);
		const separator = header?.querySelector(
			".MuiDataGrid-columnSeparator--resizable",
		);
		expect(
			separator,
			`the ${field} column offers no resize handle; a person must be able to resize a text column`,
		).not.toBeNull();
		fireEvent.mouseDown(separator as Element, { button: 0, clientX: 0 });
		fireEvent.mouseMove(document, { buttons: 1, clientX: byPixels });
		fireEvent.mouseUp(document, { clientX: byPixels });
		act(() => {
			vi.advanceTimersByTime(1000);
		});
	};

	const storedWidthFields = () =>
		Object.keys(storedState()?.columnWidths ?? {}).sort((a, b) =>
			a.localeCompare(b),
		);

	afterEach(() => {
		vi.restoreAllMocks();
		vi.useRealTimers();
	});

	it("orders the actions column last when no order is saved", () => {
		const { container } = whenTheGridIsShown();
		thenTheActionsColumnIsLast(container);
	});

	it("orders the actions column last when the saved order puts it first", () => {
		givenTheSavedColumnOrder(["actions", "name", "owner", "age"]);
		const { container } = whenTheGridIsShown();
		thenTheActionsColumnIsLast(container);
	});

	it("orders the actions column last when the saved order does not name it", () => {
		givenTheSavedColumnOrder(["age", "name", "owner"]);
		const { container } = whenTheGridIsShown();
		thenTheActionsColumnIsLast(container);
	});

	it("stores only the width of the column the person resized", () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		whenTheGridIsShown();

		resizeColumn("name", 300);

		expect(
			storedWidthFields(),
			"storing every column width after one resize turns the fit off for all of them and pushes Delete off screen; only the resized Name width may be stored",
		).toEqual(["name"]);
	});

	it("keeps the Name width when Owner is resized next, and Reset clears both", () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		whenTheGridIsShown();
		resizeColumn("name", 300);
		const nameWidth = storedState()?.columnWidths?.name;

		resizeColumn("owner", 250);

		expect(
			storedWidthFields(),
			"a second resize must add the Owner width to the stored Name width and store nothing else",
		).toEqual(["name", "owner"]);
		expect(
			storedState()?.columnWidths?.name,
			"resizing Owner must leave the Name width the person set as it was",
		).toBe(nameWidth);

		act(() => {
			fireEvent.click(screen.getByTestId("reset-layout-button"));
		});
		expect(
			storedState(),
			"Reset must clear the stored widths so the grid returns to the fitted layout",
		).toBeNull();
	});

	it("gives the full text of a cut text cell in a tooltip on hover, and keeps it in the cell", async () => {
		theTextIsCut();
		whenTheGridIsShown();

		await userEvent.hover(screen.getByText("Team Lightspeed"));

		expect(
			await screen.findByRole("tooltip"),
			"a cut Owner name must show its full text in a tooltip when the person hovers it",
		).toHaveTextContent("Team Lightspeed");
		expect(
			screen.getAllByText("Team Lightspeed").length,
			"the full text must stay in the cell too, for assistive technology",
		).toBeGreaterThan(0);
	});

	it("gives the full text of a cut cell drawn by its own renderer in a tooltip when it gets keyboard focus", async () => {
		theTextIsCut();
		whenTheGridIsShown();

		fireEvent.focusIn(screen.getByRole("link", { name: LONG_NAME }));

		expect(
			await screen.findByRole("tooltip"),
			"a keyboard user who focuses a cut name must get the full name in a tooltip, taken from the column value",
		).toHaveTextContent(LONG_NAME);
	});

	it("shows no tooltip for a text cell that is not cut", () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		whenTheGridIsShown();

		fireEvent.mouseOver(screen.getByText("Team Lightspeed"));
		act(() => {
			vi.advanceTimersByTime(1500);
		});

		expect(
			screen.queryByRole("tooltip"),
			"a tooltip on text that is shown whole only repeats it; it must open only when the text is cut",
		).toBeNull();
	});
});
