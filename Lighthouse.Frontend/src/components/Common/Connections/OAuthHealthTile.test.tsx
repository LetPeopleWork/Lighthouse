import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../../services/Api/ApiError";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import {
	createMockApiServiceContext,
	createMockOAuthService,
	createMockRbacService,
} from "../../../tests/MockApiServiceProvider";
import OAuthHealthTile from "./OAuthHealthTile";

const mockIsSystemAdmin = vi.fn();
const mockCanUsePremiumFeatures = vi.fn();

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

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: mockCanUsePremiumFeatures() },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

const renderTile = (oauthService: IOAuthService) => {
	const mockRbacService = createMockRbacService();
	const ctx: IApiServiceContext = createMockApiServiceContext({
		rbacService: mockRbacService,
		oauthService,
	});

	return render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={ctx}>
				<OAuthHealthTile />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
};

describe("OAuthHealthTile", () => {
	beforeEach(() => {
		mockIsSystemAdmin.mockReturnValue(true);
		mockCanUsePremiumFeatures.mockReturnValue(true);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("renders the three KPI rows with values for Premium SystemAdmin when all KPIs are present", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockResolvedValue({
			setupSuccessRate30d: { value: 0.95, unavailableReason: null },
			refreshSuccessRate7d: { value: 0.98, unavailableReason: null },
			staleRefreshFailedCount24h: 0,
			staleRefreshFailedCount7d: 0,
		});

		renderTile(oauthService);

		await waitFor(() => {
			expect(screen.getByText(/Setup success/i)).toBeInTheDocument();
		});
		expect(screen.getByText(/Refresh success/i)).toBeInTheDocument();
		expect(screen.getByText(/Stale RefreshFailed/i)).toBeInTheDocument();
		expect(screen.getByText("95%")).toBeInTheDocument();
		expect(screen.getByText("98%")).toBeInTheDocument();
		expect(screen.getByText(/All connections healthy/i)).toBeInTheDocument();
		expect(oauthService.getHealth).toHaveBeenCalledTimes(1);
	});

	it("renders Pending for the two KPIs the event store cannot yet provide while the stale counts still render", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn().mockResolvedValue({
			setupSuccessRate30d: {
				value: null,
				unavailableReason: "event_store_pending",
			},
			refreshSuccessRate7d: {
				value: null,
				unavailableReason: "event_store_pending",
			},
			staleRefreshFailedCount24h: 3,
			staleRefreshFailedCount7d: 5,
		});

		renderTile(oauthService);

		await waitFor(() => {
			expect(screen.getAllByText(/Pending/i).length).toBeGreaterThanOrEqual(2);
		});
		expect(
			screen.getByText(/3 connections require reconnect \(24h\) \/ 5 \(7d\)/i),
		).toBeInTheDocument();
		expect(screen.getAllByText(/Epic #5017/i).length).toBeGreaterThan(0);
	});

	it("shows the Upgrade affordance to a SystemAdmin who is not Premium and does not call the API", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(false);
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn();

		renderTile(oauthService);

		expect(
			screen.getByTestId("oauth-health-tile-upgrade-affordance"),
		).toBeInTheDocument();
		expect(screen.getByText(/Premium feature/i)).toBeInTheDocument();
		expect(oauthService.getHealth).not.toHaveBeenCalled();
	});

	it("shows the Upgrade affordance when the API returns 403 even though the client thought it had Premium", async () => {
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi
			.fn()
			.mockRejectedValue(new ApiError(403, "Forbidden"));

		renderTile(oauthService);

		await waitFor(() => {
			expect(
				screen.getByTestId("oauth-health-tile-upgrade-affordance"),
			).toBeInTheDocument();
		});
	});

	it("renders nothing when the user is not a SystemAdmin", () => {
		mockIsSystemAdmin.mockReturnValue(false);
		const oauthService = createMockOAuthService();
		oauthService.getHealth = vi.fn();

		const { container } = renderTile(oauthService);

		expect(container.firstChild).toBeNull();
		expect(oauthService.getHealth).not.toHaveBeenCalled();
	});
});
