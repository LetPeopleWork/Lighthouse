import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { PAGE_SIZE } from "../../../components/Common/DataGrid/RowRangeFooter";
import type { IFeatureList } from "../../../models/FeatureList";
import {
	aFeatureServiceAnswering,
	aList,
	aRow,
	aTeamMetricsService,
	NorthwindMobile,
	NorthwindPlatform,
	NorthwindPlatformsList,
	RenamedTerms,
	renderAFeatureList,
	stubTheScreenSizeTheGridAsksFor,
	theListBody,
	theListStatus,
} from "../../../tests/FeatureListTestKit";
import { deferred } from "../../../tests/HeldMetricsService";
import PortfolioFeatureList from "./PortfolioFeatureList";

const COULD_NOT_LOAD = "The Features couldn't be loaded.";

const NO_FEATURES_YET = "This Portfolio has no Features yet.";

const CHECKOUT_REDESIGN = /Checkout redesign/;

const theListKeyOf = (portfolioId: number) => [
	"feature-list",
	"portfolio",
	portfolioId,
];

const aDroppedConnection = () =>
	Promise.reject<IFeatureList>(new Error("the network dropped"));

beforeEach(() => {
	stubTheScreenSizeTheGridAsksFor();
	localStorage.clear();
});

describe("Story 6408 (slice 04): Martin opens Northwind Platform's Features and one answer fills the list", () => {
	it("the list asks once, for the Portfolio's list, and reads nothing else", async () => {
		const featureService = aFeatureServiceAnswering(
			Promise.resolve(NorthwindPlatformsList),
		);
		const teamMetricsService = aTeamMetricsService();

		await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{ featureService, teamMetricsService },
		);

		expect(await screen.findByText(CHECKOUT_REDESIGN)).toBeInTheDocument();
		expect(featureService.getFeatureList).toHaveBeenCalledTimes(1);
		expect(featureService.getFeatureList).toHaveBeenCalledWith({
			kind: "portfolio",
			id: NorthwindPlatform.id,
		});
		expect(featureService.getFeaturesByIds).not.toHaveBeenCalled();
		expect(featureService.getFeaturesByReferences).not.toHaveBeenCalled();
		expect(teamMetricsService.getFeaturesInProgress).not.toHaveBeenCalled();
	});

	it("while the first answer is on its way the list shows a spinner and no rows, and once it lands it says it is ready", async () => {
		const answer = deferred<IFeatureList>();
		const { container } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{ featureService: aFeatureServiceAnswering(answer.promise) },
		);

		expect(theListStatus(container)).toBe("loading");
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(
			screen.queryByRole("row", { name: CHECKOUT_REDESIGN }),
		).not.toBeInTheDocument();
		expect(screen.queryByText(NO_FEATURES_YET)).not.toBeInTheDocument();

		await act(async () => answer.resolve(NorthwindPlatformsList));

		await waitFor(() => expect(theListStatus(container)).toBe("ready"));
		expect(screen.getByText(/Search revamp/)).toBeInTheDocument();
	});

	it("a row's parent is named from the same answer", async () => {
		await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(
					Promise.resolve(NorthwindPlatformsList),
				),
			},
		);

		const row = await screen.findByRole("row", { name: CHECKOUT_REDESIGN });
		expect(within(row).getByText(/Platform 2027/)).toBeInTheDocument();
	});

	it("a row shows the Teams working on it, from the same answer", async () => {
		await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(
					Promise.resolve(NorthwindPlatformsList),
				),
			},
		);

		const row = await screen.findByRole("row", { name: CHECKOUT_REDESIGN });
		const indicator = within(row).getByTestId("active-work-indicator");
		const otherRow = screen.getByRole("row", { name: /Search revamp/ });
		expect(within(otherRow).getByTestId("no-active-work")).toBeInTheDocument();
		expect(
			within(otherRow).queryByTestId("active-work-indicator"),
		).not.toBeInTheDocument();

		await userEvent.hover(indicator);

		const tooltip = await screen.findByRole("tooltip");
		expect(within(tooltip).getByText("Atlas")).toBeInTheDocument();
	});
});

describe("Story 6408 (slice 04): the list says when it cannot show Features", () => {
	it("when the list cannot be loaded it says so and offers to try again", async () => {
		const { container } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{ featureService: aFeatureServiceAnswering(aDroppedConnection()) },
		);

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("error");
		expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument();
	});

	it("Retry asks again and shows the list once it arrives", async () => {
		const featureService = aFeatureServiceAnswering(
			aDroppedConnection(),
			Promise.resolve(NorthwindPlatformsList),
		);
		const { container } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{ featureService },
		);

		await userEvent.click(await screen.findByRole("button", { name: "Retry" }));

		expect(await screen.findByText(CHECKOUT_REDESIGN)).toBeInTheDocument();
		expect(featureService.getFeatureList).toHaveBeenCalledTimes(2);
		expect(theListStatus(container)).toBe("ready");
	});

	it("a Portfolio with no Features yet says so, as an answer that has arrived", async () => {
		const { container } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindMobile} />,
			{ featureService: aFeatureServiceAnswering(Promise.resolve(aList([]))) },
		);

		expect(await screen.findByText(NO_FEATURES_YET)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("ready");
	});

	it("when Hide Completed hides every Feature the list says how many there are and how to see them", async () => {
		// More completed Features than one page of the grid holds, so the count cannot be the page's.
		const completedFeatures = PAGE_SIZE + 7;
		const everyFeatureDone = aList(
			Array.from({ length: completedFeatures }, (_, index) =>
				aRow(100 + index, `NW-${100 + index}`, `Finished work ${index}`, {
					completed: true,
				}),
			),
		);

		await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(
					Promise.resolve(everyFeatureDone),
				),
			},
		);

		expect(
			await screen.findByText(
				`All ${completedFeatures} Features are completed. Turn off Hide Completed Features to see them.`,
			),
		).toBeInTheDocument();
	});

	it("every message uses the instance's own words for Features and Portfolios", async () => {
		await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindMobile} />,
			{
				featureService: aFeatureServiceAnswering(Promise.resolve(aList([]))),
				terms: RenamedTerms,
			},
		);

		expect(
			await screen.findByText("This Program has no Bets yet."),
		).toBeInTheDocument();
	});

	it("the could-not-load message uses the instance's own word for Features", async () => {
		await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(aDroppedConnection()),
				terms: RenamedTerms,
			},
		);

		expect(
			await screen.findByText("The Bets couldn't be loaded."),
		).toBeInTheDocument();
	});
});

describe("Story 6408 (slice 04): after an update the list refreshes without hiding what it shows", () => {
	it.skip("while a refresh is on its way the rows stay, dimmed and out of the pointer's reach, and the list says it is loading", async () => {
		const refresh = deferred<IFeatureList>();
		const { container, queryClient } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(
					Promise.resolve(NorthwindPlatformsList),
					refresh.promise,
				),
			},
		);
		await screen.findByText(CHECKOUT_REDESIGN);

		act(() => {
			void queryClient.invalidateQueries({
				queryKey: theListKeyOf(NorthwindPlatform.id),
			});
		});

		expect(theListStatus(container)).toBe("loading");
		expect(
			screen.getByRole("row", { name: CHECKOUT_REDESIGN }),
		).toBeInTheDocument();
		const dimmed = getComputedStyle(theListBody(container));
		expect(Number(dimmed.opacity)).toBeLessThan(1);
		expect(dimmed.pointerEvents).toBe("none");

		await act(async () => refresh.resolve(NorthwindPlatformsList));
		await waitFor(() => expect(theListStatus(container)).toBe("ready"));
		expect(getComputedStyle(theListBody(container)).pointerEvents).not.toBe(
			"none",
		);
	});

	it.skip("a refresh that fails takes the could-not-load look rather than showing the old rows as current", async () => {
		const { container, queryClient } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(
					Promise.resolve(NorthwindPlatformsList),
					aDroppedConnection(),
				),
			},
		);
		await screen.findByText(CHECKOUT_REDESIGN);

		await act(async () => {
			await queryClient
				.invalidateQueries({ queryKey: theListKeyOf(NorthwindPlatform.id) })
				.catch(() => undefined);
		});

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeInTheDocument();
		expect(theListStatus(container)).toBe("error");
		expect(
			screen.queryByRole("row", { name: CHECKOUT_REDESIGN }),
		).not.toBeInTheDocument();
	});

	it.skip("an answer for the Portfolio the reader has already left never replaces the one they are on", async () => {
		const platformsAnswer = deferred<IFeatureList>();
		const mobilesAnswer = deferred<IFeatureList>();
		const { rerender } = await renderAFeatureList(
			<PortfolioFeatureList portfolio={NorthwindPlatform} />,
			{
				featureService: aFeatureServiceAnswering(
					platformsAnswer.promise,
					mobilesAnswer.promise,
				),
			},
		);

		rerender(<PortfolioFeatureList portfolio={NorthwindMobile} />);
		await act(async () => mobilesAnswer.resolve(aList([])));
		await act(async () => platformsAnswer.resolve(NorthwindPlatformsList));

		expect(await screen.findByText(NO_FEATURES_YET)).toBeInTheDocument();
		expect(screen.queryByText(CHECKOUT_REDESIGN)).not.toBeInTheDocument();
	});
});
