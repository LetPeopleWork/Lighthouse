import { screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../models/EntityReference";
import { Feature } from "../../../models/Feature";
import type { IFeatureDependency } from "../../../models/FeatureDependency";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import {
	givenANarrowWindow,
	givenTextIsWiderThanItsColumn,
	resetTheCut,
	resetTheWindow,
	thenALongTextIsCutToItsColumn,
	thenHoveringOverItOpensNoTooltip,
	thenTheActionsColumnIsPinnedAndLast,
	thenTheColumnIsShownWhole,
	thenTheFullTextIsInATooltip,
	thenTheTableSaysColumnsAreOutOfSightOnItsRight,
	whenThePersonHoversOver,
	whenThePersonReachesWithTheKeyboard,
} from "../../../tests/SharedGridFitKit";
import {
	createForecastsColumn,
	createNameColumn,
	createParentColumn,
	createStateColumn,
} from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

vi.mock("@mui/x-data-grid", async () =>
	(await import("../../../tests/SharedGridFitKit")).dataGridWithItsBoxStated(),
);

let mockedPolicy: "SourceOrder" | "ManualOrder" = "SourceOrder";

vi.mock("../../../hooks/useFeatureOrdering", () => ({
	useFeatureOrdering: () => ({
		policy: mockedPolicy,
		positionColumnLabel: mockedPolicy === "ManualOrder" ? "Manual" : "#",
		// These tests are about the columns the grid injects, not about who may move a row - that verdict
		// is judged in useFeatureOrdering.moveGate.test.tsx and FeatureMoveMenu.test.tsx.
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
			if (key === "workItems") return "Work Items";
			return "Unknown";
		},
	}),
}));

const createFeature = (
	overrides: Partial<{
		id: number;
		name: string;
		referenceId: string;
		stateCategory: string;
		remainingWork: { [key: number]: number };
		isUsingDefaultFeatureSize: boolean;
		position: number;
		parentWorkItemReference: string;
	}>,
): Feature => {
	const feature = new Feature();
	feature.id = overrides.id ?? 1;
	feature.name = overrides.name ?? "Test Feature";
	feature.referenceId = overrides.referenceId ?? "FTR-1";
	feature.stateCategory =
		(overrides.stateCategory as Feature["stateCategory"]) ?? "ToDo";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize =
		overrides.isUsingDefaultFeatureSize ?? false;
	feature.projects = [];
	feature.remainingWork = overrides.remainingWork ?? { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	feature.state = overrides.stateCategory === "Done" ? "Closed" : "Active";
	feature.position = overrides.position;
	feature.parentWorkItemReference = overrides.parentWorkItemReference ?? "";
	return feature;
};

const defaultColumns = [createForecastsColumn(), createStateColumn()];

describe("FeatureListDataGrid export plumbing", () => {
	// This grid forwarded none of the export props, so the Delivery grid had no export button at
	// all. The forwarding is the whole precursor — assert it rather than the toolbar's behaviour.
	it("offers the two ways out when a grid asks for them", () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={[createFeature({ id: 1, name: "Checkout redesign" })]}
					columns={[createStateColumn()]}
					storageKey="export-on-grid"
					hideCompletedStorageKey="export-on-hide-completed"
					enableExport={true}
					exportFileName="Q3 Platform"
					exportTable={() => ({ headers: ["Name"], rows: [["Q3 Platform"]] })}
				/>
			</MemoryRouter>,
		);

		expect(screen.getByTestId("copy-button")).toBeInTheDocument();
		expect(screen.getByTestId("export-button")).toBeInTheDocument();
	});

	it("offers neither when a grid does not ask", () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={[createFeature({ id: 1, name: "Checkout redesign" })]}
					columns={[createStateColumn()]}
					storageKey="export-off-grid"
					hideCompletedStorageKey="export-off-hide-completed"
				/>
			</MemoryRouter>,
		);

		expect(screen.queryByTestId("copy-button")).not.toBeInTheDocument();
		expect(screen.queryByTestId("export-button")).not.toBeInTheDocument();
	});
});

describe("FeatureListDataGrid", () => {
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

	const features = [
		createFeature({
			id: 1,
			name: "Active Feature",
			referenceId: "FTR-1",
			stateCategory: "ToDo",
		}),
		createFeature({
			id: 2,
			name: "In Progress Feature",
			referenceId: "FTR-2",
			stateCategory: "Doing",
		}),
		createFeature({
			id: 3,
			name: "Done Feature",
			referenceId: "FTR-3",
			stateCategory: "Done",
			remainingWork: {},
		}),
	];

	it("should render the hide completed features toggle", () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={features}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		expect(
			screen.getByTestId("hide-completed-features-toggle"),
		).toBeInTheDocument();
		expect(screen.getByText("Hide Completed Features")).toBeInTheDocument();
	});

	it("should have toggle checked by default", () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={features}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		expect(switchInput).toBeChecked();
	});

	it("should hide completed features by default", async () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={features}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		// Active features should render via state column (2 non-done features)
		const activeElements = await screen.findAllByText("Active");
		expect(activeElements).toHaveLength(2);

		// Done feature state should not appear
		expect(screen.queryByText("Closed")).not.toBeInTheDocument();
	});

	it("should show completed features when toggle is unchecked", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={features}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		expect(await screen.findByText("Closed")).toBeInTheDocument();
	});

	it("should save toggle preference to localStorage when changed", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={features}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		expect(localStorage.getItem("test-hide-completed")).toBe("true");

		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		expect(localStorage.getItem("test-hide-completed")).toBe("false");
	});

	it("should load toggle preference from localStorage on mount", () => {
		localStorage.setItem("test-hide-completed", "false");

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={features}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		expect(switchInput).not.toBeChecked();
	});

	it("should show a Done feature with remaining work when hideCompleted is true", async () => {
		const featuresWithDoneHavingRemainingWork = [
			createFeature({ id: 1, stateCategory: "ToDo" }),
			createFeature({
				id: 2,
				stateCategory: "Done",
				remainingWork: { 1: 3 },
			}),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresWithDoneHavingRemainingWork}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		// Both the ToDo (Active) and the Done-with-remaining-work (Closed) should appear
		const activeElements = await screen.findAllByText("Active");
		expect(activeElements).toHaveLength(1);
		expect(await screen.findByText("Closed")).toBeInTheDocument();
	});

	it("should hide a Done feature with no remaining work when hideCompleted is true", async () => {
		const featuresWithDoneNoRemaining = [
			createFeature({ id: 1, stateCategory: "ToDo" }),
			createFeature({
				id: 2,
				stateCategory: "Done",
				remainingWork: {},
			}),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresWithDoneNoRemaining}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		const activeElements = await screen.findAllByText("Active");
		expect(activeElements).toHaveLength(1);
		expect(screen.queryByText("Closed")).not.toBeInTheDocument();
	});

	it("should show warning icon for Done feature with remaining work", async () => {
		const featuresWithWarning = [
			createFeature({
				id: 1,
				stateCategory: "Done",
				remainingWork: { 1: 5 },
			}),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresWithWarning}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		const warningIcon = await screen.findByTestId("warnings");
		expect(warningIcon.getAttribute("aria-label")).toContain(
			"marked as done but still has remaining",
		);
	});

	it("should show warning icon for feature using default feature size", async () => {
		const featuresWithDefaultSize = [
			createFeature({
				id: 1,
				stateCategory: "ToDo",
				isUsingDefaultFeatureSize: true,
			}),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresWithDefaultSize}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		const warningIcon = await screen.findByTestId("warnings");
		expect(warningIcon.getAttribute("aria-label")).toContain(
			"based on the default",
		);
	});

	it("should not show warning icon for Done feature with no remaining work", async () => {
		const featuresNoWarning = [
			createFeature({ id: 1, stateCategory: "ToDo" }),
			createFeature({
				id: 2,
				stateCategory: "Done",
				remainingWork: {},
			}),
		];

		localStorage.setItem("test-hide-completed", "false");

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresNoWarning}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		await screen.findByText("Closed");
		expect(screen.queryByTestId("warnings")).not.toBeInTheDocument();
	});

	it("should not show warning icon for non-Done feature with remaining work", async () => {
		const nonDoneFeatures = [
			createFeature({ id: 1, stateCategory: "Doing", remainingWork: { 1: 3 } }),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={nonDoneFeatures}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		await screen.findAllByText("Active");
		expect(screen.queryByTestId("warnings")).not.toBeInTheDocument();
	});

	it("should show active work indicator when getActiveWorkTeams returns teams", async () => {
		const activeTeam: IEntityReference = { id: 1, name: "Team Alpha" };
		const featuresWithActiveWork = [
			createFeature({ id: 1, stateCategory: "Doing" }),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresWithActiveWork}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
					getActiveWorkTeams={() => [activeTeam]}
				/>
			</MemoryRouter>,
		);

		expect(
			await screen.findByTestId("active-work-indicator"),
		).toBeInTheDocument();
	});

	it("should show no-active-work indicator when getActiveWorkTeams returns empty", async () => {
		const featuresNoActiveWork = [
			createFeature({ id: 1, stateCategory: "Doing" }),
		];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={featuresNoActiveWork}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
					getActiveWorkTeams={() => []}
				/>
			</MemoryRouter>,
		);

		expect(await screen.findByTestId("no-active-work")).toBeInTheDocument();
	});

	it("should not show active work indicator when getActiveWorkTeams prop is not provided", async () => {
		const someFeatures = [createFeature({ id: 1, stateCategory: "Doing" })];

		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={someFeatures}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		await screen.findAllByText("Active");
		expect(
			screen.queryByTestId("active-work-indicator"),
		).not.toBeInTheDocument();
	});

	// AC-1.5 names only the Features view and the Portfolio Feature list; a whole-instance ordinal
	// inside a team- or delivery-scoped subset would misread, so the column is opt-in.
	const rankedFeatures = [
		createFeature({ id: 1, stateCategory: "ToDo", position: 17 }),
	];

	it("should not show the position column when the surface does not ask for it", async () => {
		const { container } = render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={rankedFeatures}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
				/>
			</MemoryRouter>,
		);

		await screen.findAllByText("Active");
		expect(container.querySelector('[data-field="position"]')).toBeNull();
		expect(screen.queryByText("17")).not.toBeInTheDocument();
	});

	it("should show the position column when the surface asks for it", async () => {
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={rankedFeatures}
					columns={defaultColumns}
					storageKey="test-grid"
					hideCompletedStorageKey="test-hide-completed"
					showPosition
				/>
			</MemoryRouter>,
		);

		expect(await screen.findByText("17")).toBeInTheDocument();
	});

	// Epic 5375 AC-5.4 — the column names whoever owns the order, and the grid must render the heading
	// the ordering seam gives it rather than one of its own.
	it.each([
		["SourceOrder", "#"],
		["ManualOrder", "Manual"],
	] as const)(
		"should head the position column with %s's label",
		async (policy, expectedHeading) => {
			mockedPolicy = policy;

			render(
				<MemoryRouter>
					<FeatureListDataGrid
						features={rankedFeatures}
						columns={defaultColumns}
						storageKey="test-grid"
						hideCompletedStorageKey="test-hide-completed"
						showPosition
					/>
				</MemoryRouter>,
			);

			expect(
				await screen.findByRole("columnheader", { name: expectedHeading }),
			).toBeInTheDocument();
		},
	);
});

describe("the Feature list table fits its columns, pins its move menu and says when some are out of sight", () => {
	const LONG_NAME =
		"Replatform the order management stack onto the shared event backbone for every region";
	const SHOWN_NAME = `FTR-1: ${LONG_NAME}`;
	const LONG_PARENT =
		"Order management modernisation programme for the northern and southern regions";
	const SHOWN_PARENT = `PRG-7: ${LONG_PARENT}`;
	const parents = new Map([
		["PRG-7", { referenceId: "PRG-7", name: LONG_PARENT, url: "" }],
	]);

	beforeEach(() => {
		givenANarrowWindow();
	});

	afterEach(() => {
		resetTheCut();
		resetTheWindow();
		localStorage.clear();
	});

	const FIRST_DEPENDENCY = "DEP-1: Card payments gateway cutover";
	const SECOND_DEPENDENCY = "DEP-2: Ledger export to the data warehouse";

	const aDependency = (
		referenceId: string,
		name: string,
	): IFeatureDependency => ({
		referenceId,
		name,
		url: null,
		source: "TrackerLink",
		notHonouredReason: null,
		blockerPositionedBelow: false,
		isWithheld: false,
	});

	const aFeatureWaitingOnTwoOthers = (): Feature => {
		const feature = createFeature({
			id: 1,
			name: LONG_NAME,
			referenceId: "FTR-1",
			stateCategory: "Doing",
			position: 3,
			parentWorkItemReference: "PRG-7",
		});
		feature.dependsOn = [
			aDependency("DEP-1", "Card payments gateway cutover"),
			aDependency("DEP-2", "Ledger export to the data warehouse"),
		];
		return feature;
	};

	const thenEachDependencyLineIsCut = (line: string) => {
		const cell = screen
			.getByText(line)
			.closest<HTMLElement>('[role="gridcell"]');
		const lineAndItsWrappers: HTMLElement[] = [];
		for (
			let element: HTMLElement | null = screen.getByText(line);
			element !== null && element !== cell;
			element = element.parentElement
		) {
			lineAndItsWrappers.push(element);
		}
		expect(
			lineAndItsWrappers.some(
				(element) => getComputedStyle(element).textOverflow === "ellipsis",
			),
			`the dependency "${line}" must end with an ellipsis on its own line when it is wider than the column, as every other dependency does`,
		).toBe(true);
	};

	const whenTheFeatureListShows = () =>
		render(
			<MemoryRouter>
				<FeatureListDataGrid
					features={[aFeatureWaitingOnTwoOthers()]}
					columns={[
						createNameColumn("Feature"),
						createParentColumn(parents),
						createStateColumn(),
					]}
					storageKey="fit-feature-list"
					hideCompletedStorageKey="fit-feature-list-hide-completed"
					showPosition
				/>
			</MemoryRouter>,
		);

	it("cuts a long Feature name and a long parent name to their columns and shows the position and the state whole", () => {
		givenTextIsWiderThanItsColumn();
		const { container } = whenTheFeatureListShows();

		thenALongTextIsCutToItsColumn(container, "name");
		thenALongTextIsCutToItsColumn(container, "parent");
		thenTheColumnIsShownWhole(container, "position");
		thenTheColumnIsShownWhole(container, "state");
	});

	it("pins the move menu as the last column and says columns are out of sight", () => {
		const { container } = whenTheFeatureListShows();

		thenTheActionsColumnIsPinnedAndLast(container, "featureOrderingActions");
		thenTheTableSaysColumnsAreOutOfSightOnItsRight(container);
	});

	it("shows the full Feature name in a tooltip when the person hovers over a cut name", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		whenThePersonHoversOver(screen.getByText(SHOWN_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});

	it("shows the full parent name in a tooltip when the person hovers over a cut parent", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		whenThePersonHoversOver(screen.getByText(SHOWN_PARENT));

		await thenTheFullTextIsInATooltip(SHOWN_PARENT);
	});

	it("shows the full parent name in a tooltip when the person reaches a cut parent with the keyboard", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		whenThePersonReachesWithTheKeyboard(screen.getByText(SHOWN_PARENT));

		await thenTheFullTextIsInATooltip(SHOWN_PARENT);
	});

	it("opens no tooltip over a Feature name that fits its column", async () => {
		whenTheFeatureListShows();

		await thenHoveringOverItOpensNoTooltip(screen.getByText(SHOWN_NAME));
	});

	it("opens no tooltip over a state, which is never cut", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		await thenHoveringOverItOpensNoTooltip(screen.getByText("Active"));
	});

	it("shows the full Feature name in a tooltip when the person reaches a cut name with the keyboard", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		whenThePersonReachesWithTheKeyboard(screen.getByText(SHOWN_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});

	it("cuts a long dependency list to its column", () => {
		givenTextIsWiderThanItsColumn();
		const { container } = whenTheFeatureListShows();

		thenALongTextIsCutToItsColumn(container, "dependsOn");
		thenEachDependencyLineIsCut(FIRST_DEPENDENCY);
		thenEachDependencyLineIsCut(SECOND_DEPENDENCY);
	});

	it("names every dependency in a tooltip when the person hovers over a cut dependency list", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		whenThePersonHoversOver(screen.getByText(FIRST_DEPENDENCY));

		await thenTheFullTextIsInATooltip(FIRST_DEPENDENCY);
		await thenTheFullTextIsInATooltip(SECOND_DEPENDENCY);
	});

	it("names every dependency in a tooltip when the person reaches a cut dependency list with the keyboard", async () => {
		givenTextIsWiderThanItsColumn();
		whenTheFeatureListShows();

		whenThePersonReachesWithTheKeyboard(screen.getByText(FIRST_DEPENDENCY));

		await thenTheFullTextIsInATooltip(FIRST_DEPENDENCY);
		await thenTheFullTextIsInATooltip(SECOND_DEPENDENCY);
	});

	it("opens no tooltip over a dependency list that fits its column", async () => {
		whenTheFeatureListShows();

		await thenHoveringOverItOpensNoTooltip(screen.getByText(FIRST_DEPENDENCY));
	});
});
