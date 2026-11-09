import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IAuthService } from "../../../services/Api/AuthService";
import type { IRbacService } from "../../../services/Api/RbacService";
import {
	createMockApiServiceContext,
	createMockLicensingService,
} from "../../../tests/MockApiServiceProvider";
import RbacSettings from "./RbacSettings";

const { reportUsage, getTerm } = vi.hoisted(() => ({
	reportUsage: vi.fn(),
	getTerm: vi.fn(),
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

vi.mock("../../../services/TerminologyContext", async (importOriginal) => ({
	...(await importOriginal<
		typeof import("../../../services/TerminologyContext")
	>()),
	useTerminology: () => ({ getTerm }),
}));

const theirWords: Record<string, string> = {
	[TERMINOLOGY_KEYS.TEAM]: "Squad",
	[TERMINOLOGY_KEYS.TEAMS]: "Squads",
	[TERMINOLOGY_KEYS.PORTFOLIO]: "Program",
	[TERMINOLOGY_KEYS.PORTFOLIOS]: "Programs",
	[TERMINOLOGY_KEYS.REFINEMENT]: "Refinement",
};

const theSwitchLabel = /^Everyone signed in can view all Squads and Programs$/;

const offWithItsToken = { everyoneCanView: false, concurrencyToken: "token-1" };
const onWithANewToken = { everyoneCanView: true, concurrencyToken: "token-2" };

const rbacService = {
	getStatus: vi.fn(),
	getUsers: vi.fn(),
	getGroupMappings: vi.fn(),
	getEveryoneSignedIn: vi.fn(),
	setEveryoneSignedIn: vi.fn(),
};

const authService = {
	getCurrentUserProfile: vi.fn(),
} as unknown as IAuthService;

const aStatus = (hasSystemAdmin: boolean) => ({
	enabled: true,
	premiumGateSatisfied: true,
	hasSystemAdmin,
	hasEmergencyAdminConfigured: false,
	readyForEnablement: true,
	unassignedUserCount: 0,
	groupClaimName: "groups",
});

const openAccessSettings = () =>
	render(
		<QueryClientProvider
			client={
				new QueryClient({
					defaultOptions: {
						queries: { retry: false },
						mutations: { retry: false },
					},
				})
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					rbacService: rbacService as unknown as IRbacService,
					authService,
					licensingService: createMockLicensingService(),
				})}
			>
				<RbacSettings />
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);

const theSwitch = () => screen.findByLabelText(theSwitchLabel);

const theSwitchOnceKnown = async () => {
	const control = await theSwitch();
	await waitFor(() => expect(control).toBeEnabled());
	return control;
};

const theConfirmation = () =>
	screen.findByRole("dialog", {
		name: /^Let everyone signed in view all Squads and Programs\?$/,
	});

describe("Settings > Access, everyone signed in can view all Teams and Portfolios", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		getTerm.mockImplementation((key: string) => theirWords[key] ?? key);
		rbacService.getStatus.mockResolvedValue(aStatus(true));
		rbacService.getUsers.mockResolvedValue([]);
		rbacService.getGroupMappings.mockResolvedValue([]);
		rbacService.getEveryoneSignedIn.mockResolvedValue(offWithItsToken);
		rbacService.setEveryoneSignedIn.mockResolvedValue(undefined);
		vi.mocked(authService.getCurrentUserProfile).mockResolvedValue({
			subject: "oidc|alex-chen",
			displayName: "Alex Chen",
			email: "alex@example.com",
		});
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("shows the switch off under its own heading, with what off means, once it is known", async () => {
		openAccessSettings();

		const control = await theSwitchOnceKnown();
		expect(
			screen.getByRole("heading", { name: /^Everyone signed in$/ }),
		).toBeInTheDocument();
		expect(control).not.toBeChecked();
		expect(
			screen.getByText(/^Off: people see only what they are given\.$/),
		).toBeInTheDocument();
	});

	it("keeps the switch disabled under a spinner, never shown as off, while it is on its way", async () => {
		rbacService.getEveryoneSignedIn.mockReturnValue(new Promise(() => {}));

		openAccessSettings();

		await waitFor(() =>
			expect(rbacService.getEveryoneSignedIn).toHaveBeenCalled(),
		);
		expect(await screen.findAllByRole("progressbar")).not.toHaveLength(0);
		const control = screen.queryByLabelText(theSwitchLabel);
		expect(control === null || (control as HTMLInputElement).disabled).toBe(
			true,
		);
		expect(
			screen.queryByText(/^Off: people see only what they are given\.$/),
		).not.toBeInTheDocument();
	});

	it("says the setting could not load, with no switch, when it cannot be read", async () => {
		rbacService.getEveryoneSignedIn.mockRejectedValue(new Error("offline"));

		openAccessSettings();

		expect(
			await screen.findByText(
				/^Could not load this setting\. Reload the page to try again\.$/,
			),
		).toBeInTheDocument();
		expect(screen.queryByLabelText(theSwitchLabel)).not.toBeInTheDocument();
	});

	it("asks first, naming the reach in the organisation's words, and Cancel changes nothing", async () => {
		openAccessSettings();

		fireEvent.click(await theSwitchOnceKnown());
		const confirmation = await theConfirmation();
		expect(confirmation).toHaveTextContent(
			/Everyone who can sign in, now and later, can read every Squad and Program, and can vote and comment on Refinement\./,
		);
		fireEvent.click(screen.getByRole("button", { name: /^Cancel$/ }));

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(rbacService.setEveryoneSignedIn).not.toHaveBeenCalled();
		expect(await theSwitch()).not.toBeChecked();
		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("turns it on with the token read after Turn on, shows it on and reports that access was granted", async () => {
		rbacService.getEveryoneSignedIn
			.mockResolvedValueOnce(offWithItsToken)
			.mockResolvedValue(onWithANewToken);
		openAccessSettings();

		fireEvent.click(await theSwitchOnceKnown());
		await theConfirmation();
		fireEvent.click(screen.getByRole("button", { name: /^Turn on$/ }));

		await waitFor(() =>
			expect(rbacService.setEveryoneSignedIn).toHaveBeenCalledWith({
				everyoneCanView: true,
				concurrencyToken: "token-1",
			}),
		);
		await waitFor(async () => expect(await theSwitch()).toBeChecked());
		expect(reportUsage.mock.calls).toEqual([
			[{ name: "SystemWideAccessGranted" }],
		]);
	});

	it("turns it off with no question and reports nothing", async () => {
		rbacService.getEveryoneSignedIn
			.mockResolvedValueOnce(onWithANewToken)
			.mockResolvedValue({
				everyoneCanView: false,
				concurrencyToken: "token-3",
			});
		openAccessSettings();

		const control = await theSwitchOnceKnown();
		await waitFor(() => expect(control).toBeChecked());
		fireEvent.click(control);

		await waitFor(() =>
			expect(rbacService.setEveryoneSignedIn).toHaveBeenCalledWith({
				everyoneCanView: false,
				concurrencyToken: "token-2",
			}),
		);
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("reloads the switch and says someone else changed it when the save is a conflict", async () => {
		rbacService.setEveryoneSignedIn.mockRejectedValue(
			new ApiError(
				409,
				"Conflict",
				undefined,
				undefined,
				"concurrency-conflict",
			),
		);
		openAccessSettings();

		fireEvent.click(await theSwitchOnceKnown());
		await theConfirmation();
		fireEvent.click(screen.getByRole("button", { name: /^Turn on$/ }));

		expect(
			await screen.findByText(/changed by someone else/i),
		).toBeInTheDocument();
		await waitFor(() =>
			expect(rbacService.getEveryoneSignedIn).toHaveBeenCalledTimes(2),
		);
		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("is not offered before the first System Admin exists", async () => {
		rbacService.getStatus.mockResolvedValue(aStatus(false));

		openAccessSettings();

		expect(
			await screen.findByText(/^No System Admin is assigned yet\.$/),
		).toBeInTheDocument();
		expect(screen.queryByLabelText(theSwitchLabel)).not.toBeInTheDocument();
		expect(rbacService.getEveryoneSignedIn).not.toHaveBeenCalled();
	});
});
