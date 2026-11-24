import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type WorkItemRights,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { renderWithTerms } from "../../../tests/FieldTipsTestKit";
import ModifyConnectionSettings from "./ModifyConnectionSettings";

const openOAuthPopupMock = vi.fn();

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: true },
	}),
}));

const NAME = "Fabrikam ADO";
const urlOption = {
	key: "url",
	displayName: "Organization URL",
	isSecret: false,
	isOptional: false,
};

const fabrikam = (grantedRights: WorkItemRights) =>
	Object.assign(
		new WorkTrackingSystemConnection({
			id: 7,
			name: NAME,
			workTrackingSystem: "AzureDevOps",
			authenticationMethodKey: "ado.oauth",
			availableAuthenticationMethods: [
				{
					key: "ado.oauth",
					displayName: "Azure DevOps (OAuth)",
					options: [urlOption],
				},
				{
					key: "ado.pat",
					displayName: "Personal Access Token",
					options: [
						urlOption,
						{
							key: "token",
							displayName: "Token",
							isSecret: true,
							isOptional: false,
						},
					],
				},
			],
			options: [{ ...urlOption, value: "https://dev.azure.com/fabrikam" }],
			usedBy: { teams: 0, portfolios: 0 },
			defaults: {},
			supportedDefaults: [],
		}),
		{ grantedRights, needsWriteConsent: grantedRights === "Read" },
	);

const openTheConnection = async (
	reload: () => Promise<WorkTrackingSystemConnection | null>,
) => {
	const getConnectionSettings = vi
		.fn()
		.mockResolvedValueOnce(fabrikam("Read"))
		.mockImplementationOnce(reload);
	await renderWithTerms(
		<MemoryRouter>
			<ModifyConnectionSettings
				title="Update Connection"
				getSupportedSystems={vi.fn().mockResolvedValue([])}
				getConnectionSettings={getConnectionSettings}
				saveConnectionSettings={vi.fn()}
				validateConnectionSettings={vi.fn()}
			/>
		</MemoryRouter>,
		{},
		{
			oauthService: {
				initiateConnect: vi.fn().mockResolvedValue({
					authorizationUrl: "https://login.example.test",
				}),
				disconnect: vi.fn(),
			},
		},
	);
	await screen.findByDisplayValue(NAME);
	return getConnectionSettings;
};

const signInAgain = async (getConnectionSettings: ReturnType<typeof vi.fn>) => {
	await userEvent.click(screen.getByRole("button", { name: "Sign in again" }));
	await waitFor(() => expect(getConnectionSettings).toHaveBeenCalledTimes(2));
};

const theRightsLine = () => screen.queryByText(/^Rights: /);
const theWarning = () => screen.queryByText(/^Write-back needs more rights\./);

describe("ModifyConnectionSettings: the saved sign-in's rights", () => {
	beforeEach(() => {
		openOAuthPopupMock.mockReset();
		openOAuthPopupMock.mockResolvedValue({ status: "success" });
		vi.spyOn(console, "error").mockImplementation(() => {});
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("after signing in again, shows the rights the connection now has", async () => {
		const getConnectionSettings = await openTheConnection(() =>
			Promise.resolve(fabrikam("ReadAndWrite")),
		);

		await signInAgain(getConnectionSettings);

		expect(
			await screen.findByText("Rights: Read and write work items"),
		).toBeInTheDocument();
		expect(theWarning()).not.toBeInTheDocument();
	});

	it.each([
		["cannot be loaded", () => Promise.reject(new Error("offline"))],
		["is no longer found", () => Promise.resolve(null)],
	])(
		"when the connection %s after signing in again, keeps the form but drops the rights and the warning",
		async (_label, reload) => {
			const getConnectionSettings = await openTheConnection(reload);

			await signInAgain(getConnectionSettings);

			await waitFor(() => expect(theRightsLine()).not.toBeInTheDocument());
			expect(theWarning()).not.toBeInTheDocument();
			expect(screen.getByDisplayValue(NAME)).toBeInTheDocument();
			expect(
				screen.getByRole("combobox", { name: /Authentication Method/ }),
			).toBeInTheDocument();
		},
	);

	it("hides the rights once another authentication method is picked, since they belong to the saved sign-in", async () => {
		await openTheConnection(() => new Promise(() => {}));
		expect(theRightsLine()).toHaveTextContent("Rights: Read work items");

		await userEvent.click(
			screen.getByRole("combobox", { name: /Authentication Method/ }),
		);
		await userEvent.click(
			within(screen.getByRole("listbox")).getByRole("option", {
				name: /Personal Access Token/,
			}),
		);

		expect(theRightsLine()).not.toBeInTheDocument();
	});
});
