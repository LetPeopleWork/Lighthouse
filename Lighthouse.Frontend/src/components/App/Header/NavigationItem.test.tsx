import { createTheme, ThemeProvider } from "@mui/material";
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import NavigationItem from "./NavigationItem";

describe("NavigationItem component", () => {
	const theme = createTheme();

	it("should render with the correct text", () => {
		const { getByText } = render(
			<ThemeProvider theme={theme}>
				<MemoryRouter>
					<NavigationItem path="/home" text="Home" />
				</MemoryRouter>
			</ThemeProvider>,
		);
		expect(getByText("Home")).toBeTruthy();
	});

	it("should have the correct link path", () => {
		const { getByText } = render(
			<ThemeProvider theme={theme}>
				<MemoryRouter>
					<NavigationItem path="/home" text="Home" />
				</MemoryRouter>
			</ThemeProvider>,
		);
		const linkElement = getByText("Home").closest("a");
		expect(linkElement).toHaveAttribute("href", "/home");
	});

	it('should have "nav-item" class by default', () => {
		const { getByText } = render(
			<ThemeProvider theme={theme}>
				<MemoryRouter>
					<NavigationItem path="/home" text="Home" />
				</MemoryRouter>
			</ThemeProvider>,
		);
		const linkElement = getByText("Home").closest("a");
		expect(linkElement).toHaveClass("nav-item");
	});

	it('should have "nav-item nav-active" class when active', () => {
		const { getByText } = render(
			<ThemeProvider theme={theme}>
				<MemoryRouter initialEntries={["/home"]}>
					<NavigationItem path="/home" text="Home" />
				</MemoryRouter>
			</ThemeProvider>,
		);
		const linkElement = getByText("Home").closest("a");
		expect(linkElement).toHaveClass("nav-item nav-active");
	});

	it('should not have "nav-active" class when not active', () => {
		const { getByText } = render(
			<ThemeProvider theme={theme}>
				<MemoryRouter initialEntries={["/"]}>
					<NavigationItem path="/home" text="Home" />
				</MemoryRouter>
			</ThemeProvider>,
		);
		const linkElement = getByText("Home").closest("a");
		expect(linkElement).not.toHaveClass("nav-active");
	});
});
