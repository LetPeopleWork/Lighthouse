import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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

// The server names the switch's reach as a Viewer rule held by "everyone signed in".
const everyoneSignedInAsViewer = {
	role: "Viewer",
	holder: "everyone signed in",
} as const;

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

describe("The Access tab notice while everyone signed in can view", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		getTerm.mockImplementation((key: string) => theirWords[key] ?? key);
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("names everyone signed in as Viewer, not as a group, beside a group rule", async () => {
		rbacService.getTeamSystemWideRules.mockResolvedValue([
			everyoneSignedInAsViewer,
			{ role: "Viewer", holder: "team-readers" },
		]);

		render(<SystemWideRulesNotice unitKind="Team" unitId={11} />, {
			wrapper: withTheServices,
		});

		expect(
			await screen.findByText(/^Viewer: everyone signed in$/),
		).toBeInTheDocument();
		expect(
			screen.queryByText(/^Viewer: group everyone signed in$/),
		).not.toBeInTheDocument();
		expect(
			screen.getByText(/^Viewer: group team-readers$/),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				/^Also reachable through access to all Squads and Programs$/,
			),
		).toBeInTheDocument();
	});

	it("shows the notice for a Portfolio when everyone signed in is the only rule", async () => {
		rbacService.getPortfolioSystemWideRules.mockResolvedValue([
			everyoneSignedInAsViewer,
		]);

		render(<SystemWideRulesNotice unitKind="Portfolio" unitId={21} />, {
			wrapper: withTheServices,
		});

		expect(
			await screen.findByText(/^Viewer: everyone signed in$/),
		).toBeInTheDocument();
		expect(rbacService.getPortfolioSystemWideRules).toHaveBeenCalledWith(21);
	});
});
