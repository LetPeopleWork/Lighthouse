import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../models/Feature";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "../../tests/MockApiServiceProvider";
import { renderWithQueryClient as render } from "../../tests/renderWithQueryClient";
import FeaturesView from "./FeaturesView";

vi.mock("../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const aFeature = (id: number, name: string, portfolios: string[]): Feature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.referenceId = `FTR-${id}`;
	feature.stateCategory = "ToDo";
	feature.state = "Active";
	feature.lastUpdated = new Date();
	feature.isUsingDefaultFeatureSize = false;
	feature.projects = portfolios.map((portfolio, index) => ({
		id: id * 10 + index,
		name: portfolio,
	}));
	feature.remainingWork = { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	feature.position = id;
	return feature;
};

const namesShown = () =>
	Array.from(
		document.querySelectorAll('.MuiDataGrid-cell[data-field="name"]'),
	).map((cell) => cell.textContent ?? "");

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

describe("FeaturesView search by Portfolio", () => {
	it("finds a Feature by the name of any Portfolio it belongs to", async () => {
		const featureService = createMockFeatureService();
		featureService.getAllFeatures = vi
			.fn()
			.mockResolvedValue([
				aFeature(1, "Rebuild the index", ["Platform", "Payments"]),
				aFeature(2, "Publish the catalogue", ["Partnerships"]),
			]);
		const user = userEvent.setup();
		render(
			<MemoryRouter>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({ featureService })}
				>
					<FeaturesView />
				</ApiServiceContext.Provider>
			</MemoryRouter>,
		);
		await screen.findByText(/Rebuild the index/);

		await user.type(screen.getByRole("textbox"), "payments");

		await waitFor(() => expect(namesShown()).toHaveLength(1));
		expect(namesShown()[0]).toContain("Rebuild the index");
	});
});
