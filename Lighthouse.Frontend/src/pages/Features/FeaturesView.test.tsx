import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IEntityReference } from "../../models/EntityReference";
import { Feature } from "../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../tests/MockApiServiceProvider";
import FeaturesView from "./FeaturesView";

// The instance renames the concept, so nothing on this page may hard-code the word "Feature".
// The terms are mutable so a test can rename them mid-session, the way the settings screen does.
const terminology = vi.hoisted(() => ({ terms: {} as Record<string, string> }));

vi.mock("../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terminology.terms[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.FEATURE]: "Deliverable",
	[TERMINOLOGY_KEYS.FEATURES]: "Deliverables",
	[TERMINOLOGY_KEYS.PORTFOLIOS]: "Portfolios",
};

const rankedFeature = (
	id: number,
	name: string,
	position: number,
	stateCategory: Feature["stateCategory"],
	projects: IEntityReference[] = [],
): Feature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.referenceId = `FTR-${id}`;
	feature.stateCategory = stateCategory;
	feature.state = stateCategory === "Done" ? "Closed" : "Active";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = projects;
	feature.remainingWork = stateCategory === "Done" ? {} : { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	feature.position = position;
	return feature;
};

type MockFeatureService = ReturnType<typeof createMockFeatureService>;

const serviceReturning = (features: Feature[]): MockFeatureService => {
	const featureService = createMockFeatureService();
	featureService.getAllFeatures = vi.fn().mockResolvedValue(features);
	return featureService;
};

const rankedFeatures = () => [
	rankedFeature(1, "Rebuild the search index", 1, "ToDo"),
	rankedFeature(2, "Ship the pricing page", 2, "Done"),
	rankedFeature(3, "Publish the partner catalogue", 3, "ToDo"),
];

const featuresPage = (featureService: MockFeatureService) => (
	<MemoryRouter>
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ featureService })}
		>
			<FeaturesView />
		</ApiServiceContext.Provider>
	</MemoryRouter>
);

const renderPage = (featureService: MockFeatureService) => {
	const view = render(featuresPage(featureService));
	return {
		...view,
		showPageAgain: (next: MockFeatureService = featureService) =>
			view.rerender(featuresPage(next)),
	};
};

const renderFeaturesView = () => renderPage(serviceReturning(rankedFeatures()));

const listedPlaces = (container: HTMLElement): string[] =>
	Array.from(
		container.querySelectorAll('.MuiDataGrid-cell[data-field="position"]'),
	).map((cell) => cell.textContent ?? "");

const cellsOf = (container: HTMLElement, field: string): string[] =>
	Array.from(
		container.querySelectorAll(`.MuiDataGrid-cell[data-field="${field}"]`),
	).map((cell) => cell.textContent ?? "");

const columnHeader = (container: HTMLElement, field: string): HTMLElement => {
	const header = container.querySelector<HTMLElement>(
		`.MuiDataGrid-columnHeader[data-field="${field}"]`,
	);
	if (!header) {
		throw new Error(`The "${field}" column is not on the page`);
	}
	return header;
};

const isStillLoading = (container: HTMLElement): boolean =>
	container.querySelector(".MuiDataGrid-skeletonLoadingOverlay") !== null ||
	screen.queryByRole("progressbar") !== null;

describe("FeaturesView", () => {
	beforeEach(() => {
		terminology.terms = { ...defaultTerms };
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

	it("shows the list as loading until the ranked rows arrive, never an empty list first", async () => {
		let publishFeatures: (features: Feature[]) => void = () => {};
		const featureService = createMockFeatureService();
		featureService.getAllFeatures = vi.fn().mockReturnValue(
			new Promise<Feature[]>((resolve) => {
				publishFeatures = resolve;
			}),
		);

		const { container } = renderPage(featureService);

		expect(isStillLoading(container)).toBe(true);
		expect(screen.queryByText("No Deliverables found")).not.toBeInTheDocument();

		publishFeatures(rankedFeatures());

		await screen.findByText(/Rebuild the search index/);
		expect(isStillLoading(container)).toBe(false);
	});

	it("reports an empty list in the instance's own word once the fetch comes back with nothing", async () => {
		const { container } = renderPage(serviceReturning([]));

		expect(
			await screen.findByText("No Deliverables found"),
		).toBeInTheDocument();
		expect(isStillLoading(container)).toBe(false);
	});

	// The Portfolios a feature belongs to are listed on the row, comma-separated.
	it("lists every Portfolio a row belongs to, separated by a comma and a space", async () => {
		const { container } = renderPage(
			serviceReturning([
				rankedFeature(1, "Rebuild the search index", 1, "ToDo", [
					{ id: 10, name: "Platform" },
					{ id: 11, name: "Payments" },
				]),
				rankedFeature(2, "Publish the partner catalogue", 2, "ToDo", [
					{ id: 12, name: "Partnerships" },
				]),
			]),
		);

		await screen.findByText(/Rebuild the search index/);

		expect(cellsOf(container, "projects")).toEqual([
			"Platform, Payments",
			"Partnerships",
		]);
	});

	it("titles the Portfolio column with the instance's own word and offers no sort control on it", async () => {
		const { container } = renderPage(
			serviceReturning([
				rankedFeature(1, "Rebuild the search index", 1, "ToDo", [
					{ id: 10, name: "Platform" },
				]),
			]),
		);

		await screen.findByText(/Rebuild the search index/);

		const portfolios = columnHeader(container, "projects");
		expect(within(portfolios).getByText("Portfolios")).toBeInTheDocument();
		expect(within(portfolios).queryAllByLabelText("Sort")).toEqual([]);
		// The manual rank is the order that matters, but sortable columns do exist on this grid.
		expect(
			within(columnHeader(container, "state")).queryAllByLabelText("Sort"),
		).not.toEqual([]);
	});

	it("re-reads the list when the page is served by a different feature service", async () => {
		const first = serviceReturning([
			rankedFeature(1, "Rebuild the search index", 1, "ToDo"),
		]);
		const { showPageAgain } = renderPage(first);

		await screen.findByText(/Rebuild the search index/);
		expect(first.getAllFeatures).toHaveBeenCalledTimes(1);

		showPageAgain(
			serviceReturning([
				rankedFeature(2, "Publish the partner catalogue", 1, "ToDo"),
			]),
		);

		expect(
			await screen.findByText(/Publish the partner catalogue/),
		).toBeInTheDocument();
		expect(
			screen.queryByText(/Rebuild the search index/),
		).not.toBeInTheDocument();
	});

	it("retitles the columns when the instance renames the concepts", async () => {
		const { container, showPageAgain } = renderPage(
			serviceReturning([
				rankedFeature(1, "Rebuild the search index", 1, "ToDo", [
					{ id: 10, name: "Platform" },
				]),
			]),
		);

		await screen.findByText(/Rebuild the search index/);
		expect(
			within(columnHeader(container, "projects")).getByText("Portfolios"),
		).toBeInTheDocument();
		expect(
			within(columnHeader(container, "name")).getByText("Deliverable Name"),
		).toBeInTheDocument();

		terminology.terms = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.FEATURE]: "Epic",
			[TERMINOLOGY_KEYS.PORTFOLIOS]: "Programmes",
		};
		showPageAgain();

		expect(
			within(columnHeader(container, "projects")).getByText("Programmes"),
		).toBeInTheDocument();
		expect(
			within(columnHeader(container, "name")).getByText("Epic Name"),
		).toBeInTheDocument();
	});

	it("heads the page with the instance's own word, spanning the full window width", async () => {
		const { container } = renderFeaturesView();

		await screen.findByText(/Rebuild the search index/);

		const page = container.querySelector<HTMLElement>(".MuiContainer-root");
		expect(page?.className).not.toMatch(/MuiContainer-maxWidth/);

		const heading = screen.getByRole("heading", {
			level: 4,
			name: "Deliverables",
		});
		expect(heading).toHaveStyle({ marginBottom: "8px" });

		const explanation = screen.getByText(/^Lighthouse forecasts Deliverables/);
		expect(explanation.tagName).toBe("P");
		expect(explanation).toHaveStyle({ marginBottom: "16px" });
	});
});
