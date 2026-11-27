import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DataGridBase from "./DataGridBase";
import type { DataGridColumn } from "./types";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

interface Listed {
	id: number;
	name: string;
}

const columns: DataGridColumn<Listed>[] = [
	{ field: "name", headerName: "Name", width: 150 },
];

const listWith = (onTextChange: (text: string) => void) => (
	<DataGridBase
		rows={[{ id: 1, name: "Alpha" }]}
		columns={columns}
		storageKey="search-owner-list"
		search={{ placeholder: "Search Rows", onTextChange }}
	/>
);

describe("DataGridBase search owner", () => {
	it("tells the owner it has now, not the one it had before", async () => {
		const first = vi.fn();
		const second = vi.fn();
		const { rerender } = render(listWith(first));

		rerender(listWith(second));
		await userEvent.type(
			screen.getByRole("textbox", { name: "Search Rows" }),
			"a",
		);

		expect(second).toHaveBeenCalledWith("a");
		expect(first).not.toHaveBeenCalled();
	});
});
