import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { ApiServiceContext } from "./Api/ApiServiceContext";
import type { ITerminologyService } from "./Api/TerminologyService";
import { TerminologyProvider, useTerminology } from "./TerminologyContext";

/**
 * The word a screen prints while the instance's own list is still on its way, and the word it prints
 * when that list never arrives. The list is fetched once and never retried, so whatever is printed
 * in the second case is what a reader sees for the rest of the session.
 */

const mockGetAllTerminology = vi.fn();

const AWordFor = ({ term }: { readonly term: string }) => {
	const { getTerm } = useTerminology();
	return <span data-testid="the-word">{getTerm(term)}</span>;
};

const renderTheWordFor = (term: string) => {
	const terminologyService = {
		getAllTerminology: mockGetAllTerminology,
	} as unknown as ITerminologyService;

	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

	render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ terminologyService })}
			>
				<TerminologyProvider>
					<AWordFor term={term} />
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

const theWordOnScreen = () => screen.getByTestId("the-word").textContent;

describe("TerminologyContext", () => {
	beforeEach(() => {
		vi.resetAllMocks();
	});

	it("prints the product's own word while the instance's list is still on its way", () => {
		mockGetAllTerminology.mockReturnValue(new Promise(() => undefined));

		renderTheWordFor("features");

		expect(theWordOnScreen()).toBe("Features");
	});

	// Nothing retries this fetch, so a failure is not a moment - it is the rest of the session. The
	// lowercase key was reaching the screen as ordinary prose: "the order of your features".
	it("prints the product's own word when the instance's list never arrives", async () => {
		mockGetAllTerminology.mockRejectedValue(new Error("unreachable"));

		renderTheWordFor("workItems");

		await waitFor(() => {
			expect(theWordOnScreen()).toBe("Work Items");
		});
	});

	it("prints the instance's own word once its list arrives", async () => {
		mockGetAllTerminology.mockResolvedValue([
			{
				id: 1,
				key: "features",
				defaultValue: "Features",
				description: "Term used for multiple features",
				value: "Deliverables",
			},
		]);

		renderTheWordFor("features");

		await waitFor(() => {
			expect(theWordOnScreen()).toBe("Deliverables");
		});
	});

	// A key the product has no word for is a caller's mistake, and printing it verbatim is what makes
	// it noticeable. There is no better word to reach for, and inventing one buries the mistake.
	it("prints a key the product has no word for exactly as asked", async () => {
		mockGetAllTerminology.mockRejectedValue(new Error("unreachable"));

		renderTheWordFor("somethingNobodyNamed");

		await waitFor(() => {
			expect(theWordOnScreen()).toBe("somethingNobodyNamed");
		});
	});
});
