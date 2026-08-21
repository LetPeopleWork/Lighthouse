import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
import type { IFeatureDependency } from "../../../models/FeatureDependency";
import type { DependencyTerms } from "../../../utils/dependencies/dependencySentences";
import { createDependsOnColumn, createStateColumn } from "./columns";

const THE_DEFAULT_TERMS: DependencyTerms = {
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
};

import FeatureListDataGrid from "./FeatureListDataGrid";

vi.mock("../../../hooks/useFeatureOrdering", () => ({
	useFeatureOrdering: () => ({
		policy: "SourceOrder",
		positionColumnLabel: "#",
		resolveMoveGate: () => ({
			enabled: false,
			reason: "policy-off",
			blockingPortfolios: [],
		}),
		refresh: vi.fn(),
	}),
}));

const SEEDED_TERMS: Record<string, string> = {
	feature: "Feature",
	features: "Features",
	blocked: "Blocked",
};

const terminology = vi.hoisted(() => ({
	terms: {} as Record<string, string>,
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terminology.terms[key] ?? "Unknown",
	}),
}));

beforeEach(() => {
	terminology.terms = { ...SEEDED_TERMS };
	localStorage.clear();
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

const aDependency = (
	overrides: Partial<IFeatureDependency> = {},
): IFeatureDependency => ({
	referenceId: "FTR-9",
	name: "Warehouse sync",
	url: "https://tracker.example/FTR-9",
	source: "TrackerLink",
	notHonouredReason: null,
	blockerPositionedBelow: false,
	isWithheld: false,
	...overrides,
});

const feature = (dependsOn: IFeatureDependency[] = []): IFeature =>
	({
		id: 1,
		name: "Rebuild the search index",
		referenceId: "FTR-1",
		forecasts: [],
		teamsWithoutForecast: [],
		dependsOn,
	}) as unknown as IFeature;

const renderCell = (row: IFeature) => {
	const column = createDependsOnColumn(THE_DEFAULT_TERMS);
	return render(
		column.renderCell?.({ row, value: (row.dependsOn ?? []).length }),
	);
};

describe("createDependsOnColumn", () => {
	it("names each Feature this one waits on, id first", () => {
		renderCell(feature([aDependency()]));

		expect(screen.getByText("FTR-9: Warehouse sync")).toBeInTheDocument();
	});

	// A reader scanning this column is looking for which Features are involved. Two of them running
	// into one line is a line they have to read twice.
	it("gives every Feature waited on a line of its own", () => {
		renderCell(
			feature([
				aDependency(),
				aDependency({ referenceId: "FTR-8", name: "Payment gateway upgrade" }),
			]),
		);

		expect(screen.getByText("FTR-9: Warehouse sync")).toBeInTheDocument();
		expect(
			screen.getByText("FTR-8: Payment gateway upgrade"),
		).toBeInTheDocument();
		expect(screen.getAllByRole("link")).toHaveLength(2);
	});

	// Deciding what to do about a wait happens in the work tracking system, and the reader is in the
	// middle of reading a list here - sending them away from it would cost them their place.
	it("opens the record in the work tracking system in a new tab", () => {
		renderCell(feature([aDependency()]));

		const link = screen.getByRole("link", { name: "FTR-9: Warehouse sync" });
		expect(link).toHaveAttribute("href", "https://tracker.example/FTR-9");
		expect(link).toHaveAttribute("target", "_blank");
		expect(link).toHaveAttribute("rel", expect.stringContaining("noopener"));
	});

	// An anchor with nowhere to go still looks and behaves like one. Asserted on the element itself
	// rather than on the link role, which an anchor without an href does not have anyway.
	it("still names a Feature the work tracking system gave no link to", () => {
		const { container } = renderCell(feature([aDependency({ url: null })]));

		expect(screen.getByText("FTR-9: Warehouse sync")).toBeInTheDocument();
		expect(container.querySelectorAll("a")).toHaveLength(0);
	});

	// The row is here because something is being waited on. Naming it would be the disclosure the
	// payload already refused.
	it("says a Feature the reader may not see is there without naming it", () => {
		renderCell(
			feature([
				aDependency({
					referenceId: "",
					name: "",
					url: null,
					isWithheld: true,
					notHonouredReason: "OutsideThisPortfolio",
				}),
			]),
		);

		expect(screen.getByText("No access")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("leaves the cell empty when the Feature waits on nothing", () => {
		const { container } = renderCell(feature());

		expect(container.textContent).toBe("");
	});

	// Setting dependencies aside is not hiding them. The whole point of the switch over editing links in
	// the tracker is that the reader can still see what they set aside.
	it("still names every Feature a Portfolio that set its dependencies aside waits on", () => {
		renderCell(
			feature([aDependency({ notHonouredReason: "IgnoredByPortfolio" })]),
		);

		expect(screen.getByText("FTR-9: Warehouse sync")).toBeInTheDocument();
	});

	// Nothing warns about a set-aside dependency, so this entry is the only place the reader is told.
	it("says on the entry itself that a dependency has been set aside", () => {
		renderCell(
			feature([aDependency({ notHonouredReason: "IgnoredByPortfolio" })]),
		);

		expect(screen.getByTestId("dependency-set-aside")).toBeInTheDocument();
	});

	it("marks nothing as set aside when the dependencies are being acted on", () => {
		renderCell(feature([aDependency(), aDependency({ referenceId: "FTR-8" })]));

		expect(
			screen.queryByTestId("dependency-set-aside"),
		).not.toBeInTheDocument();
	});

	it("is named for the thing itself, in words no instance renames", () => {
		expect(createDependsOnColumn(THE_DEFAULT_TERMS).headerName).toBe(
			"Dependencies",
		);
	});

	it("never borrows the word an instance may already have renamed for board-blocked work", () => {
		expect(createDependsOnColumn(THE_DEFAULT_TERMS).headerName).not.toMatch(
			/block/i,
		);
	});

	it("stays sortable, so a list can be read waiting-most first", () => {
		expect(createDependsOnColumn(THE_DEFAULT_TERMS).sortable).toBe(true);
	});

	// The list has no order a reader would sort by, and the question the column answers first is which
	// rows are entangled at all.
	it("sorts on how many are waited on rather than on what the cell prints", () => {
		const column = createDependsOnColumn(THE_DEFAULT_TERMS);

		expect(
			column.valueGetter?.(
				undefined,
				feature([aDependency(), aDependency({ referenceId: "FTR-8" })]),
			),
		).toBe(2);
	});
});

const featureRow = (dependsOn: IFeatureDependency[]): Feature => {
	const row = new Feature();
	row.id = 1;
	row.name = "Rebuild the search index";
	row.referenceId = "FTR-1";
	row.stateCategory = "ToDo";
	row.state = "Active";
	row.lastUpdated = new Date();
	row.isUsingDefaultFeatureSize = false;
	row.projects = [];
	row.remainingWork = { 1: 5 };
	row.totalWork = { 1: 10 };
	row.forecasts = [];
	row.url = "";
	row.dependsOn = dependsOn;
	return row;
};

const renderGrid = (dependsOn: IFeatureDependency[] = [aDependency()]) =>
	render(
		<MemoryRouter>
			<FeatureListDataGrid
				features={[featureRow(dependsOn)]}
				columns={[createStateColumn()]}
				storageKey="depends-on-shared-grid"
				hideCompletedStorageKey="depends-on-shared-grid-hide-completed"
			/>
		</MemoryRouter>,
	);

describe("the same column is read on both Feature lists, because there is only one of them", () => {
	// Neither Feature list can supply this column: the shared grid composes it, so there is one
	// definition to disagree with itself. A caller passing only its own columns still gets it.
	it("comes from the shared grid, so no surface can hand in its own version", () => {
		renderGrid();

		expect(
			screen.getByRole("columnheader", { name: "Dependencies" }),
		).toBeInTheDocument();
		expect(screen.getByText("FTR-9: Warehouse sync")).toBeInTheDocument();
	});

	// An instance can rename what it calls held-up work, and one screen carrying that word in two
	// meanings is unreadable. Whatever the instance chose, this column must not have borrowed it.
	it("never borrows the word this instance keeps for held-up work", () => {
		terminology.terms.blocked = "Impeded";

		const { container } = renderGrid();
		const columnCells = container.querySelectorAll('[data-field="dependsOn"]');

		expect(columnCells.length).toBeGreaterThan(0);
		for (const cell of columnCells) {
			expect(cell.textContent).not.toMatch(/impeded/i);
			expect(cell.textContent).not.toMatch(/block/i);
		}
	});
});
