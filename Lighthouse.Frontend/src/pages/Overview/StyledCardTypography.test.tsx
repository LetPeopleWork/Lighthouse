import type { SvgIconComponent } from "@mui/icons-material";
import SvgIcon from "@mui/material/SvgIcon";
import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import StyledCardTypography from "./StyledCardTypography";

describe("StyledCardTypography component", () => {
	const MockIcon: SvgIconComponent = SvgIcon;

	const mockText = "Styled Card Typography";
	const mockChildren = <span>Mock Children</span>;

	// Create a test theme that matches the one used in the app
	const theme = createTheme({
		palette: {
			primary: {
				main: "rgb(25, 118, 210)",
			},
		},
	});

	it("renders text and icon correctly", () => {
		render(
			<ThemeProvider theme={theme}>
				<StyledCardTypography text={mockText} icon={MockIcon} />
			</ThemeProvider>,
		);

		const iconElement = screen.getByTestId("styled-card-icon");
		expect(iconElement).toBeInTheDocument();

		const textElement = screen.getByText(mockText);
		expect(textElement).toBeInTheDocument();
	});

	it("renders text, icon, and children correctly", () => {
		render(
			<ThemeProvider theme={theme}>
				<StyledCardTypography text={mockText} icon={MockIcon}>
					{mockChildren}
				</StyledCardTypography>
			</ThemeProvider>,
		);

		const iconElement = screen.getByTestId("styled-card-icon");
		expect(iconElement).toBeInTheDocument();

		const textElement = screen.getByText(mockText);
		expect(textElement).toBeInTheDocument();

		const childrenElement = screen.getByText("Mock Children");
		expect(childrenElement).toBeInTheDocument();
	});

	it("uses default icon style", () => {
		render(
			<ThemeProvider theme={theme}>
				<StyledCardTypography text={mockText} icon={MockIcon} />
			</ThemeProvider>,
		);

		const iconElement = screen.getByTestId("styled-card-icon");
		expect(iconElement).toHaveStyle("color: rgb(25, 118, 210)");
		expect(iconElement).toHaveStyle("margin-right: 8px");
	});
});
