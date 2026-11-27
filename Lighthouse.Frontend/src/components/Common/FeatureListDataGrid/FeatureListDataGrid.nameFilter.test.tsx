import { screen, waitFor } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createNameColumn } from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

/**
 * The Name column's own filter finds a Feature by the reference ID its cell shows, and sorting
 * that column still orders by the Feature name. These run the real grid and its real filter panel.
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

const aFeature = (id: number, referenceId: string, name: string): Feature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.referenceId = referenceId;
	feature.stateCategory = "ToDo";
	feature.state = "Active";
	feature.lastUpdated = new Date();
	feature.projects = [];
	feature.remainingWork = { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	return feature;
};

// The names run opposite to the IDs, so a sort by the shown text differs from a sort by name.
const ZEBRA = "PROJ-12: Zebra";
const APPLE = "PROJ-20: Apple";
const MANGO = "PROJ-30: Mango";

const theFeatures = [
	aFeature(1, "PROJ-12", "Zebra"),
	aFeature(2, "PROJ-20", "Apple"),
	aFeature(3, "PROJ-30", "Mango"),
];

const givenTheFeatureList = (storageKey: string) => {
	render(
		<MemoryRouter>
			<FeatureListDataGrid
				features={theFeatures}
				columns={[createNameColumn("Feature")]}
				storageKey={storageKey}
				hideCompletedStorageKey={`${storageKey}-hide-completed`}
			/>
		</MemoryRouter>,
	);
	return userEvent.setup();
};

const featuresShown = () =>
	[...document.querySelectorAll('[role="gridcell"][data-field="name"]')].map(
		(cell) => cell.textContent ?? "",
	);

const whenTheyFilterTheNameColumnBy = async (user: UserEvent, text: string) => {
	await user.click(screen.getByLabelText("Feature Name column menu"));
	await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
	await user.type(await screen.findByRole("textbox", { name: "Value" }), text);
};

const whenTheySortTheNameColumnAscending = async (user: UserEvent) => {
	await user.click(screen.getByRole("columnheader", { name: /Feature Name/i }));
};

const thenOnlyTheseAreListed = async (names: string[]) => {
	await waitFor(() =>
		expect(
			featuresShown(),
			`Expected the list to show ${names.join(", ")}. The Name filter must match the ID: name text the cell shows. Make the Name column's value that shown text.`,
		).toEqual(names),
	);
};

const thenTheyAreListedInThisOrder = async (names: string[]) => {
	await waitFor(() =>
		expect(
			featuresShown(),
			`Expected sort by name: ${names.join(", ")}. Sorting must order by Feature name, not by the ID prefix. Give the Name column a name-order sort.`,
		).toEqual(names),
	);
};

describe("FeatureListDataGrid name filter should", () => {
	beforeEach(() => {
		localStorage.clear();
	});

	it("list a Feature when its reference ID is typed into the Name column filter", async () => {
		const user = givenTheFeatureList("name-filter-by-id");

		await whenTheyFilterTheNameColumnBy(user, "PROJ-12");

		await thenOnlyTheseAreListed([ZEBRA]);
	});

	it("keep sorting the Name column by Feature name rather than by reference ID", async () => {
		const user = givenTheFeatureList("name-sort-by-name");

		await whenTheySortTheNameColumnAscending(user);

		await thenTheyAreListedInThisOrder([APPLE, MANGO, ZEBRA]);
	});
});
