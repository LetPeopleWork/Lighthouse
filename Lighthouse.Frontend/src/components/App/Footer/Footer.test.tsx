import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter as Router } from "react-router";
import { UsageDataConsentProvider } from "../../../hooks/useUsageDataConsent";
import Footer from "./Footer";

vi.mock("../LetPeopleWork/LighthouseVersion", () => ({
	default: () => <span data-testid="version">LighthouseVersion</span>,
}));

describe("Footer component", () => {
	it("renders LetPeopleWorkLogo and LighthouseVersion components", async () => {
		render(
			<Router>
				<UsageDataConsentProvider>
					<Footer />
				</UsageDataConsentProvider>
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
				<UsageDataConsentProvider>
					<Footer />
				</UsageDataConsentProvider>
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

	// The dialog deliberately carries no list of what is collected. That list lives on the page this
	// link points at, where it can grow as the feature does without going stale behind somebody's
	// back - which makes the link the whole of the disclosure, and an address that had gone missing a
	// dialog asking people to decide with nothing to read. The address is written out here rather
	// than imported, because importing it would assert only that the code equals itself.
	it("points the usage data dialog at the published page", async () => {
		render(
			<Router>
				<UsageDataConsentProvider>
					<Footer />
				</UsageDataConsentProvider>
			</Router>,
		);

		await userEvent.click(screen.getByTestId("usage-data-indicator"));

		expect(
			screen.getByRole("link", { name: /read the full usage data page/i }),
		).toHaveAttribute(
			"href",
			"https://docs.lighthouse.letpeople.work/settings/usagedata.html",
		);
	});
});
