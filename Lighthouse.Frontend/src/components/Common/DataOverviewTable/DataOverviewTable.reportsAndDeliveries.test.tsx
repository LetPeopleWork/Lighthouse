import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { assert, beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../models/Delivery";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import type {
	IReportList,
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { reportsQueryKeyOf } from "../../../pages/Common/Reports/reportOwner";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IDeliveryService } from "../../../services/Api/DeliveryService";
import type { IReportService } from "../../../services/Api/ReportService";
import { actAndSettle } from "../../../tests/actAndSettle";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../tests/MockApiServiceProvider";
import {
	aListedReport,
	aListedReportOfALaterType,
	aReportList,
	aReportService,
} from "../../../tests/ReportsTestKit";
import { openManageColumns } from "../../../tests/WorkItemsDialogTestKit";
import { fireAndForget } from "../../../utils/fireAndForget";
import DataOverviewTable from "./DataOverviewTable";

/**
 * The Overview's Teams and Portfolios tables, each row carrying its owner's reports and, for a Portfolio, its
 * active Deliveries: two chips at most, then a link to the rest, and a cell that says when it is still reading
 * or could not read rather than claiming there is nothing.
 */

const { terms } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
	}),
}));

const seededTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.FEATURE]: "Feature",
	[TERMINOLOGY_KEYS.FEATURES]: "Features",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
	[TERMINOLOGY_KEYS.DELIVERY]: "Delivery",
	[TERMINOLOGY_KEYS.DELIVERIES]: "Deliveries",
};

const LIGHTSPEED: IReportOwner = { kind: "team", id: 1 };
const ROCKET: IReportOwner = { kind: "team", id: 2 };
const APOLLO: IReportOwner = { kind: "portfolio", id: 3 };

const aTeam = (id: number, name: string): IFeatureOwner => ({
	id,
	name,
	remainingFeatures: 4,
	features: [],
	tags: [],
	lastUpdated: new Date("2026-10-14T08:00:00Z"),
	serviceLevelExpectationProbability: 0,
	serviceLevelExpectationRange: 0,
	systemWIPLimit: 0,
});

const aPortfolio = (id: number, name: string): IPortfolio => ({
	...aTeam(id, name),
	involvedTeams: [],
	featureSizeTargetProbability: 0,
	featureSizeTargetRange: 0,
});

const TEAM_LIGHTSPEED = aTeam(LIGHTSPEED.id, "Lightspeed");
const TEAM_ROCKET = aTeam(ROCKET.id, "Rocket");
const PORTFOLIO_APOLLO = aPortfolio(APOLLO.id, "Apollo");

const reportsNamed = (
	owner: IReportOwner,
	...names: string[]
): IReportListItem[] =>
	names.map((name, index) =>
		aListedReport({
			id: owner.id * 100 + index + 1,
			name,
			ownerKind: owner.kind,
			ownerId: owner.id,
		}),
	);

const aDelivery = (
	id: number,
	name: string,
	portfolioId = APOLLO.id,
): Delivery => {
	const delivery = new Delivery();
	delivery.id = id;
	delivery.portfolioId = portfolioId;
	delivery.name = name;
	delivery.likelihoodPercentage = 80;
	delivery.remainingWork = 10;
	delivery.hasSufficientData = true;
	delivery.teamsWithoutForecast = [];
	delivery.features = [1, 2];
	return delivery;
};

const activeDeliveries = (...deliveries: Delivery[]) => ({
	active: deliveries,
	archived: [],
});

const reportsAnswering = (list: IReportList): IReportService =>
	aReportService({ getReports: vi.fn().mockResolvedValue(list) });

const noReports = () => reportsAnswering(aReportList([]));

const deliveriesAnswering = (
	getByPortfolio: IDeliveryService["getByPortfolio"],
): IDeliveryService => ({ ...createMockDeliveryService(), getByPortfolio });

/** Where a followed link took the reader, so a chip is proven to open its page rather than merely to point at it. */
const WhereTheReaderLanded = () => (
	<span data-testid="landed-on">{useLocation().pathname}</span>
);

/** How long the app trusts an answer before it reads it again, so a return visit is tested as the app runs it. */
const THE_APPS_STALE_TIME = 5 * 60 * 1000;

const aQueryClient = (staleTime = 0) =>
	new QueryClient({
		defaultOptions: {
			queries: { retry: false, staleTime },
			mutations: { retry: false },
		},
	});

const renderTheTable = (
	api: "teams" | "portfolios",
	data: IFeatureOwner[],
	{
		reportService = noReports(),
		deliveryService = createMockDeliveryService(),
		queryClient = aQueryClient(),
	}: {
		reportService?: IReportService;
		deliveryService?: IDeliveryService;
		queryClient?: QueryClient;
	} = {},
) => {
	const { unmount } = render(
		<QueryClientProvider client={queryClient}>
			<MemoryRouter initialEntries={["/"]}>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({
						reportService,
						deliveryService,
					})}
				>
					<Routes>
						<Route
							path="/"
							element={
								<DataOverviewTable
									data={data}
									api={api}
									title={api}
									onDelete={vi.fn()}
									filterText=""
								/>
							}
						/>
						<Route path="*" element={<WhereTheReaderLanded />} />
					</Routes>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);
	return { queryClient, reportService, deliveryService, unmount };
};

const theRowOf = (ownerName: string): HTMLElement => {
	const row = screen
		.getByRole("link", { name: ownerName })
		.closest<HTMLElement>('[role="row"]');
	if (!row) throw new Error(`No row for ${ownerName}`);
	return row;
};

const theCellOf = (
	ownerName: string,
	column: "reports" | "deliveries",
): HTMLElement => {
	const cell = theRowOf(ownerName).querySelector<HTMLElement>(
		`[data-field="${column}"]`,
	);
	if (!cell) throw new Error(`No ${column} cell for ${ownerName}`);
	return cell;
};

/** The part of the cell that says whether what it shows is current: loading, error or ready. */
const theLookOf = (cell: HTMLElement): HTMLElement => {
	const look = cell.querySelector<HTMLElement>("[data-cell-status]");
	if (!look) throw new Error("The cell does not say how current it is");
	return look;
};

const whenReady = async (
	ownerName: string,
	column: "reports" | "deliveries",
): Promise<HTMLElement> => {
	await waitFor(() =>
		expect(theLookOf(theCellOf(ownerName, column)).dataset.cellStatus).toBe(
			"ready",
		),
	);
	return theCellOf(ownerName, column);
};

/** WidgetShell's dimmed look, so a re-read reads the same everywhere on the page. */
const DIMMED = { opacity: "0.4" };

/**
 * The nearest box around a chip, up to the cell, that is drawn dimmed; the chip's own link when none is,
 * so a chip that is not dimmed fails the check rather than finding nothing to check.
 */
const theBoxDimmingTheChip = (cell: HTMLElement, chip: HTMLElement) => {
	const link = chip.closest<HTMLElement>("a") ?? chip;
	for (
		let box: HTMLElement | null = link;
		box && box !== cell.parentElement;
		box = box.parentElement
	) {
		if (getComputedStyle(box).opacity === DIMMED.opacity) return box;
	}
	return link;
};

const linkNamesIn = (cell: HTMLElement): string[] =>
	within(cell)
		.queryAllByRole("link")
		.map((link) => (link.textContent ?? "").trim());

const theColumnTitles = (): string[] =>
	screen
		.getAllByRole("columnheader")
		.map((header) => (header.textContent ?? "").trim());

beforeEach(() => {
	terms.current = { ...seededTerms };
	globalThis.localStorage.clear();
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query: string) => ({
			matches: false,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		})),
	});
});

describe("Reports on the Overview", () => {
	it("shows a Reports column on the Teams table, after Tags and before Last Updated", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED]);

		await whenReady("Lightspeed", "reports");

		const titles = theColumnTitles();
		expect(titles.indexOf("Reports")).toBe(titles.indexOf("Tags") + 1);
		expect(titles.indexOf("Last Updated")).toBe(titles.indexOf("Reports") + 1);
	});

	it("shows a Reports column on the Portfolios table, after Deliveries and before Last Updated", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO]);

		await whenReady("Apollo", "reports");

		const titles = theColumnTitles();
		expect(titles.indexOf("Reports")).toBe(titles.indexOf("Deliveries") + 1);
		expect(titles.indexOf("Last Updated")).toBe(titles.indexOf("Reports") + 1);
	});

	it("lets a reader hide the Reports column from the column menu", async () => {
		const user = userEvent.setup();
		renderTheTable("teams", [TEAM_LIGHTSPEED]);
		await whenReady("Lightspeed", "reports");

		await openManageColumns(user, () => document.body);
		const reportsBox = screen
			.getAllByRole("checkbox")
			.find((box) => box.closest("label")?.textContent?.trim() === "Reports");
		if (!reportsBox) assert.fail("Manage columns offers no Reports column");
		await user.click(reportsBox);
		await user.keyboard("{Escape}");

		expect(theColumnTitles()).not.toContain("Reports");
		expect(theColumnTitles()).toContain("Tags");
	});

	it("shows a Team's report as a chip with its name that opens the report", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: reportsAnswering(
				aReportList(reportsNamed(LIGHTSPEED, "Engagement start")),
			),
		});

		const cell = await whenReady("Lightspeed", "reports");
		await userEvent.click(
			within(cell).getByRole("link", { name: "Engagement start" }),
		);

		expect(screen.getByTestId("landed-on")).toHaveTextContent(
			/^\/teams\/1\/reports\/101$/,
		);
	});

	it("shows a Portfolio's report as a chip that opens the report under that Portfolio", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			reportService: reportsAnswering(
				aReportList(reportsNamed(APOLLO, "Quarter review")),
			),
		});

		const cell = await whenReady("Apollo", "reports");
		await userEvent.click(
			within(cell).getByRole("link", { name: "Quarter review" }),
		);

		expect(screen.getByTestId("landed-on")).toHaveTextContent(
			/^\/portfolios\/3\/reports\/301$/,
		);
	});

	it("shows the first two reports by name, A to Z, whatever order they arrive in", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: reportsAnswering(
				aReportList(
					reportsNamed(LIGHTSPEED, "Zeta sprint", "alpha kickoff", "Mid-year"),
				),
			),
		});

		const cell = await whenReady("Lightspeed", "reports");

		expect(linkNamesIn(cell)).toEqual(["alpha kickoff", "Mid-year", "+1 more"]);
	});

	it.each([
		{ count: 1, expected: ["Report A"] },
		{ count: 2, expected: ["Report A", "Report B"] },
		{ count: 3, expected: ["Report A", "Report B", "+1 more"] },
		{ count: 7, expected: ["Report A", "Report B", "+5 more"] },
	])("with $count reports shows $expected", async ({ count, expected }) => {
		const names = Array.from(
			{ length: count },
			(_, index) => `Report ${String.fromCodePoint(65 + index)}`,
		);
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: reportsAnswering(
				aReportList(reportsNamed(LIGHTSPEED, ...names)),
			),
		});

		const cell = await whenReady("Lightspeed", "reports");

		expect(linkNamesIn(cell)).toEqual(expected);
		expect(within(cell).queryByText("No Reports")).toBeNull();
	});

	it("opens the Team's Reports tab from +N more", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: reportsAnswering(
				aReportList(reportsNamed(LIGHTSPEED, "One", "Two", "Three")),
			),
		});

		const cell = await whenReady("Lightspeed", "reports");
		await userEvent.click(within(cell).getByRole("link", { name: "+1 more" }));

		expect(screen.getByTestId("landed-on")).toHaveTextContent(
			/^\/teams\/1\/reports$/,
		);
	});

	it("opens the Portfolio's Reports tab from +N more", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			reportService: reportsAnswering(
				aReportList(reportsNamed(APOLLO, "One", "Two", "Three", "Four")),
			),
		});

		const cell = await whenReady("Apollo", "reports");
		await userEvent.click(within(cell).getByRole("link", { name: "+2 more" }));

		expect(screen.getByTestId("landed-on")).toHaveTextContent(
			/^\/portfolios\/3\/reports$/,
		);
	});

	it("says No Reports for an owner without any", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED]);

		const cell = await whenReady("Lightspeed", "reports");

		expect(cell).toHaveTextContent("No Reports");
		expect(within(cell).queryAllByRole("link")).toHaveLength(0);
	});

	// Its own page explains it cannot be shown; the Overview still lists it so it can be reached and deleted.
	it("still shows a chip for a report of a type this version cannot show", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: reportsAnswering(
				aReportList([aListedReportOfALaterType()]),
			),
		});

		const cell = await whenReady("Lightspeed", "reports");

		expect(linkNamesIn(cell)).toEqual(["Signals — flow alerts"]);
	});

	it("shows a small spinner, not No Reports, while the reports are on their way", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: aReportService({
				getReports: vi.fn().mockReturnValue(deferred<IReportList>().promise),
			}),
		});

		await waitFor(() => theLookOf(theCellOf("Lightspeed", "reports")));
		const cell = theCellOf("Lightspeed", "reports");
		const look = theLookOf(cell);

		expect(look.dataset.cellStatus).toBe("loading");
		expect(look).toHaveAttribute("aria-busy", "true");
		expect(
			within(cell).getByRole("progressbar", { name: "Loading Reports" }),
		).toBeInTheDocument();
		expect(cell).not.toHaveTextContent("No Reports");
	});

	// Creating, editing or deleting a report drops the owner's list, which is what this re-read stands for.
	it("keeps the chips on screen, dimmed under a spinner, while the reports are read again", async () => {
		const reread = deferred<IReportList>();
		const { queryClient } = renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValueOnce(
						aReportList(reportsNamed(LIGHTSPEED, "Engagement start")),
					)
					.mockReturnValue(reread.promise),
			}),
		});
		await whenReady("Lightspeed", "reports");

		act(() => {
			fireAndForget(
				queryClient.invalidateQueries({
					queryKey: reportsQueryKeyOf(LIGHTSPEED),
				}),
			);
		});

		const cell = theCellOf("Lightspeed", "reports");
		await waitFor(() =>
			expect(theLookOf(cell).dataset.cellStatus).toBe("loading"),
		);
		expect(theLookOf(cell)).toHaveAttribute("aria-busy", "true");
		expect(
			within(cell).getByRole("progressbar", { name: "Loading Reports" }),
		).toBeInTheDocument();
		const chip = within(cell).getByText("Engagement start");
		expect(theBoxDimmingTheChip(cell, chip)).toHaveStyle(DIMMED);
	});

	it("shows a report created elsewhere once the owner's reports are read again", async () => {
		const { queryClient } = renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: aReportService({
				getReports: vi
					.fn()
					.mockResolvedValueOnce(aReportList([]))
					.mockResolvedValue(
						aReportList(reportsNamed(LIGHTSPEED, "Engagement start")),
					),
			}),
		});
		expect(await whenReady("Lightspeed", "reports")).toHaveTextContent(
			"No Reports",
		);

		await act(() =>
			queryClient.invalidateQueries({
				queryKey: reportsQueryKeyOf(LIGHTSPEED),
			}),
		);

		await waitFor(() =>
			expect(linkNamesIn(theCellOf("Lightspeed", "reports"))).toEqual([
				"Engagement start",
			]),
		);
	});

	// A report made by someone else, or in another tab, drops nothing here, so only the next visit can bring it.
	it("shows a report changed elsewhere when the Overview is opened again, however soon", async () => {
		const getReports = vi
			.fn()
			.mockResolvedValue(
				aReportList(reportsNamed(LIGHTSPEED, "Engagement start")),
			);
		const reportService = aReportService({ getReports });
		const { queryClient, unmount } = renderTheTable(
			"teams",
			[TEAM_LIGHTSPEED],
			{ reportService, queryClient: aQueryClient(THE_APPS_STALE_TIME) },
		);
		await whenReady("Lightspeed", "reports");
		unmount();

		getReports.mockResolvedValue(
			aReportList(reportsNamed(LIGHTSPEED, "Engagement close")),
		);
		renderTheTable("teams", [TEAM_LIGHTSPEED], { reportService, queryClient });

		await waitFor(() =>
			expect(linkNamesIn(theCellOf("Lightspeed", "reports"))).toEqual([
				"Engagement close",
			]),
		);
		expect(
			theLookOf(theCellOf("Lightspeed", "reports")).dataset.cellStatus,
		).toBe("ready");
	});

	it("says Couldn't load Reports when they could not be read, never No Reports", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED], {
			reportService: aReportService({
				getReports: vi.fn().mockRejectedValue(new Error("unreachable")),
			}),
		});

		await waitFor(() =>
			expect(
				theLookOf(theCellOf("Lightspeed", "reports")).dataset.cellStatus,
			).toBe("error"),
		);
		const cell = theCellOf("Lightspeed", "reports");

		expect(cell).toHaveTextContent("Couldn't load Reports");
		expect(cell).not.toHaveTextContent("No Reports");
		expect(within(cell).queryByRole("progressbar")).toBeNull();
	});

	it("keeps one owner's failure to its own row", async () => {
		renderTheTable("teams", [TEAM_LIGHTSPEED, TEAM_ROCKET], {
			reportService: aReportService({
				getReports: vi.fn((owner: IReportOwner) =>
					owner.id === LIGHTSPEED.id
						? Promise.reject(new Error("unreachable"))
						: Promise.resolve(
								aReportList(reportsNamed(ROCKET, "Launch review")),
							),
				),
			}),
		});

		const rocket = await whenReady("Rocket", "reports");
		await waitFor(() =>
			expect(theCellOf("Lightspeed", "reports")).toHaveTextContent(
				"Couldn't load Reports",
			),
		);

		expect(linkNamesIn(rocket)).toEqual(["Launch review"]);
	});

	it("never lets a late answer for one Team land in another Team's row", async () => {
		const lightspeedAnswer = deferred<IReportList>();
		renderTheTable("teams", [TEAM_LIGHTSPEED, TEAM_ROCKET], {
			reportService: aReportService({
				getReports: vi.fn((owner: IReportOwner) =>
					owner.id === LIGHTSPEED.id
						? lightspeedAnswer.promise
						: Promise.resolve(
								aReportList(reportsNamed(ROCKET, "Launch review")),
							),
				),
			}),
		});
		await whenReady("Rocket", "reports");

		await actAndSettle(() => {
			lightspeedAnswer.resolve(
				aReportList(reportsNamed(LIGHTSPEED, "Engagement start")),
			);
		});

		expect(linkNamesIn(await whenReady("Lightspeed", "reports"))).toEqual([
			"Engagement start",
		]);
		expect(linkNamesIn(theCellOf("Rocket", "reports"))).toEqual([
			"Launch review",
		]);
	});

	// Renaming every configurable word around it shows the column's own word is not one of them.
	it("keeps the word Reports whatever the instance calls its Teams, Portfolios and Deliveries", async () => {
		terms.current = {
			...seededTerms,
			[TERMINOLOGY_KEYS.TEAM]: "Squad",
			[TERMINOLOGY_KEYS.PORTFOLIO]: "Programme",
			[TERMINOLOGY_KEYS.DELIVERIES]: "Releases",
		};
		renderTheTable("portfolios", [PORTFOLIO_APOLLO]);

		const cell = await whenReady("Apollo", "reports");

		expect(theColumnTitles()).toContain("Reports");
		expect(cell).toHaveTextContent("No Reports");
	});
});

describe("Deliveries on the Overview", () => {
	it("shows the first two Deliveries in the order the server answers, then +N more to the Portfolio's Deliveries", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi
					.fn()
					.mockResolvedValue(
						activeDeliveries(
							aDelivery(1, "Winter Release"),
							aDelivery(2, "Autumn Release"),
							aDelivery(3, "Summer Release"),
							aDelivery(4, "Spring Release"),
						),
					),
			),
		});

		const cell = await whenReady("Apollo", "deliveries");
		const links = linkNamesIn(cell);

		expect(links).toHaveLength(3);
		expect(links[0]).toMatch(/^Winter Release/);
		expect(links[1]).toMatch(/^Autumn Release/);
		expect(links[2]).toBe("+2 more");
		expect(cell).not.toHaveTextContent("Summer Release");

		await userEvent.click(within(cell).getByRole("link", { name: "+2 more" }));
		expect(screen.getByTestId("landed-on")).toHaveTextContent(
			/^\/portfolios\/3\/deliveries$/,
		);
	});

	it("shows two Deliveries without any +N more", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi
					.fn()
					.mockResolvedValue(
						activeDeliveries(
							aDelivery(1, "Winter Release"),
							aDelivery(2, "Autumn Release"),
						),
					),
			),
		});

		const cell = await whenReady("Apollo", "deliveries");

		expect(linkNamesIn(cell)).toHaveLength(2);
		expect(within(cell).queryByText(/more$/)).toBeNull();
	});

	it("says No Deliveries for a Portfolio without any", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO]);

		const cell = await whenReady("Apollo", "deliveries");

		expect(cell).toHaveTextContent("No Deliveries");
	});

	it("shows a small spinner, not No Deliveries, while the Deliveries are on their way", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi.fn().mockReturnValue(new Promise(() => undefined)),
			),
		});

		await waitFor(() => theLookOf(theCellOf("Apollo", "deliveries")));
		const cell = theCellOf("Apollo", "deliveries");

		expect(theLookOf(cell).dataset.cellStatus).toBe("loading");
		expect(theLookOf(cell)).toHaveAttribute("aria-busy", "true");
		expect(
			within(cell).getByRole("progressbar", { name: "Loading Deliveries" }),
		).toBeInTheDocument();
		expect(cell).not.toHaveTextContent("No Deliveries");
	});

	it("keeps the Delivery chips on screen, dimmed under a spinner, while they are read again", async () => {
		const reread = deferred<ReturnType<typeof activeDeliveries>>();
		const { queryClient } = renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi
					.fn()
					.mockResolvedValueOnce(
						activeDeliveries(aDelivery(1, "Winter Release")),
					)
					.mockReturnValue(reread.promise),
			),
		});
		await whenReady("Apollo", "deliveries");

		act(() => {
			fireAndForget(
				queryClient.invalidateQueries({
					queryKey: ["deliveries", "portfolio", APOLLO.id],
				}),
			);
		});

		const cell = theCellOf("Apollo", "deliveries");
		await waitFor(() =>
			expect(theLookOf(cell).dataset.cellStatus).toBe("loading"),
		);
		expect(theLookOf(cell)).toHaveAttribute("aria-busy", "true");
		expect(
			within(cell).getByRole("progressbar", { name: "Loading Deliveries" }),
		).toBeInTheDocument();
		const chip = within(cell).getByText(/^Winter Release/);
		expect(theBoxDimmingTheChip(cell, chip)).toHaveStyle(DIMMED);
	});

	// The Deliveries tab reads them without going through this cell, so nothing else would tell it they changed.
	it("shows Deliveries changed elsewhere when the Overview is opened again, however soon", async () => {
		const getByPortfolio = vi
			.fn()
			.mockResolvedValue(activeDeliveries(aDelivery(1, "Winter Release")));
		const deliveryService = deliveriesAnswering(getByPortfolio);
		const { queryClient, unmount } = renderTheTable(
			"portfolios",
			[PORTFOLIO_APOLLO],
			{ deliveryService, queryClient: aQueryClient(THE_APPS_STALE_TIME) },
		);
		await whenReady("Apollo", "deliveries");
		unmount();

		getByPortfolio.mockResolvedValue(
			activeDeliveries(aDelivery(2, "Spring Release")),
		);
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService,
			queryClient,
		});

		await waitFor(() => {
			const links = linkNamesIn(theCellOf("Apollo", "deliveries"));
			expect(links).toHaveLength(1);
			expect(links[0]).toMatch(/^Spring Release/);
		});
		expect(
			theLookOf(theCellOf("Apollo", "deliveries")).dataset.cellStatus,
		).toBe("ready");
	});

	it("shows each Portfolio its own Deliveries", async () => {
		const GEMINI = aPortfolio(4, "Gemini");
		renderTheTable("portfolios", [PORTFOLIO_APOLLO, GEMINI], {
			deliveryService: deliveriesAnswering(
				vi.fn((portfolioId: number) =>
					Promise.resolve(
						portfolioId === APOLLO.id
							? activeDeliveries(aDelivery(1, "Winter Release"))
							: activeDeliveries(aDelivery(2, "Moon Landing", GEMINI.id)),
					),
				),
			),
		});

		const apollo = linkNamesIn(await whenReady("Apollo", "deliveries"));
		const gemini = linkNamesIn(await whenReady("Gemini", "deliveries"));

		expect(apollo).toHaveLength(1);
		expect(apollo[0]).toMatch(/^Winter Release/);
		expect(gemini).toHaveLength(1);
		expect(gemini[0]).toMatch(/^Moon Landing/);
	});

	it("says Couldn't load Deliveries when they could not be read, never No Deliveries", async () => {
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi.fn().mockRejectedValue(new Error("unreachable")),
			),
		});

		await waitFor(() =>
			expect(
				theLookOf(theCellOf("Apollo", "deliveries")).dataset.cellStatus,
			).toBe("error"),
		);
		const cell = theCellOf("Apollo", "deliveries");

		expect(cell).toHaveTextContent("Couldn't load Deliveries");
		expect(cell).not.toHaveTextContent("No Deliveries");
	});

	it("names the Deliveries in the instance's own word, empty and failed alike", async () => {
		terms.current = {
			...seededTerms,
			[TERMINOLOGY_KEYS.DELIVERIES]: "Releases",
		};
		renderTheTable("portfolios", [PORTFOLIO_APOLLO, aPortfolio(4, "Gemini")], {
			deliveryService: deliveriesAnswering(
				vi.fn((portfolioId: number) =>
					portfolioId === APOLLO.id
						? Promise.resolve(activeDeliveries())
						: Promise.reject(new Error("unreachable")),
				),
			),
		});

		expect(await whenReady("Apollo", "deliveries")).toHaveTextContent(
			"No Releases",
		);
		await waitFor(() =>
			expect(theCellOf("Gemini", "deliveries")).toHaveTextContent(
				"Couldn't load Releases",
			),
		);
		expect(theColumnTitles()).toContain("Releases");
		expect(theColumnTitles()).not.toContain("Deliveries");
	});
});

describe("What the Overview's chip columns offer", () => {
	const theMenuOf = async (
		user: ReturnType<typeof userEvent.setup>,
		column: string,
	): Promise<string[]> => {
		await user.click(screen.getByLabelText(`${column} column menu`));
		const menu = await screen.findByRole("menu");
		return within(menu)
			.getAllByRole("menuitem")
			.map((item) => (item.textContent ?? "").trim());
	};

	// A cell holds chips, not a value, so sorting or filtering by it would reorder or empty the table for no reason.
	it.each(["Reports", "Deliveries"])(
		"lets a reader hide the %s column but neither sort nor filter by it",
		async (column) => {
			const user = userEvent.setup();
			renderTheTable("portfolios", [PORTFOLIO_APOLLO]);
			await whenReady("Apollo", "reports");
			await whenReady("Apollo", "deliveries");

			const offered = await theMenuOf(user, column);

			expect(offered).toContain("Hide column");
			expect(offered.some((item) => /sort/i.test(item))).toBe(false);
			expect(offered.some((item) => /filter/i.test(item))).toBe(false);
		},
	);

	it("stacks a cell's chips, and a dimmed cell takes no clicks while it is read again", async () => {
		const reread = deferred<ReturnType<typeof activeDeliveries>>();
		const { queryClient } = renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi
					.fn()
					.mockResolvedValueOnce(
						activeDeliveries(
							aDelivery(1, "Winter Release"),
							aDelivery(2, "Autumn Release"),
						),
					)
					.mockReturnValue(reread.promise),
			),
		});
		const ready = await whenReady("Apollo", "deliveries");
		const stack = within(ready)
			.getByText(/^Winter Release/)
			.closest("a")?.parentElement;
		expect(stack).toHaveStyle({ display: "flex", flexDirection: "column" });

		act(() => {
			fireAndForget(
				queryClient.invalidateQueries({
					queryKey: ["deliveries", "portfolio", APOLLO.id],
				}),
			);
		});

		const cell = theCellOf("Apollo", "deliveries");
		await waitFor(() =>
			expect(theLookOf(cell).dataset.cellStatus).toBe("loading"),
		);
		const chip = within(cell).getByText(/^Winter Release/);
		expect(theBoxDimmingTheChip(cell, chip)).toHaveStyle({
			pointerEvents: "none",
		});
	});
});

describe("A Delivery's chip on the Overview", () => {
	it("says Cannot forecast for a Delivery that has no likelihood yet", async () => {
		const unforecast = aDelivery(1, "Winter Release");
		unforecast.likelihoodPercentage = null;
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi.fn().mockResolvedValue(activeDeliveries(unforecast)),
			),
		});

		const cell = await whenReady("Apollo", "deliveries");

		expect(linkNamesIn(cell)[0]).toMatch(/\| Cannot forecast$/);
	});

	it("says Cannot forecast when a Team has no history to forecast from, whatever the likelihood", async () => {
		const blocked = aDelivery(1, "Winter Release");
		blocked.teamsWithoutForecast = ["Rocket"];
		renderTheTable("portfolios", [PORTFOLIO_APOLLO], {
			deliveryService: deliveriesAnswering(
				vi.fn().mockResolvedValue(activeDeliveries(blocked)),
			),
		});

		const cell = await whenReady("Apollo", "deliveries");

		expect(linkNamesIn(cell)[0]).toMatch(/\| Cannot forecast$/);
	});
});
