import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOAuthService } from "../../../services/Api/OAuthService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import WriteConsentAlert from "./WriteConsentAlert";

const openOAuthPopupMock = vi.fn();

vi.mock("../../../hooks/useOAuthPopup", () => ({
	useOAuthPopup: () => ({ openOAuthPopup: openOAuthPopupMock }),
}));

const aConnection = (needsWriteConsent: boolean, id: number | null = 7) =>
	Object.assign(
		new WorkTrackingSystemConnection({
			name: "Fabrikam ADO",
			workTrackingSystem: "AzureDevOps",
			options: [],
			authenticationMethodKey: "ado.oauth",
		}),
		{ id, needsWriteConsent },
	);

const renderAlert = (
	connection: WorkTrackingSystemConnection,
	initiateConnect: IOAuthService["initiateConnect"] = vi
		.fn()
		.mockResolvedValue({ authorizationUrl: "https://login.example.test" }),
) =>
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				oauthService: { initiateConnect, disconnect: vi.fn() },
			})}
		>
			<WriteConsentAlert connection={connection} />
		</ApiServiceContext.Provider>,
	);

const theOutcome = () =>
	screen.findByTestId("write-consent-alert-inline-message");

describe("WriteConsentAlert", () => {
	beforeEach(() => {
		openOAuthPopupMock.mockReset();
	});

	it.each([
		["the sign-in already may write", aConnection(false)],
		["the connection is not saved yet", aConnection(true, null)],
	])("shows nothing when %s", (_label, connection) => {
		renderAlert(connection);

		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it.each([
		[
			"a blocked popup",
			{ status: "popup_blocked" },
			"Your browser blocked the OAuth popup. Allow popups for this site and click Sign in again.",
		],
		[
			"a cancelled popup",
			{ status: "cancelled" },
			"OAuth was cancelled. Click Sign in again to try again.",
		],
	])(
		"after %s, words the outcome for Sign in again",
		async (_label, popupResult, copy) => {
			openOAuthPopupMock.mockResolvedValue(popupResult);
			renderAlert(aConnection(true));

			await userEvent.click(
				screen.getByRole("button", { name: "Sign in again" }),
			);

			expect(await theOutcome()).toHaveTextContent(copy);
		},
	);

	it("when the sign-in cannot start, says so in its own words", async () => {
		renderAlert(
			aConnection(true),
			vi.fn().mockRejectedValue(new Error("offline")),
		);

		await userEvent.click(
			screen.getByRole("button", { name: "Sign in again" }),
		);

		expect(await theOutcome()).toHaveTextContent(
			"Failed to start the sign-in. Please try again.",
		);
	});
});
