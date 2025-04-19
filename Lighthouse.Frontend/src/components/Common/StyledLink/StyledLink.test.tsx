import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import StyledLink from "./StyledLink";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				primary: {
					main: "#1976d2",
				},
				mode: "light",
			},
		}),
	};
});

describe("StyledLink component", () => {
	const renderWithRouter = (ui: React.ReactNode) => {
		return render(<MemoryRouter>{ui}</MemoryRouter>);
	};

	it("renders link with correct text", () => {
		const linkText = "Click me";
		renderWithRouter(<StyledLink to="/test">{linkText}</StyledLink>);

		const link = screen.getByText(linkText);
		expect(link).toBeInTheDocument();
		expect(link.tagName).toBe("A");
		expect(link).toHaveAttribute("href", "/test");
	});

	it("renders with default body2 variant", () => {
		renderWithRouter(<StyledLink to="/test">Default variant</StyledLink>);

		const link = screen.getByText("Default variant");
		expect(link).toHaveClass("MuiTypography-body2");
	});

	it("renders with specified variant", () => {
		renderWithRouter(
			<StyledLink to="/test" variant="subtitle1">
				Custom variant
			</StyledLink>,
		);

		const link = screen.getByText("Custom variant");
		expect(link).toHaveClass("MuiTypography-subtitle1");
	});

	it("applies custom className when provided", () => {
		const customClass = "custom-class";
		renderWithRouter(
			<StyledLink to="/test" className={customClass}>
				With custom class
			</StyledLink>,
		);

		const link = screen.getByText("With custom class");
		expect(link).toHaveClass(customClass);
	});

	it("formats as a Typography component that is a Link", () => {
		renderWithRouter(<StyledLink to="/test">Typography link</StyledLink>);

		const link = screen.getByText("Typography link");
		expect(link).toHaveClass("MuiTypography-root");
	});
});
