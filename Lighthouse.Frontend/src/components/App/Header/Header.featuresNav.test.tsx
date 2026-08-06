import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { ITerminology } from "../../../models/Terminology";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import Header from "./Header";

vi.mock("../../../hooks/useUpdateAll", () => ({
	useUpdateAll: () => ({
		handleUpdateAll: vi.fn(),
		globalUpdateStatus: { hasActiveUpdates: false, activeCount: 0 },
		isLoading: false,
		hasError: false,
	}),
}));

// Epic 5375 slice 01 — US-01 AC-1.1 and D16. A third way in, next to Overview and System Settings,
// wearing whatever this instance calls its Features.
const renderHeaderWhereFeaturesAreCalled = (featuresTerm: string) => {
	const terminology: ITerminology[] = [
		{
			id: 1,
			key: TERMINOLOGY_KEYS.FEATURES,
			defaultValue: "Features",
			value: featuresTerm,
			description: "Term used for multiple features",
		},
	];

	const terminologyService = createMockTerminologyService();
	terminologyService.getAllTerminology = vi.fn().mockResolvedValue(terminology);

	const apiContext = createMockApiServiceContext({
		rbacService: createMockRbacService(),
		terminologyService,
	});

	return render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider value={apiContext}>
				<TerminologyProvider>
					<MemoryRouter>
						<Header />
					</MemoryRouter>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("Header — the way in to the Features view", () => {
	it("offers a third way in beside Overview and System Settings", async () => {
		renderHeaderWhereFeaturesAreCalled("Features");

		await waitFor(() => {
			expect(
				screen.getAllByRole("link", { name: "Features" }).length,
			).toBeGreaterThan(0);
		});
	});

	it("leads to the Features view", async () => {
		renderHeaderWhereFeaturesAreCalled("Features");

		await waitFor(() => {
			expect(
				screen.getAllByRole("link", { name: "Features" })[0],
			).toHaveAttribute("href", "/features");
		});
	});

	it("wears the word this instance uses for its features", async () => {
		renderHeaderWhereFeaturesAreCalled("Deliverables");

		await waitFor(() => {
			expect(
				screen.getAllByRole("link", { name: "Deliverables" }).length,
			).toBeGreaterThan(0);
		});
		expect(screen.queryByRole("link", { name: "Features" })).toBeNull();
	});
});
