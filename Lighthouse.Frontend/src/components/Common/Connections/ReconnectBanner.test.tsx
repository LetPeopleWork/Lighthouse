import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import ReconnectBanner from "./ReconnectBanner";

const openOAuthPopupMock = vi.fn();

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

const createMockOAuthService = (): IOAuthService => ({
	initiateConnect: vi.fn(),
	disconnect: vi.fn(),
	getHealth: vi.fn(),
});

const getMockConnection = (
	overrides: Partial<IWorkTrackingSystemConnection> = {},
): IWorkTrackingSystemConnection => {
	const base = new WorkTrackingSystemConnection({
		id: 42,
		name: "My Jira",
		workTrackingSystem: "Jira",
		options: [],
		authenticationMethodKey: "jira.oauth",
	});
	return Object.assign(base, overrides);
};

const renderBanner = (props: {
	connection: IWorkTrackingSystemConnection;
	oauthService?: IOAuthService;
	onReconnected?: () => void;
}) => {
	const oauthService = props.oauthService ?? createMockOAuthService();
	const mockApiServiceContext = createMockApiServiceContext({ oauthService });

	return {
		oauthService,
		...render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<ReconnectBanner
					connection={props.connection}
					onReconnected={props.onReconnected}
				/>
			</ApiServiceContext.Provider>,
		),
	};
};

describe("ReconnectBanner", () => {
	beforeEach(() => {
		openOAuthPopupMock.mockReset();
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("renders the warning Alert with the required US-02 AC #3 copy when requiresReconnect is true", () => {
		renderBanner({
			connection: getMockConnection({ requiresReconnect: true }),
		});

		const alert = screen.getByRole("alert");
		expect(alert).toBeInTheDocument();
		expect(alert).toHaveTextContent(
			"Reconnect required — the OAuth refresh token is no longer valid",
		);
		expect(alert.className).toMatch(/MuiAlert-colorWarning/);
		expect(
			screen.getByRole("button", { name: /Reconnect/i }),
		).toBeInTheDocument();
	});

	it.each([
		["false", false],
		["undefined", undefined],
		["null", null],
	])("renders nothing when requiresReconnect is %s", (_label, requiresReconnectValue) => {
		renderBanner({
			connection: getMockConnection({
				requiresReconnect: requiresReconnectValue as unknown as
					| boolean
					| undefined,
			}),
		});

		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /Reconnect/i }),
		).not.toBeInTheDocument();
	});

	it("on Reconnect click, calls disconnect THEN initiateConnect with the provider key + connection id, then opens the OAuth popup with the authorization URL", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		const callOrder: string[] = [];
		vi.mocked(oauthService.disconnect).mockImplementation(async () => {
			callOrder.push("disconnect");
		});
		vi.mocked(oauthService.initiateConnect).mockImplementation(async () => {
			callOrder.push("initiateConnect");
			return {
				authorizationUrl: "https://auth.atlassian.com/authorize?state=z",
			};
		});
		openOAuthPopupMock.mockResolvedValue({
			status: "success",
			connectionId: 42,
		});

		renderBanner({
			connection: getMockConnection({
				id: 42,
				authenticationMethodKey: "jira.oauth",
				requiresReconnect: true,
			}),
			oauthService,
		});

		await user.click(screen.getByRole("button", { name: /Reconnect/i }));

		await waitFor(() => {
			expect(oauthService.disconnect).toHaveBeenCalledWith("jira.oauth", 42);
			expect(oauthService.initiateConnect).toHaveBeenCalledWith(
				"jira.oauth",
				42,
			);
		});

		expect(callOrder).toEqual(["disconnect", "initiateConnect"]);

		await waitFor(() => {
			expect(openOAuthPopupMock).toHaveBeenCalledWith(
				"https://auth.atlassian.com/authorize?state=z",
			);
		});
	});

	it("invokes onReconnected exactly once after the OAuth popup resolves with success", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.disconnect).mockResolvedValue(undefined);
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=z",
		});
		openOAuthPopupMock.mockResolvedValue({
			status: "success",
			connectionId: 42,
		});
		const onReconnected = vi.fn();

		renderBanner({
			connection: getMockConnection({ requiresReconnect: true }),
			oauthService,
			onReconnected,
		});

		await user.click(screen.getByRole("button", { name: /Reconnect/i }));

		await waitFor(() => {
			expect(onReconnected).toHaveBeenCalledTimes(1);
		});
	});

	it("keeps the banner visible and surfaces an inline alert when the OAuth popup is blocked by the browser", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.disconnect).mockResolvedValue(undefined);
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=z",
		});
		openOAuthPopupMock.mockResolvedValue({ status: "popup_blocked" });
		const onReconnected = vi.fn();

		renderBanner({
			connection: getMockConnection({ requiresReconnect: true }),
			oauthService,
			onReconnected,
		});

		await user.click(screen.getByRole("button", { name: /Reconnect/i }));

		await waitFor(() => {
			expect(screen.getByText(/blocked the OAuth popup/i)).toBeInTheDocument();
		});

		expect(onReconnected).not.toHaveBeenCalled();
		expect(
			screen.getByText(
				/Reconnect required — the OAuth refresh token is no longer valid/i,
			),
		).toBeInTheDocument();
		expect(screen.getByRole("button", { name: /Reconnect/i })).toBeEnabled();
	});

	it("keeps the banner visible and surfaces an inline notice when the user cancels the OAuth popup", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.disconnect).mockResolvedValue(undefined);
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=z",
		});
		openOAuthPopupMock.mockResolvedValue({ status: "cancelled" });
		const onReconnected = vi.fn();

		renderBanner({
			connection: getMockConnection({ requiresReconnect: true }),
			oauthService,
			onReconnected,
		});

		await user.click(screen.getByRole("button", { name: /Reconnect/i }));

		await waitFor(() => {
			expect(screen.getByText(/cancelled/i)).toBeInTheDocument();
		});

		expect(onReconnected).not.toHaveBeenCalled();
		expect(screen.getByRole("button", { name: /Reconnect/i })).toBeEnabled();
	});

	it("surfaces the IdP error reason inline when the OAuth popup resolves with error", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.disconnect).mockResolvedValue(undefined);
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=z",
		});
		openOAuthPopupMock.mockResolvedValue({
			status: "error",
			reason: "invalid_grant",
		});
		const onReconnected = vi.fn();

		renderBanner({
			connection: getMockConnection({ requiresReconnect: true }),
			oauthService,
			onReconnected,
		});

		await user.click(screen.getByRole("button", { name: /Reconnect/i }));

		await waitFor(() => {
			expect(screen.getByText(/invalid_grant/i)).toBeInTheDocument();
		});

		expect(onReconnected).not.toHaveBeenCalled();
	});

	it("disables the Reconnect button while the disconnect-then-reconnect flow is in flight", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.disconnect).mockResolvedValue(undefined);
		const pending = new Promise<{ authorizationUrl: string }>(() => {
			// never resolves while the test runs
		});
		vi.mocked(oauthService.initiateConnect).mockReturnValue(pending);

		renderBanner({
			connection: getMockConnection({ requiresReconnect: true }),
			oauthService,
		});

		const button = screen.getByRole("button", { name: /Reconnect/i });
		expect(button).not.toBeDisabled();

		await user.click(button);

		await waitFor(() => {
			expect(button).toBeDisabled();
		});
	});
});
