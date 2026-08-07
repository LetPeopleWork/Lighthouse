import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../../tests/MockApiServiceProvider";
import {
	createFeatureOrderingActionsColumn,
	createStateColumn,
} from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

/**
 * Epic 5375 slice 03 — the wiring that makes D10's "one change, both surfaces" true. `FeatureMoveMenu`
 * is judged on its own in `FeatureMoveMenu.test.tsx`; what is judged here is that the grid injects it,
 * hands it the neighbours the user can actually see, and turns a chosen gesture into a call.
 */
vi.mock("../../../hooks/useFeatureOrdering", () => ({
	useFeatureOrdering: () => ({
		policy: "ManualOrder",
		positionColumnLabel: "Manual",
		resolveMoveGate: () => ({ enabled: true }),
		refresh: vi.fn(),
	}),
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "features" ? "Features" : "Feature"),
	}),
}));

const aFeature = (id: number, name: string, done = false): Feature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.referenceId = `FTR-${id}`;
	feature.stateCategory = done ? "Done" : "ToDo";
	feature.state = done ? "Closed" : "Active";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = [];
	feature.remainingWork = done ? { 1: 0 } : { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	feature.canMove = true;
	return feature;
};

// The Done Feature sits in the middle on purpose: `hideCompleted` defaults to on, so it is NOT on
// screen, and AC-3.3 says the rows either side of it are each other's neighbours.
const theList = [
	aFeature(1, "Rebuild the search index"),
	aFeature(2, "Shipped last quarter", true),
	aFeature(3, "Retire the legacy importer"),
	aFeature(4, "Publish the partner catalogue"),
];

const renderTheGrid = (showPosition = true) => {
	const featureService = createMockFeatureService();
	const onOrderChanged = vi.fn();

	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ featureService })}
		>
			<FeatureListDataGrid
				features={theList}
				columns={[createStateColumn()]}
				storageKey="move-actions"
				hideCompletedStorageKey="move-actions-hide-completed"
				showPosition={showPosition}
				onOrderChanged={onOrderChanged}
			/>
		</ApiServiceContext.Provider>,
	);

	return { featureService, onOrderChanged };
};

const chooseOn = async (featureName: string, gesture: string) => {
	await userEvent.click(
		screen.getByRole("button", { name: `Move ${featureName}` }),
	);
	const item = screen.getByRole("menuitem", { name: gesture });
	await userEvent.click(item);
	return item;
};

describe("FeatureListDataGrid — the move actions it injects", () => {
	beforeEach(() => {
		localStorage.clear();
		vi.clearAllMocks();
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

	// D14 / AC-3.9 — the actions column is the one column that must never decide the list's order, or
	// clicking its header would silently disable the very actions it holds.
	it("is a column you cannot sort or filter the list by", () => {
		const column = createFeatureOrderingActionsColumn({
			resolveGate: () => ({ enabled: true }),
			neighboursFor: () => ({}),
			onMove: vi.fn(),
		});

		expect(column.sortable).toBe(false);
		expect(column.filterable).toBe(false);
	});

	it("gives every row it shows a way to move it", () => {
		renderTheGrid();

		expect(screen.getAllByRole("button", { name: /^Move / })).toHaveLength(3);
	});

	it("renders no move actions on a surface that does not show the order", () => {
		renderTheGrid(false);

		expect(
			screen.queryByRole("button", { name: /^Move / }),
		).not.toBeInTheDocument();
	});

	it("sends a Feature to the top of the list the user is looking at", async () => {
		const { featureService } = renderTheGrid();

		await chooseOn("Publish the partner catalogue", "Move to Top");

		expect(featureService.moveFeature).toHaveBeenCalledWith(4, {
			beforeFeatureId: 1,
		});
	});

	// AC-3.3 — the hidden Done Feature sits between rows 1 and 3, and is jumped rather than landed on.
	it("moves a Feature above the previous row it can see, not the previous row that exists", async () => {
		const { featureService } = renderTheGrid();

		await chooseOn("Retire the legacy importer", "Move Up");

		expect(featureService.moveFeature).toHaveBeenCalledWith(3, {
			beforeFeatureId: 1,
		});
	});

	it("moves a Feature below the next row it can see", async () => {
		const { featureService } = renderTheGrid();

		await chooseOn("Rebuild the search index", "Move Down");

		expect(featureService.moveFeature).toHaveBeenCalledWith(1, {
			afterFeatureId: 3,
		});
	});

	it("sends a Feature to the end of the order by naming no target at all", async () => {
		const { featureService } = renderTheGrid();

		await chooseOn("Rebuild the search index", "Move to Bottom");

		expect(featureService.moveFeature).toHaveBeenCalledWith(1, {
			beforeFeatureId: null,
		});
	});

	it("offers no way up to the row that is already at the top", async () => {
		const { featureService } = renderTheGrid();

		const item = await chooseOn("Rebuild the search index", "Move Up");

		expect(item).toHaveAttribute("aria-disabled", "true");
		expect(featureService.moveFeature).not.toHaveBeenCalled();
	});

	it("offers no way down to the row that is already at the bottom", async () => {
		const { featureService } = renderTheGrid();

		const item = await chooseOn("Publish the partner catalogue", "Move Down");

		expect(item).toHaveAttribute("aria-disabled", "true");
		expect(featureService.moveFeature).not.toHaveBeenCalled();
	});

	// A move renumbers the whole instance, so the surface re-reads rather than patching what it has.
	it("tells the surface to read the order again once a move lands", async () => {
		const { onOrderChanged } = renderTheGrid();

		await chooseOn("Rebuild the search index", "Move Down");

		await waitFor(() => expect(onOrderChanged).toHaveBeenCalled());
	});
});
