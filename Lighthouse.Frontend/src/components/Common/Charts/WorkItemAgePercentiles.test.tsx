import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { testTheme } from "../../../tests/testTheme";
import WorkItemAgePercentiles from "./WorkItemAgePercentiles";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

function getMockPercentiles(
	overrides?: Partial<Record<50 | 70 | 85 | 95, number>>,
): IPercentileValue[] {
	const values: Record<50 | 70 | 85 | 95, number> = {
		50: 6,
		70: 11,
		85: 18,
		95: 27,
		...overrides,
	};
	return [
		{ percentile: 50, value: values[50] },
		{ percentile: 70, value: values[70] },
		{ percentile: 85, value: values[85] },
		{ percentile: 95, value: values[95] },
	];
}

describe("WorkItemAgePercentiles component", () => {
	beforeEach(() => {
		vi.resetAllMocks();
	});

	it("renders a distinct Work Item Age Percentiles title, not the Cycle Time card", () => {
		render(<WorkItemAgePercentiles percentileValues={getMockPercentiles()} />);

		expect(screen.getByText("Work Item Age Percentiles")).toBeInTheDocument();
		expect(
			screen.queryByText("Cycle Time Percentiles"),
		).not.toBeInTheDocument();
	});

	it("lists the 50/70/85/95 percentiles in descending order", () => {
		render(<WorkItemAgePercentiles percentileValues={getMockPercentiles()} />);

		const rows = screen.getAllByText(/\d+th/);
		expect(rows.map((r) => r.textContent)).toEqual([
			"95th",
			"85th",
			"70th",
			"50th",
		]);
	});

	it("renders each percentile value with day formatting", () => {
		render(
			<WorkItemAgePercentiles
				percentileValues={getMockPercentiles({ 50: 1 })}
			/>,
		);

		expect(screen.getByText("1 day")).toBeInTheDocument();
		expect(screen.getByText("11 days")).toBeInTheDocument();
		expect(screen.getByText("18 days")).toBeInTheDocument();
		expect(screen.getByText("27 days")).toBeInTheDocument();
	});

	it("renders the graceful empty state when all four percentile values are zero", () => {
		render(
			<WorkItemAgePercentiles
				percentileValues={getMockPercentiles({ 50: 0, 70: 0, 85: 0, 95: 0 })}
			/>,
		);

		expect(screen.getByText("Work Item Age Percentiles")).toBeInTheDocument();
		expect(screen.getByText("No work in progress")).toBeInTheDocument();
		expect(screen.queryByText(/\d+th/)).not.toBeInTheDocument();
	});
});
