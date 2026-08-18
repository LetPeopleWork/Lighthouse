import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature, type IFeature } from "../../../models/Feature";
import { createDependsOnColumn, createStateColumn } from "./columns";
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

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "feature") return "Feature";
			if (key === "features") return "Features";
			return "Unknown";
		},
	}),
}));

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Rebuild the search index",
		forecasts: [],
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

const renderCell = (row: IFeature) => {
	const column = createDependsOnColumn("Features");
	return render(column.renderCell?.({ row, value: row.dependsOnCount }));
};

describe("createDependsOnColumn", () => {
	it("shows how many Features this one is waiting on", () => {
		renderCell(feature({ dependsOnCount: 2 }));

		expect(screen.getByText("2")).toBeInTheDocument();
	});

	it("leaves the cell empty when the Feature waits on nothing", () => {
		const { container } = renderCell(feature());

		expect(container.textContent).toBe("");
	});

	it("reads a counted none as nothing too, never as a literal zero", () => {
		const { container } = renderCell(feature({ dependsOnCount: 0 }));

		expect(container.textContent).toBe("");
	});

	it("takes the vocabulary it is given rather than naming the concept itself", () => {
		expect(createDependsOnColumn("Epics").headerName).toBe("Depends On Epics");
	});

	it("never borrows the word an instance may already have renamed for board-blocked work", () => {
		expect(createDependsOnColumn("Features").headerName).not.toMatch(/block/i);
	});

	it("stays sortable, so a list can be read waiting-most first", () => {
		expect(createDependsOnColumn("Features").sortable).toBe(true);
	});

	it("sorts on the number itself, not on what the cell happens to print", () => {
		const column = createDependsOnColumn("Features");

		expect(
			column.valueGetter?.(undefined, feature({ dependsOnCount: 3 })),
		).toBe(3);
	});
});

const featureRow = (dependsOnCount?: number): Feature => {
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
	row.dependsOnCount = dependsOnCount;
	return row;
};

describe("the same count is read on both Feature lists, because there is only one of them", () => {
	beforeEach(() => {
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

	// Neither Feature list can supply this column: the shared grid composes it, so there is one
	// definition to disagree with itself. A caller passing only its own columns still gets it.
	it("comes from the shared grid, so no surface can hand in its own version", () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={[featureRow(2)]}
					columns={[createStateColumn()]}
					storageKey="depends-on-shared-grid"
					hideCompletedStorageKey="depends-on-shared-grid-hide-completed"
				/>
			</MemoryRouter>,
		);

		expect(
			screen.getByRole("columnheader", { name: "Depends On Features" }),
		).toBeInTheDocument();
		expect(screen.getByText("2")).toBeInTheDocument();
	});
});
