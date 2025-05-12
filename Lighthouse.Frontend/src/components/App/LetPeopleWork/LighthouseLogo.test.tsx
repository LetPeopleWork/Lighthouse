import { ThemeProvider, createTheme } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import LighthouseLogo from "./LighthouseLogo";

describe("LighthouseLogo component", () => {
	// Create a theme for testing that matches the application theme
	const theme = createTheme({
		palette: {
			primary: {
				main: "rgb(25, 118, 210)",
			},
			mode: "light",
		},
	});

	const renderWithProviders = () => {
		return render(
			<BrowserRouter>
				<ThemeProvider theme={theme}>
					<LighthouseLogo />
				</ThemeProvider>
			</BrowserRouter>,
		);
	};

	it("renders the logo text and icon", () => {
		renderWithProviders();

		// Test for the logo image
		const logoImage = screen.getByAltText("Lighthouse logo");
		expect(logoImage).toBeInTheDocument();
		expect(logoImage).toHaveAttribute("src", "/icons/icon-512x512.png");

		// Test for the text "Light" and "house"
		const lightText = screen.getByText("Light");
		expect(lightText).toBeInTheDocument();

		const houseText = screen.getByText("house");
		expect(houseText).toBeInTheDocument();
	});

	it("has correct styles applied", () => {
		renderWithProviders();

		// Test styles for Typography components
		const lightText = screen.getByText("Light");
		expect(lightText.parentElement).toHaveStyle(
			"font-family: Quicksand,sans-serif",
		);
		expect(lightText).toHaveStyle(`color: ${theme.palette.primary.main}`);

		const houseText = screen.getByText("house");
		expect(houseText.parentElement).toHaveStyle(
			"font-family: Quicksand,sans-serif",
		);
		expect(houseText).toHaveStyle("color: rgb(0, 0, 0, 0.87)");
	});
});
