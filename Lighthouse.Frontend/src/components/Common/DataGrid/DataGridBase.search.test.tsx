import { render, screen, waitFor } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DataGridBase from "./DataGridBase";
import type { DataGridColumn } from "./types";

/**
 * A list that is given a search gets a box in its toolbar that narrows the rows by what its columns
 * show; a list that is not given one gets no box. These run the real MUI grid.
 */

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

interface ListedItem {
	id: number;
	name: string;
	stage: string;
	weight: number;
	owner: { team: string };
	urgent: boolean;
}

const columns: DataGridColumn<ListedItem>[] = [
	{ field: "name", headerName: "Name", width: 150 },
	{ field: "stage", headerName: "Stage", width: 120 },
	{
		field: "weight",
		headerName: "Weight",
		width: 100,
		valueFormatter: (value: number) => `${value} pts`,
	},
	{ field: "owner", headerName: "Owner", width: 100 },
	{ field: "urgent", headerName: "Urgent", type: "boolean", width: 100 },
];

const aRow = (id: number, overrides: Partial<ListedItem> = {}): ListedItem => ({
	id,
	name: `Row ${String(id).padStart(3, "0")}`,
	stage: "Doing",
	weight: 3,
	owner: { team: "Zeta" },
	urgent: false,
	...overrides,
});

const threeRows = [
	aRow(1, { name: "Alpha", stage: "Doing", weight: 8 }),
	aRow(2, { name: "Beta", stage: "Review", weight: 5 }),
	aRow(3, { name: "Alphabet", stage: "Review", weight: 2 }),
];

const PLACEHOLDER = "Search Rows";

const showTheList = (
	rows: ListedItem[],
	search?: { placeholder: string; onTextChange?: (text: string) => void },
) => {
	render(
		<DataGridBase
			rows={rows}
			columns={columns}
			storageKey="searched-list"
			search={search}
		/>,
	);
	return userEvent.setup();
};

const theBox = () => screen.getByRole("textbox", { name: PLACEHOLDER });

const namesShown = () =>
	[...document.querySelectorAll('[role="row"]')].flatMap((row) => {
		const cell = row.querySelector('[role="gridcell"][data-field="name"]');
		return cell === null ? [] : [cell.textContent ?? ""];
	});

const whenTheyType = async (user: UserEvent, text: string) => {
	await user.type(theBox(), text);
};

const thenOnlyTheseAreShown = async (names: string[]) => {
	await waitFor(() =>
		expect(
			namesShown(),
			`the list must show exactly ${JSON.stringify(names)} for the typed search`,
		).toEqual(names),
	);
};

beforeEach(() => {
	localStorage.clear();
});

describe("DataGridBase with a search", () => {
	it("shows no search box when it is not given a search", () => {
		showTheList(threeRows);

		expect(
			screen.queryAllByRole("textbox"),
			"a list that was not given a search must offer no text box; only the lists that opt in gain one",
		).toHaveLength(0);
	});

	it("narrows the rows by the text a column shows rather than the value behind it", async () => {
		const user = showTheList(threeRows, { placeholder: PLACEHOLDER });

		await whenTheyType(user, "8 PTS");

		await thenOnlyTheseAreShown(["Alpha"]);
	});

	it.each(["team", "true", "false"])(
		"finds nothing through a column that holds no text when %s is typed",
		async (typed) => {
			const user = showTheList(threeRows, { placeholder: PLACEHOLDER });

			await whenTheyType(user, typed);

			await thenOnlyTheseAreShown([]);
		},
	);

	it("tells its owner the text as it is typed and cleared", async () => {
		const onTextChange = vi.fn();
		const user = showTheList(threeRows, {
			placeholder: PLACEHOLDER,
			onTextChange,
		});

		await whenTheyType(user, "al");
		await user.click(screen.getByRole("button", { name: /clear/i }));

		expect(
			onTextChange.mock.calls.map(([text]) => text),
			"the owner must hear the text after every keystroke, then the empty text once the box is cleared",
		).toEqual(["a", "al", ""]);
	});
});
