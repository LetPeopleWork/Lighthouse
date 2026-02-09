import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { createForecastsColumn, createStateColumn } from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "feature") return "Feature";
			if (key === "features") return "Features";
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
	}>,
): Feature => {
	const feature = new Feature();
	feature.id = overrides.id ?? 1;
	feature.name = overrides.name ?? "Test Feature";
	feature.referenceId = overrides.referenceId ?? "FTR-1";
	feature.stateCategory =
		(overrides.stateCategory as Feature["stateCategory"]) ?? "ToDo";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = [];
	feature.remainingWork = { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	feature.state = overrides.stateCategory === "Done" ? "Closed" : "Active";
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
});
