import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import OAuthAuthForm from "./OAuthAuthForm";

const originalAssign = window.location.assign;

const createMockOAuthService = (): IOAuthService => ({
	initiateConnect: vi.fn(),
	disconnect: vi.fn(),
});

const renderForm = (props: {
	connectionId: number;
	providerKey: string;
	baseUrl: string | null;
	oauthService: IOAuthService;
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
			/>
		</ApiServiceContext.Provider>,
	);
};

describe("OAuthAuthForm", () => {
	beforeEach(() => {
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
		Object.defineProperty(window, "location", {
			value: { ...window.location, assign: originalAssign },
			writable: true,
		});
	});

	it("renders client credentials and a read-only callback URL when baseUrl is set", () => {
		renderForm({
			connectionId: 7,
			providerKey: "jira.oauth",
			baseUrl: "https://lighthouse.example.com",
			oauthService: createMockOAuthService(),
		});

		expect(screen.getByLabelText("Client ID")).toBeInTheDocument();

		const secret = screen.getByLabelText<HTMLInputElement>("Client Secret");
		expect(secret).toBeInTheDocument();
		expect(secret).toHaveAttribute("type", "password");

		const callback = screen.getByLabelText<HTMLInputElement>("Callback URL");
		expect(callback).toHaveAttribute("readonly");
		expect(callback.value).toBe(
			"https://lighthouse.example.com/api/oauth/callback",
		);

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

	it("calls initiateConnect with prop values and redirects via window.location.assign on Connect click", async () => {
		const user = userEvent.setup();
		const oauthService = createMockOAuthService();
		vi.mocked(oauthService.initiateConnect).mockResolvedValue({
			authorizationUrl: "https://auth.atlassian.com/authorize?state=xyz",
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
			expect(window.location.assign).toHaveBeenCalledWith(
				"https://auth.atlassian.com/authorize?state=xyz",
			);
		});
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
