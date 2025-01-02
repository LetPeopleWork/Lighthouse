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
});
