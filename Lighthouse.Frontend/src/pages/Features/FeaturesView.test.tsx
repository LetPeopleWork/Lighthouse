import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../tests/MockApiServiceProvider";
import FeaturesView from "./FeaturesView";

// The instance renames the concept, so nothing on this page may hard-code the word "Feature".
vi.mock("../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.FEATURE]: "Deliverable",
				[TERMINOLOGY_KEYS.FEATURES]: "Deliverables",
				[TERMINOLOGY_KEYS.PORTFOLIOS]: "Portfolios",
			};
			return terms[key] ?? key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const rankedFeature = (
	id: number,
	name: string,
	position: number,
	stateCategory: Feature["stateCategory"],
): Feature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.referenceId = `FTR-${id}`;
	feature.stateCategory = stateCategory;
	feature.state = stateCategory === "Done" ? "Closed" : "Active";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = [];
	feature.remainingWork = stateCategory === "Done" ? {} : { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	feature.position = position;
	return feature;
};

const listedPlaces = (container: HTMLElement): string[] =>
	Array.from(
		container.querySelectorAll('.MuiDataGrid-cell[data-field="position"]'),
	).map((cell) => cell.textContent ?? "");

const renderFeaturesView = () => {
	const featureService = createMockFeatureService();
	featureService.getAllFeatures = vi
		.fn()
		.mockResolvedValue([
			rankedFeature(1, "Rebuild the search index", 1, "ToDo"),
			rankedFeature(2, "Ship the pricing page", 2, "Done"),
			rankedFeature(3, "Publish the partner catalogue", 3, "ToDo"),
		]);

	return render(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ featureService })}
			>
				<FeaturesView />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
};

describe("FeaturesView", () => {
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

	// AC-1.7 — a finished row keeps its place, so hiding it cannot renumber the rows that remain.
	it("hides finished rows by default and leaves every surviving place unchanged when they are shown again", async () => {
		const user = userEvent.setup();
		const { container } = renderFeaturesView();

		// The name cell prefixes the tracker's reference id, so match on the name itself.
		await screen.findByText(/Rebuild the search index/);
		expect(screen.queryByText(/Ship the pricing page/)).not.toBeInTheDocument();
		expect(listedPlaces(container)).toEqual(["1", "3"]);

		const toggle = screen
			.getByTestId("hide-completed-features-toggle")
			.querySelector('input[type="checkbox"]');
		await user.click(toggle as HTMLElement);

		expect(
			await screen.findByText(/Ship the pricing page/),
		).toBeInTheDocument();
		expect(listedPlaces(container)).toEqual(["1", "2", "3"]);
	});

	// AC-1.6 — the page explains the order in the instance's own word for the concept.
	it("explains the order using the instance's own word", async () => {
		renderFeaturesView();

		expect(
			await screen.findByText(
				"Lighthouse forecasts Deliverables in this order — the top of the list gets your teams' throughput first.",
			),
		).toBeInTheDocument();
	});
});
