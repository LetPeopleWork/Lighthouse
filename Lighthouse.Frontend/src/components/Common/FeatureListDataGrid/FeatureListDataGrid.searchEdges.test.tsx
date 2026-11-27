import { screen, waitFor } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { PAGE_SIZE } from "../DataGrid/RowRangeFooter";
import { createNameColumn, createStateColumn } from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

/**
 * A search that finds nothing says so, in words that differ from the message of a list that holds no
 * Features at all; it narrows together with a column filter; and a new search opens the first page
 * of its results. These run the real grid through the rendered list.
 */

vi.mock("@mui/x-data-grid", async () =>
	(await import("../../../tests/SharedGridFitKit")).dataGridWithItsBoxStated(),
);

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

const EMPTY_LIST_MESSAGE = "No Features have been added to this list yet.";

const aFeature = (position: number, name: string, state: string): Feature => {
	const feature = new Feature();
	feature.id = position;
	feature.name = name;
	feature.referenceId = `PROJ-${position}`;
	feature.stateCategory = "ToDo";
	feature.state = state;
	feature.lastUpdated = new Date();
	feature.projects = [];
	feature.remainingWork = { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	return feature;
};

const givenTheFeatureList = (features: Feature[]) => {
	render(
		<MemoryRouter>
			<FeatureListDataGrid
				features={features}
				columns={[createNameColumn("Feature"), createStateColumn()]}
				storageKey="searched-edges-list"
				hideCompletedStorageKey="searched-edges-list-hide-completed"
				emptyStateMessage={EMPTY_LIST_MESSAGE}
			/>
		</MemoryRouter>,
	);
	return userEvent.setup();
};

const theBox = () => screen.getByRole("textbox", { name: "Search Features" });

const featuresShown = () =>
	[...document.querySelectorAll('[role="gridcell"][data-field="name"]')].map(
		(cell) => cell.textContent ?? "",
	);

const whenTheyType = async (user: UserEvent, text: string) => {
	await user.type(theBox(), text);
};

const whenTheyFilterTheStateColumnBy = async (
	user: UserEvent,
	value: string,
) => {
	await user.click(screen.getByLabelText("State column menu"));
	await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
	await user.type(await screen.findByRole("textbox", { name: "Value" }), value);
};

const whenTheyGoToTheNextPage = async (user: UserEvent) => {
	await user.click(
		screen.getByLabelText("Go to next page", { selector: "button" }),
	);
	await screen.findByText(
		`${PAGE_SIZE + 1}–${PAGE_SIZE + 1} of ${PAGE_SIZE + 1}`,
	);
};

const thenOnlyTheseAreListed = async (names: string[]) => {
	await waitFor(() =>
		expect(
			featuresShown(),
			`the list must show exactly ${JSON.stringify(names)}; a Feature must be listed only when it meets both the column filter and the search`,
		).toEqual(names),
	);
};

const thenTheListSaysNothingMatches = async (text: string) => {
	expect(
		await screen.findByText(`No Features match "${text}".`),
		"a search that finds nothing must say that no Feature matches it, naming the text typed",
	).toBeInTheDocument();
	expect(
		screen.queryByText(EMPTY_LIST_MESSAGE),
		"a search that finds nothing must not claim the list holds no Features at all",
	).not.toBeInTheDocument();
};

beforeEach(() => {
	localStorage.clear();
});

afterEach(() => {
	localStorage.clear();
});

describe("FeatureListDataGrid search in its edge states", () => {
	it("says that no Feature matches the search, not that the list is empty", async () => {
		const user = givenTheFeatureList([
			aFeature(1, "Payments", "Doing"),
			aFeature(2, "Checkout", "Review"),
		]);

		await whenTheyType(user, "zzz");

		await thenTheListSaysNothingMatches("zzz");
		expect(
			featuresShown(),
			"no Feature may be listed for text that matches none",
		).toEqual([]);
	});

	it("keeps the empty-list message for a list that holds no Features", () => {
		givenTheFeatureList([]);

		expect(
			screen.getByText(EMPTY_LIST_MESSAGE),
			"a list with no Features must say so in its own words",
		).toBeInTheDocument();
		expect(
			screen.queryByText(/match/),
			"a list nobody searched must not say that a search matched nothing",
		).not.toBeInTheDocument();
	});

	it("lists only the Features that meet both the column filter and the search", async () => {
		const user = givenTheFeatureList([
			aFeature(1, "Payments", "Doing"),
			aFeature(2, "Payments API", "Review"),
			aFeature(3, "Checkout", "Doing"),
			aFeature(4, "Reports", "Review"),
		]);
		await whenTheyType(user, "pay");
		await thenOnlyTheseAreListed(["PROJ-1: Payments", "PROJ-2: Payments API"]);

		await whenTheyFilterTheStateColumnBy(user, "Doing");

		await thenOnlyTheseAreListed(["PROJ-1: Payments"]);
	});

	it("opens the first page of the results when a search is typed on the second page", async () => {
		const features = Array.from({ length: PAGE_SIZE + 1 }, (_, index) =>
			aFeature(
				index + 1,
				`Feature ${String(index + 1).padStart(3, "0")}`,
				"Doing",
			),
		);
		const user = givenTheFeatureList(features);
		await whenTheyGoToTheNextPage(user);

		await whenTheyType(user, "proj");

		expect(
			await screen.findByText(`1–${PAGE_SIZE} of ${PAGE_SIZE + 1}`),
			"a search that still leaves more than one page must open at its first page",
		).toBeInTheDocument();
		expect(featuresShown()[0]).toBe("PROJ-1: Feature 001");
	});
});
