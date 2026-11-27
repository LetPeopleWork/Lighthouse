import { screen, waitFor } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import { Feature } from "../../../models/Feature";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createNameColumn, createParentColumn } from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

/**
 * A person finds a Feature by typing part of what the list shows into one search box above it:
 * its reference ID, its name or its parent. These run the real grid through the rendered list.
 */

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock("@mui/x-data-grid", async () =>
	(await import("../../../tests/SharedGridFitKit")).dataGridWithItsBoxStated(),
);

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => reportUsage,
}));

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

const aFeature = (
	id: number,
	referenceId: string,
	name: string,
	parentWorkItemReference: string,
): Feature => {
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
	feature.parentWorkItemReference = parentWorkItemReference;
	return feature;
};

const parents = new Map<string, ParentWorkItem>([
	["PROJ-3", { referenceId: "PROJ-3", name: "Checkout", url: "" }],
	["PROJ-4", { referenceId: "PROJ-4", name: "Shop", url: "" }],
]);

const CHECKOUT = "PROJ-12: Checkout";
const PAYMENTS = "PROJ-20: Payments";
const REPORTS = "PROJ-30: Reports";

const theFeatures = [
	aFeature(1, "PROJ-12", "Checkout", "PROJ-4"),
	aFeature(2, "PROJ-20", "Payments", "PROJ-3"),
	aFeature(3, "PROJ-30", "Reports", "PROJ-4"),
];

const givenTheFeatureList = () => {
	render(
		<MemoryRouter>
			<FeatureListDataGrid
				features={theFeatures}
				columns={[createNameColumn("Feature"), createParentColumn(parents)]}
				storageKey="searched-feature-list"
				hideCompletedStorageKey="searched-feature-list-hide-completed"
				enableExport={true}
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

const whenTheyClearTheBox = async (user: UserEvent) => {
	await user.click(screen.getByRole("button", { name: /clear/i }));
};

const thenOnlyTheseAreListed = async (names: string[]) => {
	await waitFor(() =>
		expect(
			featuresShown(),
			`the list must show exactly ${JSON.stringify(names)} for what was typed; a Feature found by its ID, name or parent text must be listed and every other Feature left out`,
		).toEqual(names),
	);
};

beforeEach(() => {
	localStorage.clear();
	reportUsage.mockReset();
	reportUsage.mockReturnValue(true);
});

afterEach(() => {
	localStorage.clear();
});

describe("FeatureListDataGrid search", () => {
	it("finds a Feature by its reference ID typed in lower case", async () => {
		const user = givenTheFeatureList();

		await whenTheyType(user, "proj-12");

		await thenOnlyTheseAreListed([CHECKOUT]);
	});

	it("finds a Feature through the name of its parent as well as through its own name", async () => {
		const user = givenTheFeatureList();

		await whenTheyType(user, "checkout");

		await thenOnlyTheseAreListed([CHECKOUT, PAYMENTS]);
	});

	it("finds a Feature through the reference ID of its parent", async () => {
		const user = givenTheFeatureList();

		await whenTheyType(user, "proj-3:");

		await thenOnlyTheseAreListed([PAYMENTS]);
	});

	it("lists every Feature again when the box is cleared", async () => {
		const user = givenTheFeatureList();
		await whenTheyType(user, "proj-12");
		await thenOnlyTheseAreListed([CHECKOUT]);

		await whenTheyClearTheBox(user);

		await thenOnlyTheseAreListed([CHECKOUT, PAYMENTS, REPORTS]);
	});

	it("offers an empty box named after the Features in the toolbar before its icons", () => {
		givenTheFeatureList();

		expect(theBox()).toHaveValue("");
		const icons = screen.getAllByTestId(/-button$/);
		expect(
			icons.length,
			"the toolbar row must hold its icons for the order to mean anything",
		).toBeGreaterThan(1);
		for (const icon of icons) {
			expect(
				theBox().compareDocumentPosition(icon) &
					Node.DOCUMENT_POSITION_FOLLOWING,
				`the search box belongs at the left of the toolbar row, before every icon, but it comes after ${icon.dataset.testid}`,
			).toBeTruthy();
		}
	});

	it("reports one use of the search per visit, however much is typed, cleared and typed again", async () => {
		const user = givenTheFeatureList();

		await whenTheyType(user, "proj");
		await whenTheyClearTheBox(user);
		await whenTheyType(user, "checkout");

		expect(
			reportUsage.mock.calls,
			"the search must report that it was used once, as a bare event with nothing about the text, the list or the matches",
		).toEqual([[{ name: "FeatureListSearched" }]]);
	});

	it("still reports the use once the answer to record it arrives after an unrecorded first try", async () => {
		reportUsage.mockReturnValueOnce(false).mockReturnValue(true);
		const user = givenTheFeatureList();

		await whenTheyType(user, "proj-12");

		const recorded = reportUsage.mock.results.filter(
			(result) => result.value === true,
		);
		expect(
			recorded,
			"a use that was not recorded must not use up the one report of the visit, so the next keystroke reports it, once",
		).toHaveLength(1);
	});
});
