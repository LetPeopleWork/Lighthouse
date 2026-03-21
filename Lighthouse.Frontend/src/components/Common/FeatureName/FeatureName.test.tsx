import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { describe, expect } from "vitest";
import FeatureName from "./FeatureName";

const renderWithRouter = (ui: React.ReactElement) => {
	return render(<BrowserRouter>{ui}</BrowserRouter>);
};

describe("FeatureName", () => {
	const defaultProps = {
		name: "Test Feature",
		url: "",
	};

	test("renders feature name without link when url is empty", () => {
		renderWithRouter(<FeatureName {...defaultProps} />);

		const nameElement = screen.getByText("Test Feature");
		expect(nameElement).toBeInTheDocument();
		expect(nameElement.tagName).not.toBe("A");
	});

	test("renders feature name with link when url is provided", () => {
		renderWithRouter(<FeatureName {...defaultProps} url="/feature/123" />);

		const linkElement = screen.getByText("Test Feature");
		expect(linkElement.closest("a")).toBeInTheDocument();
		expect(linkElement.closest("a")).toHaveAttribute("href", "/feature/123");
	});
});
