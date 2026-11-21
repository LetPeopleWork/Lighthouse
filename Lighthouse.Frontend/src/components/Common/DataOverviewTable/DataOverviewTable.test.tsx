import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { aReportList, aReportService } from "../../../tests/ReportsTestKit";
import DataOverviewTable from "./DataOverviewTable";

const mockNavigate = vi.fn();
vi.mock("react-router", async () => {
	const actual = await vi.importActual("react-router");
	return {
		...actual,
		useNavigate: () => mockNavigate,
	};
});

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				deliveries: "Deliveries",
				feature: "Epic",
				features: "Epics",
			};
			return terms[key] || key;
		},
	}),
}));

// The grid measures its own box with layout, which jsdom has none of. The tests state the box the
// person would see, and the grid is left to react to it as it does in a browser.
const box = vi.hoisted(() => ({ content: 0, viewport: 0, scrollLeft: 0 }));

vi.mock("@mui/x-data-grid", async () => {
	const actual =
		await vi.importActual<typeof import("@mui/x-data-grid")>(
			"@mui/x-data-grid",
		);
	const stated = new WeakSet<object>();
	const statedApis = new WeakMap<object, object>();

	const stateTheBox = (api: object) => {
		const known = statedApis.get(api);
		if (known) return known;
		const real = api as { getRootDimensions?: () => object };
		const proxy = new Proxy(api, {
			get(target, property) {
				if (property === "getRootDimensions") {
					return () => ({
						...real.getRootDimensions?.(),
						contentSize: { width: box.content, height: 0 },
						viewportInnerSize: { width: box.viewport, height: 0 },
					});
				}
				if (property === "getScrollPosition") {
					return () => ({ left: box.scrollLeft, top: 0 });
				}
				return Reflect.get(target, property);
			},
		});
		statedApis.set(api, proxy);
		return proxy;
	};

	return {
		...actual,
		useGridApiRef: () => {
			const ref = actual.useGridApiRef();
			if (!stated.has(ref)) {
				stated.add(ref);
				let current = ref.current as object | undefined;
				Object.defineProperty(ref, "current", {
					configurable: true,
					get: () => (current ? stateTheBox(current) : current),
					set: (next: object | undefined) => {
						current = next;
					},
				});
			}
			return ref;
		},
	};
});

// Mock matchMedia before each test
beforeEach(() => {
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query) => ({
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

// Every row reads its owner's reports, and a Portfolio row its Deliveries, so the table needs the
// query cache and the services even where a test looks at neither.
const renderWithRouter = (ui: React.ReactNode) => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const services = createMockApiServiceContext({
		reportService: aReportService({
			getReports: vi.fn().mockResolvedValue(aReportList([])),
		}),
	});
	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={services}>
				<BrowserRouter>{ui}</BrowserRouter>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

// Sample team data (no project-specific fields)
const sampleTeamData: IFeatureOwner[] = [
	{
		id: 1,
		name: "Team 1",
		remainingFeatures: 5,
		features: [],
		tags: ["critical", "frontend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 2,
		name: "Team 2",
		remainingFeatures: 15,
		features: [],
		tags: ["backend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 3,
		name: "Another Team",
		remainingFeatures: 25,
		features: [],
		tags: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
];

// Sample portfolio data (with portfolio-specific fields)
const samplePortfolioData: IPortfolio[] = [
	{
		id: 1,
		name: "Project 1",
		remainingFeatures: 5,
		features: [],
		tags: ["critical", "frontend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		involvedTeams: [],
		featureSizeTargetProbability: 0,
		featureSizeTargetRange: 0,
	},
	{
		id: 2,
		name: "Project 2",
		remainingFeatures: 15,
		features: [],
		tags: ["backend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		involvedTeams: [],
		featureSizeTargetProbability: 0,
		featureSizeTargetRange: 0,
	},
];

describe("DataOverviewTable", () => {
	describe("with Teams data (using DataGrid)", () => {
		it("renders DataGrid correctly for teams", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByTestId("datagrid-container")).toBeInTheDocument();
		});

		it("displays all team items in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			for (const item of sampleTeamData) {
				expect(screen.getByText(item.name)).toBeInTheDocument();
			}
		});

		it("filters teams correctly in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="Team 1"
				/>,
			);

			expect(screen.getByText("Team 1")).toBeInTheDocument();
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument();
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument();
		});

		it("displays tags in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			expect(screen.getByText("critical")).toBeInTheDocument();
			expect(screen.getByText("frontend")).toBeInTheDocument();
			expect(screen.getByText("backend")).toBeInTheDocument();
		});

		it("filters teams by tag in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="critical"
				/>,
			);

			expect(screen.getByText("Team 1")).toBeInTheDocument();
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument();
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument();
		});
	});

	describe("with Portfolios data (using DataGrid)", () => {
		it("renders DataGrid correctly for portfolios", () => {
			renderWithRouter(
				<DataOverviewTable
					data={samplePortfolioData}
					title="portfolios"
					api="portfolios"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByTestId("datagrid-container")).toBeInTheDocument();
		});

		it("displays all portfolios items in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={samplePortfolioData}
					title="portfolios"
					api="portfolios"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			for (const item of samplePortfolioData) {
				expect(screen.getByText(item.name)).toBeInTheDocument();
			}
		});
	});

	describe("Common functionality", () => {
		it("displays items in alphabetical order by name", () => {
			const unsortedTeamData: IFeatureOwner[] = [
				{
					id: 3,
					name: "Zebra Team",
					remainingFeatures: 25,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
				{
					id: 1,
					name: "Apple Team",
					remainingFeatures: 5,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
				{
					id: 2,
					name: "Banana Team",
					remainingFeatures: 15,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
			];

			renderWithRouter(
				<DataOverviewTable
					data={unsortedTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			// Check that they appear in alphabetical order
			expect(screen.getByText("Apple Team")).toBeInTheDocument();
			expect(screen.getByText("Banana Team")).toBeInTheDocument();
			expect(screen.getByText("Zebra Team")).toBeInTheDocument();
		});

		it("displays the custom message when no item matches filter", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="Non-existing Team"
				/>,
			);

			expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
		});

		it("should not display empty tags", () => {
			const dataWithEmptyTag: IFeatureOwner[] = [
				{
					id: 1,
					name: "Item with empty tag",
					remainingFeatures: 5,
					features: [],
					tags: ["valid-tag", "", "  ", "another-valid-tag"],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
			];

			renderWithRouter(
				<DataOverviewTable
					data={dataWithEmptyTag}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			expect(screen.getByText("valid-tag")).toBeInTheDocument();
			expect(screen.getByText("another-valid-tag")).toBeInTheDocument();

			const tagChips = document.querySelectorAll(".MuiChip-outlined");
			expect(tagChips.length).toEqual(2);
		});

		it("shows Clone action in Actions cell when rendering teams", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			expect(cloneButtons).toHaveLength(sampleTeamData.length);
		});

		it("navigates to clone URL when Clone button is clicked", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData.slice(0, 1)} // Only render one team for this test
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButton = screen.getByLabelText("Clone");
			fireEvent.click(cloneButton);

			expect(mockNavigate).toHaveBeenCalledWith("/teams/new?cloneFrom=1");
		});

		it("shows Clone action for portfolios", () => {
			renderWithRouter(
				<DataOverviewTable
					data={samplePortfolioData}
					title="portfolios"
					api="portfolios"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			expect(cloneButtons).toHaveLength(samplePortfolioData.length);
		});

		it("navigates to clone URL when Clone button is clicked for portfolios", () => {
			renderWithRouter(
				<DataOverviewTable
					data={samplePortfolioData}
					title="Portfolios"
					api="portfolios"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			fireEvent.click(cloneButtons[0]);

			expect(mockNavigate).toHaveBeenCalledWith("/portfolios/new?cloneFrom=1");
		});

		it("filters items by partial tag match", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="front"
				/>,
			);

			expect(screen.getByText("Team 1")).toBeInTheDocument(); // Team 1 has "frontend" tag
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument(); // Team 2 doesn't have a tag containing "front"
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument(); // Another Team doesn't have tags
		});

		it("filters by tag when name doesn't match", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="backend"
				/>,
			);

			expect(screen.queryByText("Team 1")).not.toBeInTheDocument(); // Team 1 doesn't have "backend" tag
			expect(screen.getByText("Team 2")).toBeInTheDocument(); // Team 2 has "backend" tag
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument(); // Another Team doesn't have tags
		});

		it("should not show items when neither name nor tags match", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="asdfasdfasdfasdf"
				/>,
			);

			expect(screen.queryByText("Team 1")).not.toBeInTheDocument();
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument();
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument();
			expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
		});

		it("hides Edit icon on rows where canEditRow returns false but keeps it where true", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
					canEditRow={(row) => row.id !== 1}
				/>,
			);

			const editLinks = screen
				.getAllByRole("link")
				.filter((a) => a.getAttribute("href")?.startsWith("/teams/edit/"));
			const editHrefs = editLinks.map((a) => a.getAttribute("href"));
			expect(editHrefs).not.toContain("/teams/edit/1");
			expect(editHrefs).toContain("/teams/edit/2");
			expect(editHrefs).toContain("/teams/edit/3");
		});

		it("hides Clone icon on rows where canCloneRow returns false but keeps it where true", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
					canCloneRow={(row) => row.id !== 2}
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			expect(cloneButtons).toHaveLength(sampleTeamData.length - 1);
		});

		it("hides Delete icon on rows where canDeleteRow returns false but keeps it where true", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
					canDeleteRow={(row) => row.id === 3}
				/>,
			);

			const deleteButtons = screen.getAllByTestId("delete-item-button");
			expect(deleteButtons).toHaveLength(1);
		});

		it("renders Edit, Clone, and Delete on every row when no predicates are provided", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const editLinks = screen
				.getAllByRole("link")
				.filter((a) => a.getAttribute("href")?.startsWith("/teams/edit/"));
			expect(editLinks).toHaveLength(sampleTeamData.length);
			expect(screen.getAllByLabelText("Clone")).toHaveLength(
				sampleTeamData.length,
			);
			expect(screen.getAllByTestId("delete-item-button")).toHaveLength(
				sampleTeamData.length,
			);
		});

		it("always renders the Details icon on every row regardless of predicates", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
					canEditRow={() => false}
					canCloneRow={() => false}
					canDeleteRow={() => false}
				/>,
			);

			const detailsLinks = screen
				.getAllByRole("link")
				.filter(
					(a) =>
						a.getAttribute("href")?.startsWith("/teams/") &&
						!a.getAttribute("href")?.startsWith("/teams/edit/"),
				);
			const detailsHrefs = detailsLinks.map((a) => a.getAttribute("href"));
			expect(detailsHrefs).toContain("/teams/1");
			expect(detailsHrefs).toContain("/teams/2");
			expect(detailsHrefs).toContain("/teams/3");
		});

		it("renders Features column header with custom terminology", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByText("Epics")).toBeInTheDocument();
		});

		it("renders feature count cell with custom terminology", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData.slice(0, 1)}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByText("5 Epics")).toBeInTheDocument();
		});

		it("renders singular feature count with custom terminology", () => {
			const singularData: IFeatureOwner[] = [
				{
					id: 1,
					name: "Team 1",
					remainingFeatures: 1,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
			];
			renderWithRouter(
				<DataOverviewTable
					data={singularData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByText("1 Epic")).toBeInTheDocument();
		});

		it("shows demo data link when no data is available", () => {
			renderWithRouter(
				<DataOverviewTable
					data={[]}
					title="Test Items"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			expect(screen.getByTestId("empty-items-message")).toBeInTheDocument();

			// Check that the demo data link is present and has correct href
			const demoDataLink = screen.getByText("Load Demo Data");
			expect(demoDataLink).toBeInTheDocument();
			expect(demoDataLink.closest("a")).toHaveAttribute(
				"href",
				"/settings/system/demo-data",
			);

			// Check that the documentation link is also present
			const docLink = screen.getByText("Check the documentation");
			expect(docLink).toBeInTheDocument();
			expect(docLink.closest("a")).toHaveAttribute(
				"href",
				"https://docs.lighthouse.letpeople.work",
			);
		});
	});
});

describe("The Overview table fits its columns and says when some are out of sight", () => {
	const LONG_TEAM_NAME =
		"Platform Engineering Delivery Squad for the Northern Region with cross-functional ownership of billing";
	const PINNED_CLASS = "lighthouse-pinned-actions";

	const aLongNamedTeam: IFeatureOwner = {
		...sampleTeamData[0],
		name: LONG_TEAM_NAME,
		remainingFeatures: 5,
		tags: ["critical"],
	};

	beforeEach(() => {
		box.content = 900;
		box.viewport = 400;
		box.scrollLeft = 0;
	});

	afterEach(() => {
		vi.restoreAllMocks();
		vi.useRealTimers();
		localStorage.clear();
		box.content = 0;
		box.viewport = 0;
	});

	const whenTheOverviewShows = (
		data: IFeatureOwner[],
		api: "teams" | "portfolios" = "teams",
	) =>
		renderWithRouter(
			<DataOverviewTable
				data={data}
				title={api}
				api={api}
				onDelete={vi.fn()}
				filterText=""
			/>,
		);

	const whenTheScreenIsAPhone = () => {
		Object.defineProperty(globalThis, "matchMedia", {
			writable: true,
			value: vi.fn().mockImplementation((query: string) => ({
				matches: true,
				media: query,
				onchange: null,
				addListener: vi.fn(),
				removeListener: vi.fn(),
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
				dispatchEvent: vi.fn(),
			})),
		});
	};

	/** The browser cuts a name wider than its column; jsdom has no layout, so the test states the cut. */
	const givenNamesAreWiderThanTheirColumn = () => {
		vi.spyOn(HTMLElement.prototype, "scrollWidth", "get").mockReturnValue(600);
		vi.spyOn(HTMLElement.prototype, "clientWidth", "get").mockReturnValue(160);
	};

	const cellOf = (container: HTMLElement, field: string) => {
		const cell = container.querySelector<HTMLElement>(
			`[role="gridcell"][data-field="${field}"]`,
		);
		expect(cell, `the table must show a ${field} cell`).not.toBeNull();
		return cell as HTMLElement;
	};

	const headerOf = (container: HTMLElement, field: string) => {
		const header = container.querySelector<HTMLElement>(
			`[role="columnheader"][data-field="${field}"]`,
		);
		expect(header, `the table must show a ${field} column`).not.toBeNull();
		return header as HTMLElement;
	};

	/** What the cell draws inside itself; the grid's own cell box ends every cell with an ellipsis. */
	const cutElementOf = (cell: HTMLElement) =>
		Array.from(cell.querySelectorAll<HTMLElement>("*")).find(
			(element) => getComputedStyle(element).textOverflow === "ellipsis",
		);

	const isCutWithEllipsis = (cell: HTMLElement) =>
		cutElementOf(cell) !== undefined;

	const widthOf = (header: HTMLElement) => {
		const width = Number.parseFloat(header.style.width);
		expect(
			Number.isFinite(width),
			`the ${header.dataset.field} header must carry a pixel width, or two missing widths would compare as equal`,
		).toBe(true);
		return width;
	};

	const theGridDescription = (grid: HTMLElement) =>
		(grid.getAttribute("aria-describedby") ?? "")
			.split(" ")
			.map((id) => document.getElementById(id)?.textContent ?? "")
			.join(" ")
			.trim();

	it("cuts a long Team name to its column with the full name in a tooltip, and keeps numbers and dates whole", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		givenNamesAreWiderThanTheirColumn();
		const { container } = whenTheOverviewShows([aLongNamedTeam]);

		expect(
			isCutWithEllipsis(cellOf(container, "name")),
			"a Team name longer than its column must end with an ellipsis instead of wrapping the row",
		).toBe(true);
		expect(
			getComputedStyle(cutElementOf(cellOf(container, "name")) as HTMLElement)
				.whiteSpace,
			"the cut name must stay on one line, or the ellipsis never shows and the row grows taller",
		).toBe("nowrap");
		expect(
			isCutWithEllipsis(cellOf(container, "tags")),
			"the tags are text and must give way to the other columns like the name does",
		).toBe(true);
		for (const field of ["remainingFeatures", "lastUpdated"]) {
			expect(
				isCutWithEllipsis(cellOf(container, field)),
				`the ${field} cell must show its whole content; numbers and dates are never cut`,
			).toBe(false);
		}

		fireEvent.mouseOver(screen.getByText("5 Epics"));
		act(() => {
			vi.advanceTimersByTime(1500);
		});
		expect(
			screen.queryByRole("tooltip"),
			"a number is shown whole, so hovering it must open no tooltip",
		).toBeNull();

		fireEvent.mouseOver(screen.getByText(LONG_TEAM_NAME));
		expect(
			await screen.findByRole("tooltip"),
			"hovering the cut Team name must show the full name in a tooltip",
		).toHaveTextContent(LONG_TEAM_NAME);
	});

	it("pins the actions column as the last column of a Teams table and of a Portfolios table", () => {
		const teams = whenTheOverviewShows([aLongNamedTeam]);
		const portfolios = whenTheOverviewShows(samplePortfolioData, "portfolios");

		for (const [shown, label] of [
			[teams.container, "Teams"],
			[portfolios.container, "Portfolios"],
		] as const) {
			const headers = Array.from(
				shown.querySelectorAll('[role="columnheader"]'),
			).map((header) => (header as HTMLElement).dataset.field);
			expect(
				headers.at(-1),
				`the ${label} columns are ${JSON.stringify(headers)}; the actions column is pinned to the right edge, so it must be the last one`,
			).toBe("actions");
			expect(
				headerOf(shown, "actions").classList.contains(PINNED_CLASS),
				`the ${label} Actions header must carry the pinned class, or Delete scrolls out of view`,
			).toBe(true);
			expect(
				cellOf(shown, "actions").classList.contains(PINNED_CLASS),
				`the ${label} actions cells must carry the pinned class, or Delete scrolls out of view`,
			).toBe(true);
		}
		expect(
			isCutWithEllipsis(cellOf(portfolios.container, "deliveries")),
			"the Deliveries cell must show its whole content; it is never cut",
		).toBe(false);
	});

	it.each([
		["puts it first", ["actions", "name", "tags"]],
		["does not name it", ["tags", "name", "lastUpdated"]],
	])(
		"keeps the actions column last when the person's saved column order %s",
		(_case, columnOrder) => {
			localStorage.setItem(
				"lighthouse:datagrid:data-overview-teams:state",
				JSON.stringify({ columnOrder }),
			);
			const { container } = whenTheOverviewShows([aLongNamedTeam]);

			const headers = Array.from(
				container.querySelectorAll('[role="columnheader"]'),
			).map((header) => (header as HTMLElement).dataset.field);
			expect(
				headers.at(-1),
				`the columns are ${JSON.stringify(headers)}; whatever order the person saved, the pinned actions column must stay last`,
			).toBe("actions");
		},
	);

	it("exposes that columns are out of sight, as the report list does", () => {
		const { container } = whenTheOverviewShows([aLongNamedTeam]);

		expect(
			container.querySelector<HTMLElement>("[data-overflow]")?.dataset.overflow,
			"the table must mark that columns are hidden on its right while it is scrolled to its left edge",
		).toBe("right");
		expect(
			container.querySelector('[aria-hidden="true"][data-side="right"]'),
			"a shadow must sit on the right edge so the person sees there is more to scroll to",
		).not.toBeNull();
		expect(
			theGridDescription(screen.getByRole("grid")),
			"a screen reader user must hear which side has hidden columns",
		).toBe("This table scrolls sideways. More columns to the right.");
	});

	it("shows no cue and no description when the Overview table fits its box", () => {
		box.viewport = 1000;
		const { container } = whenTheOverviewShows([aLongNamedTeam]);

		expect(
			container.querySelector<HTMLElement>("[data-overflow]")?.dataset.overflow,
			"a table that fits must not claim hidden columns",
		).toBe("none");
		expect(
			container.querySelector('[aria-hidden="true"][data-side]'),
			"a table that fits must draw no edge shadow",
		).toBeNull();
		expect(
			theGridDescription(screen.getByRole("grid")),
			"a table that fits must not tell a screen reader that it scrolls sideways",
		).toBe("");
	});

	it("gives the full name of a cut Team in a tooltip when the person reaches it with the keyboard", async () => {
		givenNamesAreWiderThanTheirColumn();
		whenTheOverviewShows([aLongNamedTeam]);

		fireEvent.focusIn(screen.getByRole("link", { name: LONG_TEAM_NAME }));

		expect(
			await screen.findByRole("tooltip"),
			"a keyboard user who focuses a cut Team name must get the full name in a tooltip, not only a mouse user",
		).toHaveTextContent(LONG_TEAM_NAME);
	});

	const MENU_MAXIMUM_WIDTH = 56;
	const MENU_MINIMUM_WIDTH = 40;

	const menuButtonOf = (name: string) =>
		screen.getByRole("button", { name: `Actions for ${name}` });

	const iconActionsShown = () => [
		...screen.queryAllByLabelText("Clone"),
		...screen.queryAllByTestId("delete-item-button"),
		...Array.from(
			document.querySelectorAll<HTMLElement>('[data-field="actions"] a'),
		).filter((link) =>
			/^\/teams\/(edit\/)?\d+$/.test(link.getAttribute("href") ?? ""),
		),
	];

	const openTheActionsMenuOf = async (name: string) => {
		fireEvent.click(menuButtonOf(name));
		return await screen.findByRole("menu");
	};

	const itemsOf = (menu: HTMLElement) =>
		within(menu)
			.getAllByRole("menuitem")
			.map((item) => (item.textContent ?? "").trim());

	const thenThePersonIsTakenTo = (path: string) => {
		const where = `${globalThis.location.pathname}${globalThis.location.search}`;
		const navigatedInCode = mockNavigate.mock.calls.some(
			([target]) => target === path,
		);
		expect(
			navigatedInCode || where === path,
			`choosing the item must take the person to ${path}; the page is at ${where} and the code navigated to ${JSON.stringify(mockNavigate.mock.calls)}. The menu item must do what the icon did.`,
		).toBe(true);
	};

	describe("on a phone, where four icons do not fit beside a readable Name", () => {
		beforeEach(() => {
			mockNavigate.mockClear();
			globalThis.history.pushState({}, "", "/");
			whenTheScreenIsAPhone();
		});

		it("collapses the four action icons into one pinned menu button per row", async () => {
			const { container } = whenTheOverviewShows(sampleTeamData.slice(0, 2));

			expect(
				screen.queryAllByRole("button", { name: /^Actions for / }),
				"every row must carry exactly one menu button named after its Team, so a screen reader announces whose actions open",
			).toHaveLength(2);
			expect(
				iconActionsShown(),
				"on a phone the Details, Edit, Clone and Delete icons must be gone from the rows; they live in the menu, or the Name column has no room",
			).toEqual([]);
			expect(menuButtonOf("Team 1")).toHaveAttribute("aria-expanded", "false");
			expect(
				menuButtonOf("Team 1").closest('[data-field="actions"]')?.classList,
				"the menu button must sit in the pinned actions cell, or it scrolls out of view with the other columns",
			).toContain(PINNED_CLASS);
			expect(
				headerOf(container, "actions").classList.contains(PINNED_CLASS),
				"the actions header must stay pinned on a phone",
			).toBe(true);
			const headers = Array.from(
				container.querySelectorAll('[role="columnheader"]'),
			).map((header) => (header as HTMLElement).dataset.field);
			expect(headers.at(-1), `the columns are ${JSON.stringify(headers)}`).toBe(
				"actions",
			);
			const width = widthOf(headerOf(container, "actions"));
			expect(
				width,
				`the actions column is only as wide as the menu button on a phone (${MENU_MINIMUM_WIDTH} to ${MENU_MAXIMUM_WIDTH} px); it is ${width} px, which leaves the Name less room than it needs`,
			).toBeLessThanOrEqual(MENU_MAXIMUM_WIDTH);
			expect(
				width,
				"the column must still be wide enough to hold the menu button",
			).toBeGreaterThanOrEqual(MENU_MINIMUM_WIDTH);

			fireEvent.mouseOver(menuButtonOf("Team 1"));
			expect(
				await screen.findByRole("tooltip"),
				"hovering the menu button must say what it is",
			).toHaveTextContent("Actions");
		});

		it.each([
			[
				"lists all four when the person may use each",
				() => true,
				() => true,
				() => true,
				["Details", "Edit", "Clone", "Delete"],
			],
			[
				"lists Details alone when the person may do nothing else",
				() => false,
				() => false,
				() => false,
				["Details"],
			],
			[
				"leaves out Clone when the person may edit and delete but not clone",
				() => true,
				() => false,
				() => true,
				["Details", "Edit", "Delete"],
			],
		])("the menu %s", async (_case, canEdit, canClone, canDelete, expected) => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData.slice(0, 1)}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
					canEditRow={canEdit}
					canCloneRow={canClone}
					canDeleteRow={canDelete}
				/>,
			);

			const menu = await openTheActionsMenuOf("Team 1");

			expect(
				itemsOf(menu),
				"the menu must list exactly the actions the person may use, in the order the icons had, so no action is offered that the server would refuse",
			).toEqual(expected);
			expect(menuButtonOf("Team 1")).toHaveAttribute("aria-expanded", "true");
		});

		it.each([
			["Details", "teams", "/teams/1"],
			["Edit", "teams", "/teams/edit/1"],
			["Clone", "teams", "/teams/new?cloneFrom=1"],
			["Clone", "portfolios", "/portfolios/new?cloneFrom=1"],
		] as const)(
			"choosing %s from the menu of a %s row does what its icon did",
			async (item, api, path) => {
				renderWithRouter(
					<DataOverviewTable
						data={
							api === "teams"
								? sampleTeamData.slice(0, 1)
								: samplePortfolioData.slice(0, 1)
						}
						title={api}
						api={api}
						onDelete={vi.fn()}
						filterText=""
					/>,
				);
				const menu = await openTheActionsMenuOf(
					api === "teams" ? "Team 1" : "Project 1",
				);

				fireEvent.click(within(menu).getByRole("menuitem", { name: item }));

				thenThePersonIsTakenTo(path);
			},
		);

		it("opens the Delete confirmation for the row when Delete is chosen from the menu", async () => {
			const onDelete = vi.fn();
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData.slice(0, 2)}
					title="teams"
					api="teams"
					onDelete={onDelete}
					filterText=""
				/>,
			);
			const menu = await openTheActionsMenuOf("Team 2");

			fireEvent.click(within(menu).getByRole("menuitem", { name: "Delete" }));

			expect(
				onDelete,
				"choosing Delete must ask for the confirmation of this very row, exactly once; the icon did the same",
			).toHaveBeenCalledExactlyOnceWith(sampleTeamData[1]);
		});

		it("returns the keyboard focus to the row's menu button when Escape closes the menu", async () => {
			const user = userEvent.setup();
			givenNamesAreWiderThanTheirColumn();
			box.content = 900;
			box.viewport = 400;
			const { container } = whenTheOverviewShows(sampleTeamData.slice(0, 2));
			const edgeCue = () =>
				container.querySelector<HTMLElement>("[data-overflow]")?.dataset
					.overflow;
			const cueBefore = edgeCue();
			expect(
				cueBefore,
				"the table must start with columns hidden on its right, or the check that the menu leaves the shadows alone has nothing to compare",
			).toBe("right");

			menuButtonOf("Team 2").focus();
			await user.keyboard("{Enter}");
			const menu = await screen.findByRole("menu");
			await waitFor(() =>
				expect(
					within(menu).getAllByRole("menuitem")[0],
					"opening the menu must move the keyboard focus into it, so the arrow keys choose an item and Escape has a place to leave from",
				).toHaveFocus(),
			);
			await user.keyboard("{Escape}");

			await waitFor(() =>
				expect(
					screen.queryByRole("menu"),
					"Escape must close the actions menu",
				).toBeNull(),
			);
			expect(
				menuButtonOf("Team 2"),
				"after Escape the focus must go back to the button that opened the menu, so a keyboard user does not lose their place in the table",
			).toHaveFocus();
			expect(
				edgeCue(),
				"opening and closing the menu must leave the edge shadows as they were, so the person still sees which side has more columns",
			).toBe(cueBefore);
		});

		it("opens the menu with Enter and chooses an item with the arrow keys and Enter", async () => {
			const user = userEvent.setup();
			whenTheOverviewShows(sampleTeamData.slice(0, 1));

			menuButtonOf("Team 1").focus();
			await user.keyboard("{Enter}");
			const menu = await screen.findByRole("menu");
			expect(
				itemsOf(menu),
				"the keyboard must open the same menu a click does",
			).toEqual(["Details", "Edit", "Clone", "Delete"]);
			await user.keyboard("{ArrowDown}{Enter}");

			thenThePersonIsTakenTo("/teams/edit/1");
		});
	});

	describe("with the menu closed and the keyboard on a row's menu button", () => {
		beforeEach(() => {
			globalThis.history.pushState({}, "", "/");
			whenTheScreenIsAPhone();
		});

		it("lets ArrowLeft on the menu button move through the row's cells", async () => {
			const user = userEvent.setup();
			const { container } = whenTheOverviewShows(sampleTeamData.slice(0, 1));
			const actionsCell = container.querySelector<HTMLElement>(
				'[role="gridcell"][data-field="actions"]',
			);
			const cellBefore = actionsCell?.previousElementSibling as HTMLElement;

			menuButtonOf("Team 1").focus();
			await user.keyboard("{ArrowLeft}");

			expect(
				document.activeElement?.closest('[role="gridcell"]'),
				"ArrowLeft on the menu button must move the grid's focus to the cell before the actions cell, as it does from any other cell, so a keyboard user is not stuck in the last column",
			).toBe(cellBefore);
		});

		it("opens the menu with Space on the menu button, as Enter does", async () => {
			const user = userEvent.setup();
			whenTheOverviewShows(sampleTeamData.slice(0, 1));

			menuButtonOf("Team 1").focus();
			await user.keyboard(" ");
			expect(
				await screen.findByRole("menu"),
				"Space on the menu button must open the menu, as Enter does",
			).toBeInTheDocument();
		});
	});

	it("keeps the four action icons and no menu button on a desktop", () => {
		const { container } = whenTheOverviewShows(sampleTeamData.slice(0, 2));

		expect(
			screen.queryAllByRole("button", { name: /^Actions for / }),
			"at desktop width the icons have room, so there is no menu button",
		).toEqual([]);
		expect(screen.getAllByLabelText("Clone")).toHaveLength(2);
		expect(screen.getAllByTestId("delete-item-button")).toHaveLength(2);
		expect(
			widthOf(headerOf(container, "actions")),
			"the desktop actions column holds four icons, so it is wider than the phone's menu column",
		).toBeGreaterThan(MENU_MAXIMUM_WIDTH);
	});
});
