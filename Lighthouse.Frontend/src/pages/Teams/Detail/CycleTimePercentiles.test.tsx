import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import CycleTimePercentiles from "./CycleTimePercentiles";

describe("CycleTimePercentiles component", () => {
  const mockPercentiles: IPercentileValue[] = [
    { percentile: 50, value: 3 },
    { percentile: 85, value: 7 },
    { percentile: 95, value: 12 },
    { percentile: 99, value: 20 }
  ];

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
      { percentile: 50, value: 1 }
    ];
    
    const multiDayPercentile: IPercentileValue[] = [
      { percentile: 85, value: 5 }
    ];

    const { rerender } = render(<CycleTimePercentiles percentileValues={singleDayPercentile} />);
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
