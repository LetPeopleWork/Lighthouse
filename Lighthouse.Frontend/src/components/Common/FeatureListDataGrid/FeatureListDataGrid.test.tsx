import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../../models/EntityReference";
import { Feature } from "../../../models/Feature";
import { createForecastsColumn, createStateColumn } from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

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
	return feature;
};

const defaultColumns = [createForecastsColumn(), createStateColumn()];

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

		const warningIcon = await screen.findByTestId(
			"warning-done-with-remaining-work",
		);
		expect(warningIcon).toBeInTheDocument();
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

		const warningIcon = await screen.findByTestId(
			"warning-default-feature-size",
		);
		expect(warningIcon).toBeInTheDocument();
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
		expect(
			screen.queryByTestId("warning-done-with-remaining-work"),
		).not.toBeInTheDocument();
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
		expect(
			screen.queryByTestId("warning-done-with-remaining-work"),
		).not.toBeInTheDocument();
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
