import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
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

const EveryWordFor = ({ terms }: { readonly terms: readonly string[] }) => {
	const { getTerm } = useTerminology();

	return (
		<div>
			{terms.map((term) => (
				<span key={term} data-testid={`the-word-for-${term}`}>
					{getTerm(term)}
				</span>
			))}
		</div>
	);
};

const renderInsideTheProvider = (children: React.ReactNode) => {
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
				<TerminologyProvider>{children}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

const renderTheWordFor = (term: string) =>
	renderInsideTheProvider(<AWordFor term={term} />);

const theWordOnScreen = () => screen.getByTestId("the-word").textContent;

/**
 * Every word the product has of its own, written out here rather than read back out of the map under
 * test - a check that fetches its answer from the thing it is checking passes whatever that thing
 * says. These are the same words the server writes into a fresh database, so the two lists are one
 * fact kept in two places, and this is the only thing that notices when they stop agreeing.
 */
const theWordsAFreshInstallIsSeededWith: ReadonlyArray<
	readonly [string, string]
> = [
	["workItem", "Work Item"],
	["workItems", "Work Items"],
	["feature", "Feature"],
	["features", "Features"],
	["cycleTime", "Cycle Time"],
	["throughput", "Throughput"],
	["workInProgress", "Work In Progress"],
	["wip", "WIP"],
	["workItemAge", "Work Item Age"],
	["tag", "Tag"],
	["workTrackingSystem", "Work Tracking System"],
	["workTrackingSystems", "Work Tracking Systems"],
	["blocked", "Blocked"],
	["serviceLevelExpectation", "Service Level Expectation"],
	["sle", "SLE"],
	["team", "Team"],
	["teams", "Teams"],
	["portfolio", "Portfolio"],
	["portfolios", "Portfolios"],
	["delivery", "Delivery"],
	["deliveries", "Deliveries"],
];

const theKey = ([key]: readonly [string, string]) => key;

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

	// Every one of these words can be blanked without a single other test noticing, and a blanked one
	// shows up as a gap in a sentence rather than as a failure. They are also the client's copy of what
	// the server seeds a fresh database with, and two copies of one list drift.
	it("prints the word a fresh install is seeded with, for every term the product names", () => {
		mockGetAllTerminology.mockReturnValue(new Promise(() => undefined));

		renderInsideTheProvider(
			<EveryWordFor terms={theWordsAFreshInstallIsSeededWith.map(theKey)} />,
		);

		for (const [key, word] of theWordsAFreshInstallIsSeededWith) {
			expect(screen.getByTestId(`the-word-for-${key}`).textContent).toBe(word);
		}
	});

	// A term added to the product without a word of its own reaches the screen as its own lookup key -
	// lowercase, mid-sentence, read as prose. The list above only guards that while it names every term
	// there is, so it has to be told when a new one appears.
	it("has a word of its own for every term the product names", () => {
		expect(theWordsAFreshInstallIsSeededWith.map(theKey).sort()).toEqual(
			[...Object.values(TERMINOLOGY_KEYS)].sort(),
		);
	});
});
