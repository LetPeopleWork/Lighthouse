import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen, waitFor } from "@testing-library/react";
import type { ComponentType, ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IRbacService } from "../../../services/Api/RbacService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";

const { getTerm } = vi.hoisted(() => ({ getTerm: vi.fn() }));

vi.mock("../../../services/TerminologyContext", async (importOriginal) => ({
	...(await importOriginal<
		typeof import("../../../services/TerminologyContext")
	>()),
	useTerminology: () => ({ getTerm }),
}));

type UnitKind = "Team" | "Portfolio";

interface NoticeProps {
	unitKind: UnitKind;
	unitId: number;
}

// The notice lives in a file the product does not have yet. Looking it up this way keeps a missing file
// a failed expectation with a message, not a collection error that hides every other test.
const noticeModules = import.meta.glob<{ default: ComponentType<NoticeProps> }>(
	"./SystemWideRulesNotice.tsx",
);

const loadTheNotice = async (): Promise<ComponentType<NoticeProps>> => {
	const load = noticeModules["./SystemWideRulesNotice.tsx"];
	if (!load) {
		expect.fail(
			"WHAT: there is no SystemWideRulesNotice component. WHY: a Team or Portfolio Admin must see on the Access tab which rules for all Teams and Portfolios also reach the unit. HOW: add components/Common/Authorization/SystemWideRulesNotice.tsx with a default export taking unitKind ('Team' or 'Portfolio') and unitId.",
		);
	}
	return (await load()).default;
};

const wordsOfTheOrganisation: Record<string, string> = {
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.TEAMS]: "Teams",
	[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
	[TERMINOLOGY_KEYS.PORTFOLIOS]: "Portfolios",
};

const rbacService = {
	getTeamSystemWideRules: vi.fn(),
	getPortfolioSystemWideRules: vi.fn(),
};

const aRuleOf = (role: string, holder: string) => ({ role, holder });

const deferred = <T,>() => {
	let resolve!: (value: T) => void;
	let reject!: (reason: unknown) => void;
	const promise = new Promise<T>((res, rej) => {
		resolve = res;
		reject = rej;
	});
	return { promise, resolve, reject };
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

const showTheNoticeOf = async (unitKind: UnitKind, unitId: number) => {
	const Notice = await loadTheNotice();
	return render(<Notice unitKind={unitKind} unitId={unitId} />, {
		wrapper: withTheServices,
	});
};

const theHeading = () =>
	screen.queryByText(/^Also reachable through access to all .+ and .+$/);

describe("The rules for all Teams and Portfolios on a unit's Access tab", () => {
	beforeEach(() => {
		vi.resetAllMocks();
		getTerm.mockImplementation(
			(key: string) => wordsOfTheOrganisation[key] ?? key,
		);
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("shows a spinner and says it is checking, and nothing else, while the answer is on its way", async () => {
		rbacService.getTeamSystemWideRules.mockReturnValue(
			new Promise(() => undefined),
		);

		const { container } = await showTheNoticeOf("Team", 11);

		expect(
			await screen.findByText(/^Checking access to all Teams and Portfolios…$/),
		).toBeInTheDocument();
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(theHeading()).not.toBeInTheDocument();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
		expect(container.textContent).not.toMatch(/team-readers/);
	});

	it("warns, never hides, and says to reload when the answer could not be read", async () => {
		rbacService.getTeamSystemWideRules.mockRejectedValue(new Error("down"));

		await showTheNoticeOf("Team", 11);

		const warning = await screen.findByRole("alert");
		expect(warning).toHaveTextContent(
			/^Could not check access to all Teams and Portfolios\. More people may reach this Team than listed below\. Reload the page to try again\.$/,
		);
		expect(warning.className).toMatch(/Warning/);
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
		expect(theHeading()).not.toBeInTheDocument();
	});

	it("lists the Viewer rule with its group, and says removing someone below does not take it away", async () => {
		rbacService.getTeamSystemWideRules.mockResolvedValue([
			aRuleOf("Viewer", "team-readers"),
		]);

		await showTheNoticeOf("Team", 11);

		expect(
			await screen.findByText(
				/^Also reachable through access to all Teams and Portfolios$/,
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(/^Viewer: group team-readers$/),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				/^Removing someone below does not take away what these rules give\.$/,
			),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				/^Only a System Admin changes them, in Settings > Access\.$/,
			),
		).toBeInTheDocument();
		expect(screen.getByRole("alert").className).toMatch(/Info/);
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
		expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
		expect(rbacService.getTeamSystemWideRules).toHaveBeenCalledWith(11);
		expect(rbacService.getPortfolioSystemWideRules).not.toHaveBeenCalled();
	});

	it("shows nothing once the answer arrives and no rule reaches the unit, and not before", async () => {
		const answer = deferred<unknown[]>();
		rbacService.getTeamSystemWideRules.mockReturnValue(answer.promise);

		const { container } = await showTheNoticeOf("Team", 11);
		expect(await screen.findByRole("progressbar")).toBeInTheDocument();
		answer.resolve([]);

		await waitFor(() =>
			expect(screen.queryByRole("progressbar")).not.toBeInTheDocument(),
		);
		expect(container).toBeEmptyDOMElement();
	});

	it("asks for a Portfolio's rules and speaks of the Portfolio in the organisation's words", async () => {
		getTerm.mockImplementation(
			(key: string) =>
				({
					[TERMINOLOGY_KEYS.TEAMS]: "Squads",
					[TERMINOLOGY_KEYS.PORTFOLIOS]: "Programs",
					[TERMINOLOGY_KEYS.TEAM]: "Squad",
					[TERMINOLOGY_KEYS.PORTFOLIO]: "Program",
				})[key] ?? key,
		);
		rbacService.getPortfolioSystemWideRules.mockRejectedValue(
			new Error("down"),
		);

		await showTheNoticeOf("Portfolio", 21);

		expect(await screen.findByRole("alert")).toHaveTextContent(
			/^Could not check access to all Squads and Programs\. More people may reach this Program than listed below\. Reload the page to try again\.$/,
		);
		expect(rbacService.getPortfolioSystemWideRules).toHaveBeenCalledWith(21);
		expect(rbacService.getTeamSystemWideRules).not.toHaveBeenCalled();
	});

	it("never lets a late answer for another unit land on the unit now shown", async () => {
		const zenith = deferred<unknown[]>();
		const apollo = deferred<unknown[]>();
		rbacService.getTeamSystemWideRules.mockImplementation((teamId: number) =>
			teamId === 11 ? zenith.promise : apollo.promise,
		);
		const Notice = await loadTheNotice();
		const view = render(<Notice unitKind="Team" unitId={11} />, {
			wrapper: withTheServices,
		});
		await waitFor(() =>
			expect(rbacService.getTeamSystemWideRules).toHaveBeenCalledWith(11),
		);

		view.rerender(<Notice unitKind="Team" unitId={12} />);
		await waitFor(() =>
			expect(rbacService.getTeamSystemWideRules).toHaveBeenCalledWith(12),
		);
		await act(async () => {
			zenith.resolve([aRuleOf("Viewer", "zenith-only-readers")]);
			await zenith.promise;
		});

		expect(screen.queryByText(/zenith-only-readers/)).not.toBeInTheDocument();
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		apollo.resolve([aRuleOf("Viewer", "team-readers")]);
		expect(
			await screen.findByText(/^Viewer: group team-readers$/),
		).toBeInTheDocument();
		expect(screen.queryByText(/zenith-only-readers/)).not.toBeInTheDocument();
	});
});
