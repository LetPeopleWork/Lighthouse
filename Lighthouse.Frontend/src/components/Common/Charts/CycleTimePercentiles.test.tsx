import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { testTheme } from "../../../tests/testTheme";
import CycleTimePercentiles from "./CycleTimePercentiles";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

describe("CycleTimePercentiles component", () => {
	const mockPercentiles: IPercentileValue[] = [
		{ percentile: 50, value: 3 },
		{ percentile: 85, value: 7 },
		{ percentile: 95, value: 12 },
		{ percentile: 99, value: 20 },
	];

	// Clear mocks between tests
	beforeEach(() => {
		vi.resetAllMocks();
	});

	it("should render with title and percentile data", () => {
		render(<CycleTimePercentiles percentileValues={mockPercentiles} />);

		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
		expect(screen.getByText("50th")).toBeInTheDocument();
		expect(screen.getByText("85th")).toBeInTheDocument();
		expect(screen.getByText("95th")).toBeInTheDocument();
		expect(screen.getByText("99th")).toBeInTheDocument();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		render(<CycleTimePercentiles percentileValues={[]} />);

		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should display percentiles in descending order", () => {
		render(<CycleTimePercentiles percentileValues={mockPercentiles} />);

		const percentileElements = screen.getAllByText(/\d+th/);
		expect(percentileElements[0].textContent).toBe("99th");
		expect(percentileElements[1].textContent).toBe("95th");
		expect(percentileElements[2].textContent).toBe("85th");
		expect(percentileElements[3].textContent).toBe("50th");
	});

	it("should format days correctly for singular and plural values", () => {
		const singleDayPercentile: IPercentileValue[] = [
			{ percentile: 50, value: 1 },
		];

		const multiDayPercentile: IPercentileValue[] = [
			{ percentile: 85, value: 5 },
		];

		const { rerender } = render(
			<CycleTimePercentiles percentileValues={singleDayPercentile} />,
		);
		expect(screen.getByText("1 day")).toBeInTheDocument();

		rerender(<CycleTimePercentiles percentileValues={multiDayPercentile} />);
		expect(screen.getByText("5 days")).toBeInTheDocument();
	});

	it("should display different colors based on percentile levels", () => {
		render(<CycleTimePercentiles percentileValues={mockPercentiles} />);

		// We can't directly test the colors in this test environment,
		// but we can verify that the component renders without errors
		// and all percentiles are displayed with their values
		expect(screen.getByText("3 days")).toBeInTheDocument();
		expect(screen.getByText("7 days")).toBeInTheDocument();
		expect(screen.getByText("12 days")).toBeInTheDocument();
		expect(screen.getByText("20 days")).toBeInTheDocument();
	});
});

describe("CycleTimePercentiles named cycle time selection", () => {
	const mockPercentiles: IPercentileValue[] = [
		{ percentile: 50, value: 3 },
		{ percentile: 85, value: 7 },
		{ percentile: 95, value: 12 },
	];

	const definitions: INamedCycleTimeDefinition[] = [
		{ id: 10, name: "Concept to Cash", isValid: true },
		{ id: 11, name: "Idea to Live", isValid: true },
	];

	const NEUTRAL_TIP =
		"SLE applies to the Default Cycle Time. Named Cycle Times have no SLE target.";

	beforeEach(() => {
		vi.resetAllMocks();
	});

	it("renders a selector defaulting to Default and listing the named definitions", async () => {
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={definitions}
				scopeDefinitionId={null}
				onScopeChange={vi.fn()}
			/>,
		);

		const selector = screen.getByRole("combobox", { name: "Cycle time scope" });
		expect(selector).toHaveTextContent("Default");

		await userEvent.click(selector);
		expect(
			screen.getByRole("option", { name: "Concept to Cash" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("option", { name: "Idea to Live" }),
		).toBeInTheDocument();
	});

	it("renders no selector when there are no named definitions", () => {
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={[]}
				scopeDefinitionId={null}
				onScopeChange={vi.fn()}
			/>,
		);

		expect(
			screen.queryByRole("combobox", { name: "Cycle time scope" }),
		).not.toBeInTheDocument();
		expect(screen.getByText("Cycle Time Percentiles")).toBeInTheDocument();
	});

	it("renders no selector when the definitions prop is omitted (default path unchanged)", () => {
		render(<CycleTimePercentiles percentileValues={mockPercentiles} />);

		expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
	});

	it("calls onScopeChange with the definition id when a named definition is selected", async () => {
		const onScopeChange = vi.fn();
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={definitions}
				scopeDefinitionId={null}
				onScopeChange={onScopeChange}
			/>,
		);

		await userEvent.click(
			screen.getByRole("combobox", { name: "Cycle time scope" }),
		);
		await userEvent.click(
			screen.getByRole("option", { name: "Concept to Cash" }),
		);

		expect(onScopeChange).toHaveBeenCalledWith(10);
	});

	it("calls onScopeChange with null when Default is re-selected", async () => {
		const onScopeChange = vi.fn();
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={definitions}
				scopeDefinitionId={10}
				onScopeChange={onScopeChange}
			/>,
		);

		await userEvent.click(
			screen.getByRole("combobox", { name: "Cycle time scope" }),
		);
		await userEvent.click(screen.getByRole("option", { name: "Default" }));

		expect(onScopeChange).toHaveBeenCalledWith(null);
	});

	it("shows the neutral SLE tip when a named definition is selected", () => {
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={definitions}
				scopeDefinitionId={10}
				onScopeChange={vi.fn()}
			/>,
		);

		expect(screen.getByText(NEUTRAL_TIP)).toBeInTheDocument();
	});

	it("does not show the neutral tip on the Default selection", () => {
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={definitions}
				scopeDefinitionId={null}
				onScopeChange={vi.fn()}
			/>,
		);

		expect(screen.queryByText(NEUTRAL_TIP)).not.toBeInTheDocument();
	});

	it("self-resets to Default when the selected definition becomes invalid", () => {
		const onScopeChange = vi.fn();
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={[
					{ id: 10, name: "Concept to Cash", isValid: false },
				]}
				scopeDefinitionId={10}
				onScopeChange={onScopeChange}
			/>,
		);

		expect(onScopeChange).toHaveBeenCalledWith(null);
	});

	it("self-resets to Default when the selected definition is absent", () => {
		const onScopeChange = vi.fn();
		render(
			<CycleTimePercentiles
				percentileValues={mockPercentiles}
				namedCycleTimeDefinitions={[
					{ id: 11, name: "Idea to Live", isValid: true },
				]}
				scopeDefinitionId={10}
				onScopeChange={onScopeChange}
			/>,
		);

		expect(onScopeChange).toHaveBeenCalledWith(null);
	});
});
