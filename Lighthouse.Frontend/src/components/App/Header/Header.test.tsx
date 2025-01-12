import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import Header from "./Header";

describe("Header component", () => {
	it("should render the LighthouseLogo", () => {
		render(
			<MemoryRouter>
				<Header />
			</MemoryRouter>,
		);
		const logo = screen.getByTestId("CellTowerIcon"); // Assuming the logo has this test id
		expect(logo).toBeInTheDocument();
	});

	it("should render navigation items", () => {
		render(
			<MemoryRouter>
				<Header />
			</MemoryRouter>,
		);

		expect(screen.getByText("Overview")).toBeInTheDocument();
		expect(screen.getByText("Teams")).toBeInTheDocument();
		expect(screen.getByText("Projects")).toBeInTheDocument();
		expect(screen.getByText("Settings")).toBeInTheDocument();
	});

	it("should render external link buttons with correct links", () => {
		render(
			<MemoryRouter>
				<Header />
			</MemoryRouter>,
		);

		const contributorsLink = screen.getByTestId(
			"https://github.com/LetPeopleWork/Lighthouse/blob/main/CONTRIBUTORS.md",
		);
		const bugReportLink = screen.getByTestId(
			"https://github.com/LetPeopleWork/Lighthouse/issues",
		);
		const documentationLink = screen.getByTestId(
			"https://docs.lighthouse.letpeople.work",
		);

		expect(contributorsLink).toHaveAttribute(
			"href",
			"https://github.com/LetPeopleWork/Lighthouse/blob/main/CONTRIBUTORS.md",
		);
		expect(bugReportLink).toHaveAttribute(
			"href",
			"https://github.com/LetPeopleWork/Lighthouse/issues",
		);
		expect(documentationLink).toHaveAttribute(
			"href",
			"https://docs.lighthouse.letpeople.work",
		);
	});
});
