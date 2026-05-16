import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import OAuthAuthForm from "./OAuthAuthForm";

const openOAuthPopupMock = vi.fn();

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

const createMockOAuthService = (): IOAuthService => ({
	initiateConnect: vi.fn(),
	disconnect: vi.fn(),
	getHealth: vi.fn(),
});

const renderForm = (props: {
	connectionId: number;
	providerKey: string;
	baseUrl: string | null;
	oauthService: IOAuthService;
	onConnect?: () => void;
}) => {
	const mockApiServiceContext = createMockApiServiceContext({
		oauthService: props.oauthService,
	});

	return render(
		<ApiServiceContext.Provider value={mockApiServiceContext}>
			<OAuthAuthForm
				connectionId={props.connectionId}
				providerKey={props.providerKey}
				baseUrl={props.baseUrl}
				onConnect={props.onConnect}
			/>
		</ApiServiceContext.Provider>,
	);
};

describe("OAuthAuthForm", () => {
	beforeEach(() => {
		openOAuthPopupMock.mockReset();
		Object.defineProperty(window, "location", {
			value: {
				origin: "https://fallback.example.com",
				assign: vi.fn(),
			},
			writable: true,
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("renders a read-only callback URL and a Connect button when baseUrl is set", () => {
		renderForm({
			connectionId: 7,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService: createMockOAuthService(),
		});

		const callback = screen.getByLabelText<HTMLInputElement>("Callback URL");
		expect(callback).toHaveAttribute("readonly");
		expect(callback.value).toBe(
			"https://lighthouse.example.com/api/oauth/callback",
		);

		expect(
			screen.getByRole("button", { name: /Connect/i }),
		).toBeInTheDocument();

		expect(screen.queryByLabelText("Client ID")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("Client Secret")).not.toBeInTheDocument();
		expect(
			screen.queryByText(/Your callback URL may be incorrect/i),
		).not.toBeInTheDocument();
	});

	it("renders the BaseUrl warning Alert and falls back to window.location.origin when baseUrl is empty", () => {
		renderForm({
			connectionId: 7,
			providerKey: "jira.oauth",
			baseUrl: null,
			oauthService: createMockOAuthService(),
		});

		expect(
			screen.getByText(/Your callback URL may be incorrect/i),
		).toBeInTheDocument();
		expect(screen.getByText(/Lighthouse:BaseUrl/i)).toBeInTheDocument();

		const callback = screen.getByLabelText<HTMLInputElement>("Callback URL");
		expect(callback.value).toBe(
			"https://fallback.example.com/api/oauth/callback",
		);
	});

	it("calls initiateConnect with prop values and opens the OAuth popup with the authorization URL on Connect click", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
		});
		openOAuthPopupMock.mockResolvedValue({
			status: "success",
			connectionId: 42,
		});

		renderForm({
			connectionId: 42,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService,
		});

		await user.click(screen.getByRole("button", { name: /Connect/i }));

		await waitFor(() => {
			expect(oauthService.initiateConnect).toHaveBeenCalledWith(
				"jira.oauth",
				42,
			);
		});

		await waitFor(() => {
			expect(openOAuthPopupMock).toHaveBeenCalledWith(
				"https://auth.atlassian.com/authorize?state=xyz",
			);
		});
	});

	it("invokes onConnect exactly once after the OAuth popup resolves with success", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
		});
		openOAuthPopupMock.mockResolvedValue({
			status: "success",
			connectionId: 42,
		});
		const onConnect = vi.fn();

		renderForm({
			connectionId: 42,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService,
			onConnect,
		});

		await user.click(screen.getByRole("button", { name: /Connect/i }));

		await waitFor(() => {
			expect(onConnect).toHaveBeenCalledTimes(1);
		});
	});

	it("surfaces the popup-blocked alert and re-enables Connect when the browser blocks the OAuth popup", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
		});
		openOAuthPopupMock.mockResolvedValue({ status: "popup_blocked" });
		const onConnect = vi.fn();

		renderForm({
			connectionId: 42,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService,
			onConnect,
		});

		await user.click(screen.getByRole("button", { name: /Connect/i }));

		await waitFor(() => {
			expect(screen.getByText(/blocked the OAuth popup/i)).toBeInTheDocument();
		});

		expect(onConnect).not.toHaveBeenCalled();
		expect(screen.getByRole("button", { name: /Connect/i })).toBeEnabled();
	});

	it("surfaces a cancellation notice inline and re-enables Connect when the user closes the OAuth popup", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
		});
		openOAuthPopupMock.mockResolvedValue({ status: "cancelled" });
		const onConnect = vi.fn();

		renderForm({
			connectionId: 42,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService,
			onConnect,
		});

		await user.click(screen.getByRole("button", { name: /Connect/i }));

		await waitFor(() => {
			expect(screen.getByText(/cancelled/i)).toBeInTheDocument();
		});

		expect(onConnect).not.toHaveBeenCalled();
	});

	it("surfaces the IdP error reason inline when the OAuth popup resolves with error", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
		});
		openOAuthPopupMock.mockResolvedValue({
			status: "error",
			reason: "invalid_grant",
		});
		const onConnect = vi.fn();

		renderForm({
			connectionId: 42,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService,
			onConnect,
		});

		await user.click(screen.getByRole("button", { name: /Connect/i }));

		await waitFor(() => {
			expect(screen.getByText(/invalid_grant/i)).toBeInTheDocument();
		});

		expect(onConnect).not.toHaveBeenCalled();
	});

	it("renders the ADO HTTPS warning when providerKey is ado.oauth and baseUrl is http", () => {
		renderForm({
			connectionId: 7,
			providerKey: "ado.oauth",
			baseUrl: "http://lighthouse.example.com",
			oauthService: createMockOAuthService(),
		});

		expect(
			screen.getByText(
				/Azure DevOps requires HTTPS callback URLs in production/i,
			),
		).toBeInTheDocument();

		expect(
			screen.queryByText(/Your callback URL may be incorrect/i),
		).not.toBeInTheDocument();
	});

	it("renders the BaseUrl warning regardless of OAuth provider when baseUrl is empty (regression)", () => {
		renderForm({
			connectionId: 7,
			providerKey: "ado.oauth",
			baseUrl: null,
			oauthService: createMockOAuthService(),
		});

		expect(
			screen.getByText(/Your callback URL may be incorrect/i),
		).toBeInTheDocument();
	});

	it("does NOT render the ADO HTTPS warning when providerKey is ado.oauth and baseUrl is https", () => {
		renderForm({
			connectionId: 7,
			providerKey: "ado.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService: createMockOAuthService(),
		});

		expect(
			screen.queryByText(
				/Azure DevOps requires HTTPS callback URLs in production/i,
			),
		).not.toBeInTheDocument();
	});

	it("does NOT render the ADO HTTPS warning when providerKey is jira.oauth even with an http baseUrl", () => {
		renderForm({
			connectionId: 7,
			providerKey: "jira.oauth",
			baseUrl: "http://lighthouse.example.com",
			oauthService: createMockOAuthService(),
		});

		expect(
			screen.queryByText(
				/Azure DevOps requires HTTPS callback URLs in production/i,
			),
		).not.toBeInTheDocument();
	});

	it("disables Connect button while initiateConnect is in flight", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		const pending = new Promise<{ authorizationUrl: string }>(() => {
			// never resolves — keeps the request in flight for the duration of the test
		});
		vi.mocked(oauthService.initiateConnect).mockReturnValue(pending);

		renderForm({
			connectionId: 42,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService,
		});

		const button = screen.getByRole("button", { name: /Connect/i });
		expect(button).not.toBeDisabled();

		await user.click(button);

		await waitFor(() => {
			expect(button).toBeDisabled();
		});
	});
});
