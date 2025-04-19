import type { SvgIconComponent } from "@mui/icons-material";
import BugReportIcon from "@mui/icons-material/BugReport";
import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ExternalLinkButton from "./ExternalLinkButton";

// Mock the useTheme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				primary: {
					main: "rgba(48, 87, 78, 1)"
				},
				mode: "light"
			}
		})
	};
});

describe("ExternalLinkButton component", () => {
	const renderComponent = (link: string, icon: SvgIconComponent) =>
		render(<ExternalLinkButton link={link} icon={icon} tooltip="" />);

	it("should render with the correct href", () => {
		const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
		const { container } = renderComponent(link, BugReportIcon);
		const button = container.querySelector("a");
		expect(button).toHaveAttribute("href", link);
	});

	it("should open link in a new tab", () => {
		const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
		const { container } = renderComponent(link, BugReportIcon);
		const button = container.querySelector("a");
		expect(button).toHaveAttribute("target", "_blank");
	});

	it('should have rel="noopener noreferrer"', () => {
		const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
		const { container } = renderComponent(link, BugReportIcon);
		const button = container.querySelector("a");
		expect(button).toHaveAttribute("rel", "noopener noreferrer");
	});

	it("should render the correct icon", () => {
		const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
		const { container } = renderComponent(link, BugReportIcon);
		const icon = container.querySelector("svg");
		expect(icon).toBeTruthy();
	});

	it("should apply the correct color style to the icon", () => {
		const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
		const { container } = renderComponent(link, BugReportIcon);
		const icon = container.querySelector("svg");
		expect(icon).toHaveStyle("color: rgba(48, 87, 78, 1)");
	});
});
