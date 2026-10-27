import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPortfolioSize } from "../../../models/Portfolio/PortfolioSize";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockPortfolioService,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
import PortfolioSizeNote from "./PortfolioSizeNote";

const NORTHWIND_PLATFORM = 2;

const aSize = (featureCount: number): IPortfolioSize => ({
	featureCount,
	threshold: 100,
	isOverThreshold: featureCount > 100,
});

/** A client set up like the app's own: answers stay fresh for minutes and a failed request is tried again. */
const anAppLikeClient = () =>
	new QueryClient({
		defaultOptions: {
			queries: { retry: 2, retryDelay: 1, staleTime: 1000 * 60 * 5 },
		},
	});

const theNoteOpenedOn = (
	queryClient: QueryClient,
	getSize: () => Promise<IPortfolioSize>,
	portfolioId = NORTHWIND_PLATFORM,
) => {
	const portfolioService = {
		...createMockPortfolioService(),
		getSize: vi.fn(getSize),
	};
	const ui = (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					portfolioService,
					terminologyService: createMockTerminologyService(),
				})}
			>
				<TerminologyProvider>
					<PortfolioSizeNote portfolioId={portfolioId} />
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
	return { portfolioService, ui };
};

describe("The Portfolio size note", () => {
	it("reopening the page asks for the size again instead of showing an answer remembered from earlier", async () => {
		const queryClient = anAppLikeClient();
		queryClient.setQueryData(["portfolioSize", NORTHWIND_PLATFORM], aSize(150));
		const { portfolioService, ui } = theNoteOpenedOn(queryClient, () =>
			Promise.resolve(aSize(40)),
		);

		render(ui);

		await waitFor(() =>
			expect(portfolioService.getSize).toHaveBeenCalledWith(NORTHWIND_PLATFORM),
		);
		await waitFor(() =>
			expect(screen.queryByText(/holds 150/)).not.toBeInTheDocument(),
		);
	});

	it("a Portfolio under the threshold shows no note, neither while its size is on the way nor once it is known", async () => {
		const { portfolioService, ui } = theNoteOpenedOn(anAppLikeClient(), () =>
			Promise.resolve(aSize(40)),
		);

		render(ui);

		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
		await waitFor(() => expect(portfolioService.getSize).toHaveBeenCalled());
		await act(async () => {});
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it("moving to another Portfolio asks for that Portfolio's size", async () => {
		const queryClient = anAppLikeClient();
		const { portfolioService, ui } = theNoteOpenedOn(queryClient, () =>
			Promise.resolve(aSize(40)),
		);
		const { portfolioService: serviceForAnother, ui: uiForAnother } =
			theNoteOpenedOn(
				queryClient,
				() => Promise.resolve(aSize(40)),
				NORTHWIND_PLATFORM + 1,
			);

		const { rerender } = render(ui);
		await waitFor(() =>
			expect(portfolioService.getSize).toHaveBeenCalledWith(NORTHWIND_PLATFORM),
		);
		rerender(uiForAnother);

		await waitFor(() =>
			expect(serviceForAnother.getSize).toHaveBeenCalledWith(
				NORTHWIND_PLATFORM + 1,
			),
		);
	});

	it("when the size cannot be checked it says so after one attempt, without retrying", async () => {
		const { portfolioService, ui } = theNoteOpenedOn(anAppLikeClient(), () =>
			Promise.reject(new Error("the network dropped")),
		);

		render(ui);

		expect(
			await screen.findByText("Couldn't check this Portfolio's size."),
		).toBeInTheDocument();
		expect(portfolioService.getSize).toHaveBeenCalledTimes(1);
	});
});
