import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILicensingService } from "../../../services/Api/LicensingService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import Header from "./Header";

describe("Header component", () => {
	let queryClient: QueryClient;
	let mockLicensingService: ILicensingService;

	beforeEach(() => {
		queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		mockLicensingService = {
			getLicenseStatus: vi.fn().mockResolvedValue({
				hasLicense: true,
				isValid: true,
			}),
			importLicense: vi.fn(),
		};
	});

	const renderHeader = () => {
		const mockApiContext = createMockApiServiceContext({
			licensingService: mockLicensingService,
		});

		return render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiContext}>
					<MemoryRouter>
						<Header />
					</MemoryRouter>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);
	};
	it("should render the LighthouseLogo", () => {
		renderHeader();
		const logo = screen.getByAltText("Lighthouse logo");
		expect(logo).toBeInTheDocument();
		expect(logo).toHaveAttribute("src", "/icons/icon-512x512.png");
	});

	it("should render navigation items", () => {
		renderHeader();

		expect(screen.getByText("Overview")).toBeInTheDocument();
		expect(screen.getByText("Teams")).toBeInTheDocument();
		expect(screen.getByText("Projects")).toBeInTheDocument();
		expect(screen.getByText("Settings")).toBeInTheDocument();
	});

	it("should render external link buttons and feedback button with correct behavior", async () => {
		const user = userEvent.setup();
		renderHeader();

		const contributorsLink = screen.getByTestId(
			"https://docs.lighthouse.letpeople.work/contributions/contributions.html",
		);
		const feedbackButton = screen.getByTestId("feedback-button");
		const documentationLink = screen.getByTestId(
			"https://docs.lighthouse.letpeople.work",
		);

		expect(contributorsLink).toHaveAttribute(
			"href",
			"https://docs.lighthouse.letpeople.work/contributions/contributions.html",
		);
		expect(feedbackButton).toBeInTheDocument();
		expect(feedbackButton).toHaveAttribute("aria-label", "Provide Feedback");
		expect(documentationLink).toHaveAttribute(
			"href",
			"https://docs.lighthouse.letpeople.work",
		);

		// Test feedback dialog opens when button is clicked
		await user.click(feedbackButton);
		expect(screen.getByText("We'd Love to Hear from You!")).toBeInTheDocument();
	});
});
