import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { RbacGroupMapping } from "../../../models/Authorization/RbacModels";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
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
	[TERMINOLOGY_KEYS.TEAMS]: "Squads",
	[TERMINOLOGY_KEYS.PORTFOLIOS]: "Programs",
};

// "AllUnits" is the scope the server stores a rule for every Team and Portfolio under.
const viewerOnAllForTeamReaders = {
	id: 2,
	groupValue: "team-readers",
	role: "Viewer",
	scopeType: "AllUnits",
	scopeId: null,
} as unknown as RbacGroupMapping;

const systemAdminsGroup: RbacGroupMapping = {
	id: 1,
	groupValue: "lh-system-admins",
	role: "SystemAdmin",
	scopeType: "System",
	scopeId: null,
};

const rbacService = {
	getStatus: vi.fn(),
	getUsers: vi.fn(),
	getGroupMappings: vi.fn(),
	createGroupMapping: vi.fn(),
	removeGroupMapping: vi.fn(),
	bootstrapCurrentUserAsSystemAdmin: vi.fn(),
	grantSystemAdmin: vi.fn(),
	revokeSystemAdmin: vi.fn(),
	deleteUser: vi.fn(),
};

const authService = {
	getCurrentUserProfile: vi.fn(),
} as unknown as IAuthService;

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

const theRulesTable = () => screen.findByTestId("rbac-group-mappings-table");

const chooseRole = async (option: string) => {
	fireEvent.mouseDown(await screen.findByRole("combobox", { name: /^Role$/ }));
	fireEvent.click(
		await screen.findByRole("option", { name: new RegExp(`^${option}$`) }),
	);
};

const addAMappingFor = async (group: string, role: string) => {
	fireEvent.change(await screen.findByLabelText(/^Group value$/), {
		target: { value: group },
	});
	await chooseRole(role);
	fireEvent.click(screen.getByRole("button", { name: /^Add Mapping$/ }));
	await waitFor(() =>
		expect(rbacService.createGroupMapping).toHaveBeenCalledTimes(1),
	);
};

describe("Settings > Access, SSO group rules for all Teams and Portfolios", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		getTerm.mockImplementation((key: string) => theirWords[key] ?? key);
		rbacService.getStatus.mockResolvedValue({
			enabled: true,
			premiumGateSatisfied: true,
			hasSystemAdmin: true,
			hasEmergencyAdminConfigured: false,
			readyForEnablement: true,
			unassignedUserCount: 0,
			groupClaimName: "groups",
		});
		rbacService.getUsers.mockResolvedValue([]);
		rbacService.getGroupMappings.mockResolvedValue([
			systemAdminsGroup,
			viewerOnAllForTeamReaders,
		]);
		rbacService.createGroupMapping.mockResolvedValue(undefined);
		vi.mocked(authService.getCurrentUserProfile).mockResolvedValue({
			subject: "oidc|alex-chen",
			displayName: "Alex Chen",
			email: "alex@example.com",
		});
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("lists each rule with the group, its role and what it applies to, in the organisation's words", async () => {
		openAccessSettings();

		const table = await theRulesTable();
		expect(
			screen.getByRole("heading", { name: /^SSO Groups$/ }),
		).toBeInTheDocument();
		expect(
			within(table)
				.getAllByRole("columnheader")
				.map((header) => header.textContent),
		).toEqual(["Group", "Role", "Applies to", "Actions"]);
		const viewerLine = within(table).getByRole("row", { name: /team-readers/ });
		expect(
			within(viewerLine)
				.getAllByRole("cell")
				.slice(0, 3)
				.map((cell) => cell.textContent),
		).toEqual(["team-readers", "Viewer", "All Squads and Programs"]);
		const adminLine = within(table).getByRole("row", {
			name: /lh-system-admins/,
		});
		expect(
			within(adminLine)
				.getAllByRole("cell")
				.slice(0, 3)
				.map((cell) => cell.textContent),
		).toEqual(["lh-system-admins", "System Admin", "Everything"]);
	});

	it("offers Viewer on all, adds it for a group and reports that it was granted, never which group", async () => {
		openAccessSettings();
		await theRulesTable();

		await addAMappingFor("team-readers", "Viewer on all");

		expect(rbacService.createGroupMapping).toHaveBeenCalledWith({
			groupValue: "team-readers",
			role: "Viewer",
			scopeType: "AllUnits",
			scopeId: null,
		});
		await waitFor(() =>
			expect(reportUsage.mock.calls).toEqual([
				[{ name: "SystemWideAccessGranted" }],
			]),
		);
	});

	it("adds a System Admin group as before and reports nothing", async () => {
		openAccessSettings();
		await theRulesTable();

		await addAMappingFor("lh-system-admins-2", "System Admin");

		expect(rbacService.createGroupMapping).toHaveBeenCalledWith({
			groupValue: "lh-system-admins-2",
			role: "SystemAdmin",
			scopeType: "System",
			scopeId: null,
		});
		await waitFor(() =>
			expect(rbacService.getGroupMappings).toHaveBeenCalledTimes(2),
		);
		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("shows a spinner where the rules table goes while the rules load", async () => {
		rbacService.getGroupMappings.mockReturnValue(new Promise(() => {}));

		openAccessSettings();

		expect(await screen.findByRole("progressbar")).toBeInTheDocument();
		expect(
			screen.queryByTestId("rbac-group-mappings-table"),
		).not.toBeInTheDocument();
	});

	it("says the rules could not load, in place of the table, when they cannot be loaded", async () => {
		rbacService.getGroupMappings.mockRejectedValue(new Error("offline"));

		openAccessSettings();

		expect(
			await screen.findByText(
				/^Could not load the SSO group rules\. Reload the page to try again\.$/,
			),
		).toBeInTheDocument();
		expect(
			screen.queryByTestId("rbac-group-mappings-table"),
		).not.toBeInTheDocument();
	});
});
