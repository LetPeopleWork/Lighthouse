import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { createPositionColumn } from "./columns";

// Epic 5375 slice 01 — US-01 AC-1.5, AC-1.6, AC-1.8. The cell reports the place the backend gave the
// row across the whole instance; nothing here may derive it from where the row happens to sit.
const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Rebuild the search index",
		forecasts: [],
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

const renderCell = (row: IFeature, headerLabel = "#") => {
	const column = createPositionColumn(headerLabel);
	render(column.renderCell?.({ row, value: row.position }));
};

describe("createPositionColumn", () => {
	it("shows the place the row holds across the whole instance", () => {
		renderCell(feature({ position: 17 }));

		expect(screen.getByText("17")).toBeInTheDocument();
	});

	it("shows a place for a feature that arrived without a rank from the tracker", () => {
		renderCell(feature({ position: 42 }));

		expect(screen.getByText("42")).toBeInTheDocument();
	});

	it("leaves the cell blank rather than printing NaN when the place is missing", () => {
		renderCell(feature());

		expect(screen.queryByText("NaN")).not.toBeInTheDocument();
	});

	it("takes the header label it is given rather than naming the concept itself", () => {
		expect(createPositionColumn("Manual").headerName).toBe("Manual");
	});

	it("stays sortable, so re-sorting the grid never hides the column", () => {
		expect(createPositionColumn("#").sortable).toBe(true);
	});

	it("reads the place off the row, never off where the row sits in the visible list", () => {
		const column = createPositionColumn("#");
		const firstVisibleRow = feature({ id: 9, position: 4 });

		expect(column.valueGetter?.(undefined, firstVisibleRow)).toBe(4);
	});
});
