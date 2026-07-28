import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { createForecastsColumn } from "./columns";

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Deep Sea Mapping Initiative",
		forecasts: [],
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

const renderCell = (row: IFeature) => {
	const column = createForecastsColumn();
	const cell = column.renderCell?.({ row, value: undefined });

	render(<>{cell}</>);
};

describe("createForecastsColumn", () => {
	it("says it cannot forecast when a contributing team has no throughput", () => {
		renderCell(feature({ teamsWithoutForecast: ["Team Meridian"] }));

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
	});

	it("names the team that could not be forecast", () => {
		renderCell(feature({ teamsWithoutForecast: ["Team Meridian"] }));

		expect(
			screen.getByLabelText(
				"No throughput history for Team Meridian. Forecast unavailable until that team has data.",
			),
		).toBeInTheDocument();
	});

	it("leaves the cell to the forecast list when every team can be forecast", () => {
		renderCell(feature());

		expect(screen.queryByText("Cannot forecast")).not.toBeInTheDocument();
	});

	it("tolerates a backend payload that omits the field", () => {
		renderCell(feature({ teamsWithoutForecast: undefined }));

		expect(screen.queryByText("Cannot forecast")).not.toBeInTheDocument();
	});
});
