import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import EnlargeableChart from "./EnlargeableChart";

const renderProbe = () =>
	render(
		<EnlargeableChart
			ariaLabel="Delivery Burnup"
			render={(height) => <div data-testid="probe">{height}</div>}
		/>,
	);

describe("EnlargeableChart", () => {
	it("renders the chart inline at the normal height", () => {
		renderProbe();

		expect(screen.getByTestId("probe")).toHaveTextContent("320");
	});

	it("offers an enlarge control labelled for the wrapped chart", () => {
		renderProbe();

		expect(
			screen.getByRole("button", { name: "Enlarge Delivery Burnup" }),
		).toBeInTheDocument();
	});

	it("opens a modal that re-renders the chart at the enlarged height when enlarged", () => {
		renderProbe();

		fireEvent.click(
			screen.getByRole("button", { name: "Enlarge Delivery Burnup" }),
		);

		const enlarged = screen.getByTestId("enlarged-Delivery Burnup");
		expect(enlarged).toHaveTextContent("720");
	});

	it("closes the enlarged view when the close control is used", () => {
		renderProbe();

		fireEvent.click(
			screen.getByRole("button", { name: "Enlarge Delivery Burnup" }),
		);
		expect(screen.getByTestId("enlarged-Delivery Burnup")).toBeInTheDocument();

		fireEvent.click(screen.getByRole("button", { name: "Close (Esc)" }));

		expect(
			screen.queryByTestId("enlarged-Delivery Burnup"),
		).not.toBeInTheDocument();
	});
});
