import {
	type Query,
	QueryClient,
	QueryClientProvider,
	QueryObserver,
} from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import { type ReactNode, useContext } from "react";
import { MemoryRouter, useNavigate } from "react-router";
import { describe, expect, it, vi } from "vitest";
import PageMoveRefresh from "../components/App/PageMoveRefresh/PageMoveRefresh";
import { ApiError } from "../services/Api/ApiError";
import { askSharedReadsAgainFor } from "./askSharedReadsAgainFor";
import { PageMovesContext } from "./PageMovesContext";
import {
	pageOfTheLatestAskFor,
	recordAskOnPage,
	recordAskOnPageFor,
} from "./pageOfTheLatestAsk";
import { usePageFreshLoader, usePageFreshQuery } from "./usePageFreshQuery";
import { listCouldNotBeLoaded } from "./useUnitLists";

const KEY = ["things"];

class PageScenario {
	page = 0;
	readonly client = new QueryClient();
	readonly wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={this.client}>
			<PageMovesContext.Provider value={this.page}>
				{children}
			</PageMovesContext.Provider>
		</QueryClientProvider>
	);

	reader(ask: () => Promise<string>, options?: { enabled?: boolean }) {
		return renderHook(() => usePageFreshQuery(KEY, ask, options), {
			wrapper: this.wrapper,
		});
	}
}

describe("usePageFreshQuery", () => {
	it("is loading until the ask answers, then gives the answer without a failure", async () => {
		const scenario = new PageScenario();
		const ask = vi.fn().mockResolvedValue("A");

		const { result } = scenario.reader(ask);

		expect(result.current).toMatchObject({
			isLoading: true,
			isFailed: false,
			answer: undefined,
		});
		await waitFor(() => expect(result.current.answer).toBe("A"));
		expect(result.current).toMatchObject({
			isLoading: false,
			isFailed: false,
			failure: undefined,
		});
		expect(ask).toHaveBeenCalledTimes(1);
	});

	it("does not ask while it is not enabled", async () => {
		const scenario = new PageScenario();
		const ask = vi.fn().mockResolvedValue("A");

		const { result } = scenario.reader(ask, { enabled: false });

		await act(async () => {
			await Promise.resolve();
		});
		expect(ask).not.toHaveBeenCalled();
		expect(result.current.isLoading).toBe(true);
	});

	it("shows the failure of the ask once, without asking again alone", async () => {
		const scenario = new PageScenario();
		const outage = new Error("down");
		const ask = vi.fn().mockRejectedValue(outage);

		const { result } = scenario.reader(ask);

		await waitFor(() => expect(result.current.isFailed).toBe(true));
		expect(result.current).toMatchObject({
			isLoading: false,
			answer: undefined,
			failure: outage,
		});
		expect(ask).toHaveBeenCalledTimes(1);
	});

	it("asks again on request, and shows a second failure as a failure", async () => {
		const scenario = new PageScenario();
		const ask = vi
			.fn()
			.mockRejectedValueOnce(new Error("first"))
			.mockResolvedValueOnce("B")
			.mockRejectedValue(new Error("third"));
		const { result } = scenario.reader(ask);
		await waitFor(() => expect(result.current.isFailed).toBe(true));

		act(() => result.current.askAgain());
		expect(result.current.isFailed).toBe(false);
		await waitFor(() => expect(result.current.answer).toBe("B"));

		act(() => {
			result.current.askAgain();
		});
		await waitFor(() => expect(ask).toHaveBeenCalledTimes(3));
		await waitFor(() => expect(result.current.isFailed).toBe(false));
	});

	it("shows a second failed ask again as failed once it has settled", async () => {
		const scenario = new PageScenario();
		const ask = vi
			.fn()
			.mockRejectedValueOnce(new Error("first"))
			.mockRejectedValue(new Error("second"));
		const { result } = scenario.reader(ask);
		await waitFor(() => expect(result.current.isFailed).toBe(true));

		act(() => result.current.askAgain());

		await waitFor(() => expect(ask).toHaveBeenCalledTimes(2));
		await waitFor(() =>
			expect(result.current.failure).toEqual(new Error("second")),
		);
		expect(result.current.isFailed).toBe(true);
	});

	it("lets a reader that appears on the same page reuse the answer without asking", async () => {
		const scenario = new PageScenario();
		const ask = vi.fn().mockResolvedValue("A");
		const first = scenario.reader(ask);
		await waitFor(() => expect(first.result.current.answer).toBe("A"));

		const second = scenario.reader(ask);

		await waitFor(() => expect(second.result.current.answer).toBe("A"));
		expect(ask).toHaveBeenCalledTimes(1);
	});

	it("asks again for a reader that appears after a page move, and gives it the new answer", async () => {
		const scenario = new PageScenario();
		const ask = vi.fn().mockResolvedValueOnce("old").mockResolvedValue("new");
		const first = scenario.reader(ask);
		await waitFor(() => expect(first.result.current.answer).toBe("old"));

		scenario.page = 1;
		const second = scenario.reader(ask);

		await waitFor(() => expect(second.result.current.answer).toBe("new"));
		expect(ask).toHaveBeenCalledTimes(2);
	});

	it("asks again for a reader that appears after a page move when the earlier ask failed", async () => {
		const scenario = new PageScenario();
		const ask = vi
			.fn()
			.mockRejectedValueOnce(new Error("first page"))
			.mockResolvedValue("recovered");
		const first = scenario.reader(ask);
		await waitFor(() => expect(first.result.current.isFailed).toBe(true));

		scenario.page = 1;
		const second = scenario.reader(ask);

		await waitFor(() => expect(second.result.current.answer).toBe("recovered"));
		expect(ask).toHaveBeenCalledTimes(2);
	});

	it("does not ask a second time for a reader that appears while the ask of the page is on its way", async () => {
		const scenario = new PageScenario();
		let answerSlowly: (value: string) => void = () => undefined;
		const ask = vi
			.fn()
			.mockResolvedValueOnce("old")
			.mockImplementation(
				() =>
					new Promise<string>((resolve) => {
						answerSlowly = resolve;
					}),
			);
		const first = scenario.reader(ask);
		await waitFor(() => expect(first.result.current.answer).toBe("old"));
		scenario.page = 1;
		const second = scenario.reader(ask);
		await waitFor(() => expect(ask).toHaveBeenCalledTimes(2));

		const third = scenario.reader(ask);
		act(() => {
			answerSlowly("new");
		});

		await waitFor(() => expect(third.result.current.answer).toBe("new"));
		expect(second.result.current.answer).toBe("new");
		expect(ask).toHaveBeenCalledTimes(2);
	});

	it("keeps showing nothing for an old answer when the reader appeared on a later page", async () => {
		const scenario = new PageScenario();
		const ask = vi.fn().mockResolvedValue("old");
		const first = scenario.reader(ask);
		await waitFor(() => expect(first.result.current.answer).toBe("old"));
		scenario.page = 1;
		let answerSlowly: (value: string) => void = () => undefined;
		ask.mockImplementation(
			() =>
				new Promise<string>((resolve) => {
					answerSlowly = resolve;
				}),
		);

		const second = scenario.reader(ask);

		expect(second.result.current).toMatchObject({
			answer: undefined,
			isLoading: true,
		});
		act(() => {
			answerSlowly("new");
		});
		await waitFor(() => expect(second.result.current.answer).toBe("new"));
	});
});

describe("usePageFreshLoader", () => {
	it("gives the answer a reader already loaded without asking again", async () => {
		const scenario = new PageScenario();
		const ask = vi.fn().mockResolvedValue("A");
		const reader = scenario.reader(ask);
		await waitFor(() => expect(reader.result.current.answer).toBe("A"));
		const loader = renderHook(() => usePageFreshLoader(KEY, ask), {
			wrapper: scenario.wrapper,
		});

		await expect(loader.result.current()).resolves.toBe("A");
		expect(ask).toHaveBeenCalledTimes(1);
	});

	it("hands the caller the error the ask failed with, not a wrapper", async () => {
		const scenario = new PageScenario();
		const outage = new Error("down");
		const ask = vi.fn().mockRejectedValue(outage);
		const loader = renderHook(() => usePageFreshLoader(KEY, ask), {
			wrapper: scenario.wrapper,
		});

		await expect(loader.result.current()).rejects.toBe(outage);
	});
});

describe("the page an ask was made on", () => {
	const aQuery = (client: QueryClient, key: string[]): Query =>
		client.getQueryCache().build(client, { queryKey: key }) as unknown as Query;

	it("is -1 for a query never asked, and the recorded page after recording", () => {
		const client = new QueryClient();
		const query = aQuery(client, ["a"]);
		expect(pageOfTheLatestAskFor(query)).toBe(-1);

		recordAskOnPage(query, 3);

		expect(pageOfTheLatestAskFor(query)).toBe(3);
	});

	it("records on the query of exactly that key, and on none when there is none", () => {
		const client = new QueryClient();
		const longer = aQuery(client, ["a", "b"]);

		recordAskOnPageFor({ client, queryKey: ["a"] } as never, 2);
		expect(pageOfTheLatestAskFor(longer)).toBe(-1);

		const exact = aQuery(client, ["a"]);
		recordAskOnPageFor({ client, queryKey: ["a"] } as never, 2);
		expect(pageOfTheLatestAskFor(exact)).toBe(2);
		expect(pageOfTheLatestAskFor(longer)).toBe(-1);
	});
});

describe("asking the shared reads again after a page move", () => {
	it("leaves a read that is not shared alone", async () => {
		const client = new QueryClient();
		const ask = vi.fn().mockResolvedValue("x");
		const observer = new QueryObserver(client, {
			queryKey: ["other"],
			queryFn: ask,
			staleTime: Number.POSITIVE_INFINITY,
		});
		const unsubscribe = observer.subscribe(() => undefined);
		await waitFor(() => expect(ask).toHaveBeenCalledTimes(1));

		await askSharedReadsAgainFor(client, 1);

		expect(ask).toHaveBeenCalledTimes(1);
		unsubscribe();
	});

	it("counts every page move, so a second move is page 2", async () => {
		const pages: number[] = [];
		const client = new QueryClient();
		let go: (to: string) => void = () => undefined;
		const Probe = () => {
			pages.push(useContext(PageMovesContext));
			go = useNavigate();
			return null;
		};
		renderHook(() => null, {
			wrapper: () => (
				<QueryClientProvider client={client}>
					<MemoryRouter initialEntries={["/a"]}>
						<PageMoveRefresh>
							<Probe />
						</PageMoveRefresh>
					</MemoryRouter>
				</QueryClientProvider>
			),
		});

		await act(async () => go("/b"));
		await act(async () => go("/c"));

		expect(pages.at(-1)).toBe(2);
		expect(pages[0]).toBe(0);
	});
});

describe("listCouldNotBeLoaded", () => {
	const read = (isFailed: boolean, failure: unknown) =>
		({
			answer: undefined,
			isLoading: false,
			isFailed,
			failure,
			askAgain: () => undefined,
		}) as never;

	it("is true for an outage, false for a refusal and false when nothing failed", () => {
		expect(listCouldNotBeLoaded(read(true, new Error("down")))).toBe(true);
		expect(
			listCouldNotBeLoaded(read(true, new ApiError(403, "Forbidden"))),
		).toBe(false);
		expect(listCouldNotBeLoaded(read(false, new Error("old")))).toBe(false);
	});
});
