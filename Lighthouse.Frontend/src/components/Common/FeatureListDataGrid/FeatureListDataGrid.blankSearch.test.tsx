import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createNameColumn, createStateColumn } from "./columns";
import FeatureListDataGrid from "./FeatureListDataGrid";

/** Typing only spaces is not a search: it is not reported as a use and it never claims that nothing matches. */

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
		getTerm: (key: string) => (key === "features" ? "Features" : "Feature"),
	}),
}));

const aFeature = (id: number, name: string): Feature => {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.referenceId = `PROJ-${id}`;
	feature.stateCategory = "ToDo";
	feature.state = "Doing";
	feature.lastUpdated = new Date();
	feature.projects = [];
	feature.remainingWork = { 1: 5 };
	feature.totalWork = { 1: 10 };
	feature.forecasts = [];
	feature.url = "";
	return feature;
};

const showTheList = () => {
	render(
		<MemoryRouter>
			<FeatureListDataGrid
				features={[aFeature(1, "Payments")]}
				columns={[createNameColumn("Feature"), createStateColumn()]}
				storageKey="blank-search-list"
				hideCompletedStorageKey="blank-search-list-hide-completed"
				emptyStateMessage="No Features have been added to this list yet."
			/>
		</MemoryRouter>,
	);
	return userEvent.setup();
};

const theBox = () => screen.getByRole("textbox", { name: "Search Features" });

beforeEach(() => {
	localStorage.clear();
	reportUsage.mockReset();
	reportUsage.mockReturnValue(true);
});

describe("FeatureListDataGrid with a blank search", () => {
	it("does not report a use for spaces typed or cleared", async () => {
		const user = showTheList();

		await user.type(theBox(), "   ");
		await user.click(screen.getByRole("button", { name: "Clear search" }));

		expect(reportUsage).not.toHaveBeenCalled();
	});

	it("does not say that nothing matches spaces when a column filter leaves no Feature", async () => {
		const user = showTheList();
		await user.click(screen.getByLabelText("State column menu"));
		await user.click(await screen.findByRole("menuitem", { name: "Filter" }));
		await user.type(
			await screen.findByRole("textbox", { name: "Value" }),
			"nothing-has-this-state",
		);

		await waitFor(() =>
			expect(
				document.querySelectorAll('[data-field="name"][role="gridcell"]'),
			).toHaveLength(0),
		);

		await user.type(theBox(), "   ");

		expect(screen.queryByText(/^No Features match/)).not.toBeInTheDocument();
	});
});
