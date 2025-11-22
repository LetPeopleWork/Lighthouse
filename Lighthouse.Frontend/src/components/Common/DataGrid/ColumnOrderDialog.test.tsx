import type { GridColDef } from "@mui/x-data-grid";
import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ColumnOrderDialog from "./ColumnOrderDialog";

describe("ColumnOrderDialog", () => {
	const columns: GridColDef[] = [
		{ field: "id", headerName: "ID" },
		{ field: "name", headerName: "Name" },
		{ field: "age", headerName: "Age" },
	];

	it("renders header name only and not the field key", () => {
		const onClose = vi.fn();
		const onSave = vi.fn();

		render(
			<ColumnOrderDialog
				open={true}
				onClose={onClose}
				columns={columns}
				columnOrder={["id", "name", "age"]}
				onSave={onSave}
			/>,
		);

		// Title is present
		expect(screen.getByText(/Reorder columns/i)).toBeInTheDocument();

		// Header names appear
		expect(screen.getByText("ID")).toBeInTheDocument();
		expect(screen.getByText("Name")).toBeInTheDocument();
		expect(screen.getByText("Age")).toBeInTheDocument();

		// Field keys should not be visible as secondary text
		expect(screen.queryByText("id")).not.toBeInTheDocument();
		expect(screen.queryByText("name")).not.toBeInTheDocument();
		expect(screen.queryByText("age")).not.toBeInTheDocument();
	});

	it("moves columns up and down and calls onSave with new order", async () => {
		const onClose = vi.fn();
		const onSave = vi.fn();

		render(
			<ColumnOrderDialog
				open={true}
				onClose={onClose}
				columns={columns}
				columnOrder={["id", "name", "age"]}
				onSave={onSave}
			/>,
		);

		// Move 'name' up (index 1 -> 0)
		const moveUpName = screen.getByTestId("move-up-name");
		await userEvent.click(moveUpName);

		// Save changes
		const saveButton = screen.getByText("Save");
		await userEvent.click(saveButton);

		// onSave should be called with new order
		expect(onSave).toHaveBeenCalled();
		const [[newOrder]] = onSave.mock.calls as unknown as string[][][];
		// The new order should have 'name' before 'id'
		expect(newOrder.indexOf("name")).toBeLessThan(newOrder.indexOf("id"));
	});
});
