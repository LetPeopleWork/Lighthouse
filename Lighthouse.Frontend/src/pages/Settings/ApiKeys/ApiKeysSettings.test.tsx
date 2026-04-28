import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IApiKeyInfo } from "../../../models/ApiKey/ApiKey";
import { AuthMode } from "../../../models/Auth/AuthModels";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiKeyService,
	createMockApiServiceContext,
} from "../../../tests/MockApiServiceProvider";
import ApiKeysSettings from "./ApiKeysSettings";

const buildWrapper = (
	apiKeyServiceOverrides = {},
	authServiceOverrides = {},
) => {
	const mockApiKeyService = {
		...createMockApiKeyService(),
		...apiKeyServiceOverrides,
	};
	const mockAuthService = {
		getRuntimeAuthStatus: vi.fn().mockResolvedValue({ mode: AuthMode.Enabled }),
		getSession: vi.fn(),
		getLoginUrl: vi.fn(),
		logout: vi.fn(),
		...authServiceOverrides,
	};
	const mockContext = createMockApiServiceContext({
		apiKeyService: mockApiKeyService,
		authService: mockAuthService,
	});

	const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);

	return { Wrapper, mockApiKeyService, mockAuthService };
};

describe("ApiKeysSettings", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("shows empty state message when there are no keys", async () => {
		const { Wrapper } = buildWrapper({
			getApiKeys: vi.fn().mockResolvedValue([]),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(screen.getByTestId("no-api-keys-message")).toBeInTheDocument();
		});
	});

	it("renders api keys table when keys are present", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "CI Key",
				description: "For CI",
				createdByUser: "alice",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
			},
		];
		const { Wrapper } = buildWrapper({
			getApiKeys: vi.fn().mockResolvedValue(keys),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(screen.getByTestId("api-keys-table")).toBeInTheDocument();
		});
		expect(screen.getByTestId("api-key-row-1")).toBeInTheDocument();
		expect(screen.getByText("CI Key")).toBeInTheDocument();
	});

	it("shows 'Never' when lastUsedAt is null", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 1,
				name: "Key",
				description: "",
				createdByUser: "alice",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
			},
		];
		const { Wrapper } = buildWrapper({
			getApiKeys: vi.fn().mockResolvedValue(keys),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(screen.getByText("Never")).toBeInTheDocument();
		});
	});

	it("renders create api key button", async () => {
		const { Wrapper } = buildWrapper();

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(screen.getByTestId("create-api-key-button")).toBeEnabled();
		});
	});

	it("disables api key management when auth is disabled", async () => {
		const getApiKeys = vi.fn().mockResolvedValue([]);
		const { Wrapper } = buildWrapper(
			{ getApiKeys },
			{
				getRuntimeAuthStatus: vi
					.fn()
					.mockResolvedValue({ mode: AuthMode.Disabled }),
			},
		);

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(
				screen.getByTestId("api-keys-disabled-message"),
			).toBeInTheDocument();
		});
		expect(screen.getByTestId("create-api-key-button")).toBeDisabled();
		expect(getApiKeys).not.toHaveBeenCalled();
	});

	it("opens create dialog when create button is clicked", async () => {
		const { Wrapper } = buildWrapper();

		render(<ApiKeysSettings />, { wrapper: Wrapper });
		await waitFor(() => {
			expect(screen.getByTestId("create-api-key-button")).toBeEnabled();
		});

		fireEvent.click(screen.getByTestId("create-api-key-button"));

		await waitFor(() => {
			expect(screen.getByTestId("api-key-name-input")).toBeInTheDocument();
		});
	});

	it("deletes a key when delete button is clicked", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 5,
				name: "Key",
				description: "",
				createdByUser: "alice",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
			},
		];
		const mockDeleteApiKey = vi.fn().mockResolvedValue(undefined);
		const { Wrapper } = buildWrapper({
			getApiKeys: vi.fn().mockResolvedValue(keys),
			deleteApiKey: mockDeleteApiKey,
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(screen.getByTestId("delete-api-key-5")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("delete-api-key-5"));

		await waitFor(() => {
			expect(mockDeleteApiKey).toHaveBeenCalledWith(5);
		});
	});

	it("removes key from list after deletion", async () => {
		const keys: IApiKeyInfo[] = [
			{
				id: 5,
				name: "Key",
				description: "",
				createdByUser: "alice",
				createdAt: "2026-01-01T00:00:00Z",
				lastUsedAt: null,
			},
		];
		const { Wrapper } = buildWrapper({
			getApiKeys: vi.fn().mockResolvedValue(keys),
			deleteApiKey: vi.fn().mockResolvedValue(undefined),
		});

		render(<ApiKeysSettings />, { wrapper: Wrapper });

		await waitFor(() => {
			expect(screen.getByTestId("api-key-row-5")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("delete-api-key-5"));

		await waitFor(() => {
			expect(screen.queryByTestId("api-key-row-5")).not.toBeInTheDocument();
		});
	});
});

describe("CreateApiKeyDialog (via ApiKeysSettings)", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	const openDialog = async (
		Wrapper: React.FC<{ children: React.ReactNode }>,
	) => {
		render(<ApiKeysSettings />, { wrapper: Wrapper });
		await waitFor(() => {
			expect(screen.getByTestId("create-api-key-button")).toBeEnabled();
		});
		fireEvent.click(screen.getByTestId("create-api-key-button"));
		await waitFor(() => {
			expect(screen.getByTestId("api-key-name-input")).toBeInTheDocument();
		});
	};

	it("submit button is disabled when name is empty", async () => {
		const { Wrapper } = buildWrapper();
		await openDialog(Wrapper);

		expect(screen.getByTestId("create-api-key-submit-button")).toBeDisabled();
	});

	it("submit button is enabled when name is filled", async () => {
		const { Wrapper } = buildWrapper();
		await openDialog(Wrapper);

		fireEvent.change(screen.getByTestId("api-key-name-input"), {
			target: { value: "my-key" },
		});

		expect(
			screen.getByTestId("create-api-key-submit-button"),
		).not.toBeDisabled();
	});

	it("shows one-time key after creation", async () => {
		const { Wrapper, mockApiKeyService } = buildWrapper();
		mockApiKeyService.createApiKey = vi.fn().mockResolvedValue({
			id: 1,
			name: "my-key",
			description: "",
			createdByUser: "alice",
			createdAt: "2026-01-01T00:00:00Z",
			plainTextKey: "lh_supersecretkey",
		});

		await openDialog(Wrapper);

		fireEvent.change(screen.getByTestId("api-key-name-input"), {
			target: { value: "my-key" },
		});
		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(screen.getByTestId("created-api-key-value")).toBeInTheDocument();
		});
		expect(screen.getByTestId("created-api-key-value")).toHaveTextContent(
			"lh_supersecretkey",
		);
	});

	it("shows copy button after key is created", async () => {
		const { Wrapper, mockApiKeyService } = buildWrapper();
		mockApiKeyService.createApiKey = vi.fn().mockResolvedValue({
			id: 1,
			name: "my-key",
			description: "",
			createdByUser: "alice",
			createdAt: "2026-01-01T00:00:00Z",
			plainTextKey: "lh_supersecretkey",
		});

		await openDialog(Wrapper);

		fireEvent.change(screen.getByTestId("api-key-name-input"), {
			target: { value: "my-key" },
		});
		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(screen.getByTestId("copy-api-key-button")).toBeInTheDocument();
		});
	});

	it("calls createApiKey with name and description", async () => {
		const { Wrapper, mockApiKeyService } = buildWrapper();
		mockApiKeyService.createApiKey = vi.fn().mockResolvedValue({
			id: 1,
			name: "my-key",
			description: "for ci",
			createdByUser: "alice",
			createdAt: "2026-01-01T00:00:00Z",
			plainTextKey: "lh_key",
		});

		await openDialog(Wrapper);

		fireEvent.change(screen.getByTestId("api-key-name-input"), {
			target: { value: "my-key" },
		});
		fireEvent.change(screen.getByTestId("api-key-description-input"), {
			target: { value: "for ci" },
		});
		fireEvent.click(screen.getByTestId("create-api-key-submit-button"));

		await waitFor(() => {
			expect(mockApiKeyService.createApiKey).toHaveBeenCalledWith({
				name: "my-key",
				description: "for ci",
			});
		});
	});
});
