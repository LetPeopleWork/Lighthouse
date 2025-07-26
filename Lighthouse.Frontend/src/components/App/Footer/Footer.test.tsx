import { render, screen } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router-dom";
import Footer from "./Footer";

vi.mock("../LetPeopleWork/LighthouseVersion", () => ({
	default: () => <span data-testid="version">LighthouseVersion</span>,
}));

describe("Footer component", () => {
	it("renders LetPeopleWorkLogo and LighthouseVersion components", async () => {
		render(
			<Router>
				<Footer />
			</Router>,
		);

		const letPeopleWorkLogo = screen.getByRole("img", {
			name: "Let People Work Logo",
		});
		expect(letPeopleWorkLogo).toBeInTheDocument();

		const version = screen.getByTestId("version");
		expect(version).toBeInTheDocument();
	});

	it("renders donation button with correct link", async () => {
		render(
			<Router>
				<Footer />
			</Router>,
		);

		const donationButton = screen.getByLabelText("Support Our Work");
		expect(donationButton).toBeInTheDocument();
		expect(donationButton).toHaveAttribute(
			"href",
			"https://ko-fi.com/letpeoplework",
		);
		expect(donationButton).toHaveAttribute("target", "_blank");
		expect(donationButton).toHaveAttribute("rel", "noopener noreferrer");
	});
});
