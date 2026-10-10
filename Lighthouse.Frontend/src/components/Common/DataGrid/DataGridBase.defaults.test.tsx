import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	resetTheLayout,
	turnColumn,
} from "../../../tests/WorkItemsDialogTestKit";
import DataGridBase from "./DataGridBase";
import type { ColumnVisibilityModel, DataGridColumn } from "./types";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

beforeEach(() => {
	localStorage.clear();
});

const RENDER_HEAVY = 20000;

interface Member {
	id: number;
	name: string;
	age: number;
	email: string;
	city: string;
}

const columns: DataGridColumn<Member>[] = [
	{ field: "name", headerName: "Name", width: 150, hideable: false },
	{ field: "age", headerName: "Age", width: 90 },
	{ field: "email", headerName: "Email", width: 200 },
	{ field: "city", headerName: "City", width: 120 },
];

const rows: Member[] = [
	{ id: 1, name: "Ada", age: 36, email: "ada@example.com", city: "London" },
];

const STORAGE_KEY = "member-grid";

function showTheGrid(defaults?: ColumnVisibilityModel) {
	return render(
		<DataGridBase
			rows={rows}
			columns={columns}
			storageKey={STORAGE_KEY}
			defaultColumnVisibilityModel={defaults}
		/>,
	);
}

const columnsShown = () =>
	screen
		.getAllByRole("columnheader")
		.map((header) => header.getAttribute("data-field"));

const thePage = () => document.body;

const onlyEmailHidden: ColumnVisibilityModel = { email: false };

describe("a grid given default visibility", () => {
	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"starts with the columns the defaults hide out of sight",
		() => {
			showTheGrid({ email: false, city: false });

			expect(columnsShown()).toEqual(["name", "age"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"remembers a column the user turned on across a reopen",
		async () => {
			const user = userEvent.setup();
			const first = showTheGrid({ email: false, city: false });
			await turnColumn(user, "email", true, thePage);
			first.unmount();

			showTheGrid({ email: false, city: false });

			expect(columnsShown()).toEqual(["name", "age", "email"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"lets a later change to the defaults reach every column the user never touched",
		async () => {
			const user = userEvent.setup();
			const first = showTheGrid(onlyEmailHidden);
			await turnColumn(user, "age", false, thePage);
			first.unmount();

			showTheGrid({ email: true, city: false });

			expect(columnsShown()).toEqual(["name", "email"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"keeps a column the user turned on, even after the defaults start hiding it",
		async () => {
			const user = userEvent.setup();
			const first = showTheGrid(onlyEmailHidden);
			await turnColumn(user, "email", true, thePage);
			first.unmount();

			showTheGrid({ email: false, city: false });

			expect(columnsShown()).toEqual(["name", "age", "email"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"Reset layout lands on the defaults, not on every column shown",
		async () => {
			const user = userEvent.setup();
			showTheGrid(onlyEmailHidden);
			await turnColumn(user, "email", true, thePage);
			await turnColumn(user, "city", false, thePage);

			await resetTheLayout(user, thePage);

			expect(columnsShown()).toEqual(["name", "age", "city"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"keeps a column that cannot be hidden on screen without freezing the other defaults",
		() => {
			const first = showTheGrid({ name: false, email: false });

			expect(columnsShown()).toContain("name");
			first.unmount();

			showTheGrid({ email: true, city: false });

			expect(columnsShown()).toEqual(["name", "age", "email"]);
		},
		RENDER_HEAVY,
	);
});

describe("a grid given no default visibility (every other grid), as today", () => {
	// @us-01 @slice-01b @regression @contract-shape:unbounded-preservation
	it(
		"shows every column",
		() => {
			showTheGrid();

			expect(columnsShown()).toEqual(["name", "age", "email", "city"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @regression @contract-shape:unbounded-preservation
	it(
		"remembers a column the user hid across a reopen",
		async () => {
			const user = userEvent.setup();
			const first = showTheGrid();
			await turnColumn(user, "email", false, thePage);
			first.unmount();

			showTheGrid();

			expect(columnsShown()).toEqual(["name", "age", "city"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @regression @contract-shape:unbounded-preservation
	it(
		"Reset layout shows every column again",
		async () => {
			const user = userEvent.setup();
			showTheGrid();
			await turnColumn(user, "email", false, thePage);

			await resetTheLayout(user, thePage);

			expect(columnsShown()).toEqual(["name", "age", "email", "city"]);
		},
		RENDER_HEAVY,
	);
});
