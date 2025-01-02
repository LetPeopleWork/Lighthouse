import { render, screen } from "@testing-library/react";
import LighthouseLogo from "./LighthouseLogo";

describe("LighthouseLogo component", () => {
	it("renders the logo text and icon", () => {
		render(<LighthouseLogo />);

		// Test for the CellTowerIcon
		const cellTowerIcon = screen.getByTestId("CellTowerIcon");
		expect(cellTowerIcon).toBeInTheDocument();

		// Test for the text "Light" and "house"
		const lightText = screen.getByText("Light", { exact: false }); // Using exact: false to match part of the text
		expect(lightText).toBeInTheDocument();

		const houseText = screen.getByText("house", { exact: false }); // Using exact: false to match part of the text
		expect(houseText).toBeInTheDocument();
	});

	it("has correct styles applied", () => {
		render(<LighthouseLogo />);

		// Test styles for Typography components
		const lightText = screen.getByText("Light", { exact: false });
		expect(lightText).toHaveStyle("fontFamily: Quicksand, sans-serif");
		expect(lightText).toHaveStyle("color: rgba(48, 87, 78, 1)");
		expect(lightText).toHaveStyle("fontWeight: bold");

		const houseText = screen.getByText("house", { exact: false });
		expect(houseText).toHaveStyle("fontFamily: Quicksand, sans-serif");
		expect(houseText).toHaveStyle("color: rgb(0, 0, 0)");
		expect(houseText).toHaveStyle("fontWeight: bold");
	});
});
