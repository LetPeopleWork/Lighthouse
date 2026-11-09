import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { SystemWideRule } from "../../../models/Authorization/RbacModels";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IRbacService } from "../../../services/Api/RbacService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import SystemWideRulesNotice from "./SystemWideRulesNotice";

const { getTerm } = vi.hoisted(() => ({ getTerm: vi.fn() }));

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
};

const rbacService = {
	getTeamSystemWideRules: vi.fn(),
	getPortfolioSystemWideRules: vi.fn(),
};

const withTheServices = ({ children }: { children: ReactNode }) => (
	<QueryClientProvider
		client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}
	>
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				rbacService: rbacService as unknown as IRbacService,
			})}
		>
			{children}
		</ApiServiceContext.Provider>
	</QueryClientProvider>
);

// "UnitAdmin" is the role the server names an Admin-on-all rule with.
const adminOnAllForDeliveryLeads = {
	role: "UnitAdmin",
	holder: "delivery-leads",
} as unknown as SystemWideRule;

describe("The Access tab notice for an Admin-on-all rule", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		getTerm.mockImplementation((key: string) => theirWords[key] ?? key);
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("names the group as Admin on a Team, beside a Viewer rule", async () => {
		rbacService.getTeamSystemWideRules.mockResolvedValue([
			{ role: "Viewer", holder: "team-readers" },
			adminOnAllForDeliveryLeads,
		]);

		render(<SystemWideRulesNotice unitKind="Team" unitId={11} />, {
			wrapper: withTheServices,
		});

		expect(
			await screen.findByText(/^Admin: group delivery-leads$/),
		).toBeInTheDocument();
		expect(
			screen.queryByText(
				/^(Viewer|Team Admin|Portfolio Admin|System Admin): group delivery-leads$/,
			),
		).not.toBeInTheDocument();
		expect(
			screen.getByText(/^Viewer: group team-readers$/),
		).toBeInTheDocument();
	});

	it("names the group as Admin on a Portfolio too", async () => {
		rbacService.getPortfolioSystemWideRules.mockResolvedValue([
			adminOnAllForDeliveryLeads,
		]);

		render(<SystemWideRulesNotice unitKind="Portfolio" unitId={21} />, {
			wrapper: withTheServices,
		});

		expect(
			await screen.findByText(/^Admin: group delivery-leads$/),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				/^Also reachable through access to all Squads and Programs$/,
			),
		).toBeInTheDocument();
	});
});
