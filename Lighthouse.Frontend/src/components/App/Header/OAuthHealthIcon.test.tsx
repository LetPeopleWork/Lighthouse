import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import {
	createMockApiServiceContext,
	createMockOAuthService,
} from "../../../tests/MockApiServiceProvider";
import OAuthHealthIcon from "./OAuthHealthIcon";

const mockIsSystemAdmin = vi.fn();

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: false,
		isSystemAdmin: mockIsSystemAdmin(),
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: () => true,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: false,
			isSystemAdmin: mockIsSystemAdmin(),
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

const renderIcon = (oauthService: IOAuthService) => {
	const ctx: IApiServiceContext = createMockApiServiceContext({ oauthService });
	return render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={ctx}>
				<OAuthHealthIcon />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
};

describe("OAuthHealthIcon", () => {
	beforeEach(() => {
		mockIsSystemAdmin.mockReturnValue(true);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("renders nothing when the user is not a system admin", () => {
		mockIsSystemAdmin.mockReturnValue(false);
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn();

		const { container } = renderIcon(oauthService);

		expect(container.firstChild).toBeNull();
		expect(oauthService.getHealth).not.toHaveBeenCalled();
	});

	it("renders nothing when no OAuth connections exist", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockResolvedValue({
			totalOAuthConnections: 0,
			disconnectedCount: 0,
		});

		const { container } = renderIcon(oauthService);

		await waitFor(() => {
			expect(oauthService.getHealth).toHaveBeenCalledTimes(1);
		});
		expect(container.firstChild).toBeNull();
	});

	it("renders the healthy icon when all OAuth connections are healthy", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockResolvedValue({
			totalOAuthConnections: 3,
			disconnectedCount: 0,
		});

		renderIcon(oauthService);

		await waitFor(() => {
			expect(screen.getByTestId("oauth-health-icon")).toBeInTheDocument();
		});
		expect(
			screen.getByLabelText(/All OAuth connections healthy/i),
		).toBeInTheDocument();
	});

	it("renders the warning icon with a count when one or more connections need reconnect", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockResolvedValue({
			totalOAuthConnections: 3,
			disconnectedCount: 2,
		});

		renderIcon(oauthService);

		await waitFor(() => {
			expect(screen.getByTestId("oauth-health-icon")).toBeInTheDocument();
		});
		expect(
			screen.getByLabelText(/2 OAuth connections need reconnect/i),
		).toBeInTheDocument();
	});

	it("uses singular phrasing when exactly one connection needs reconnect", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockResolvedValue({
			totalOAuthConnections: 2,
			disconnectedCount: 1,
		});

		renderIcon(oauthService);

		await waitFor(() => {
			expect(
				screen.getByLabelText(/1 OAuth connection needs reconnect/i),
			).toBeInTheDocument();
		});
	});

	it("renders nothing when the health endpoint errors", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockRejectedValue(new Error("boom"));

		const { container } = renderIcon(oauthService);

		await waitFor(() => {
			expect(oauthService.getHealth).toHaveBeenCalledTimes(1);
		});
		expect(container.firstChild).toBeNull();
	});
});
