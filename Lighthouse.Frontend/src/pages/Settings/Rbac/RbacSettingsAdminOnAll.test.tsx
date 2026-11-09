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

// "UnitAdmin" is the role the server stores Admin on all under, always on every Team and Portfolio.
const adminOnAllForDeliveryLeads = {
	id: 3,
	groupValue: "delivery-leads",
	role: "UnitAdmin",
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

describe("Settings > Access, Admin on all for an SSO group", () => {
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
			adminOnAllForDeliveryLeads,
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

	it("offers System Admin, Viewer on all and Admin on all as the roles of a group", async () => {
		openAccessSettings();
		await theRulesTable();

		fireEvent.mouseDown(
			await screen.findByRole("combobox", { name: /^Role$/ }),
		);

		expect(
			(await screen.findAllByRole("option")).map(
				(option) => option.textContent,
			),
		).toEqual(["System Admin", "Viewer on all", "Admin on all"]);
	});

	it("adds Admin on all for a group and reports that it was granted, never which group", async () => {
		openAccessSettings();
		await theRulesTable();

		await addAMappingFor("delivery-leads-2", "Admin on all");

		expect(rbacService.createGroupMapping).toHaveBeenCalledWith({
			groupValue: "delivery-leads-2",
			role: "UnitAdmin",
			scopeType: "AllUnits",
			scopeId: null,
		});
		await waitFor(() =>
			expect(reportUsage.mock.calls).toEqual([
				[{ name: "SystemWideAccessGranted" }],
			]),
		);
	});

	it("lists an Admin-on-all rule as Admin on all Teams and Portfolios, in the organisation's words", async () => {
		openAccessSettings();

		const table = await theRulesTable();
		const adminLine = within(table).getByRole("row", {
			name: /delivery-leads/,
		});
		expect(
			within(adminLine)
				.getAllByRole("cell")
				.slice(0, 3)
				.map((cell) => cell.textContent),
		).toEqual(["delivery-leads", "Admin", "All Squads and Programs"]);
	});
});
