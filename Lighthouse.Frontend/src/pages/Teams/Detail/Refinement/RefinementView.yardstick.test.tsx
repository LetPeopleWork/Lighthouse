import { act, cleanup, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IYardstick } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import {
	defaultRefinementTerms,
	gravitysRefinement,
	renderTheRefinementTab,
} from "../../../../tests/RefinementTabTestKit";

/**
 * The question every voter answers, and the number it is answered against. The vote column is headed
 * by the question, followed by an info icon whose tooltip says where the number comes from - the Team's
 * SLE, a fallback from its cycle time, or nothing at all. Nothing else sits between the heading and the
 * list, there is no other hint and no link. The server sends facts; the words, including every renameable term, are put
 * together here.
 */

const { terms, mockUseLicenseRestrictions } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
	mockUseLicenseRestrictions: vi.fn(),
}));

vi.mock("../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

const SLE_75_WITHIN_7: IYardstick = {
	source: "Sle",
	days: 7,
	probability: 75,
};

const FALLBACK_OF_12: IYardstick = {
	source: "CycleTimeFallback",
	days: 12,
	probability: 85,
};

const NOTHING_TO_GO_ON: IYardstick = {
	source: "Unavailable",
	days: null,
	probability: null,
};

const renderWithTheYardstick = (yardstick: IYardstick) =>
	renderTheRefinementTab(gravitysRefinement({ yardstick }));

/** The header of the column a voter votes in. */
const theVoteColumnHeader = () => {
	const header = within(screen.getByRole("grid"))
		.getAllByRole("columnheader")
		.find((columnHeader) => columnHeader.dataset.field === "myVote");
	if (header === undefined) {
		throw new Error("The grid has no vote column");
	}
	return header;
};

/** The question, found by the words it carries in the vote column's header. */
const theQuestion = async (question: string) => {
	const shown = await screen.findByText(question, { exact: true });
	expect(theVoteColumnHeader()).toContainElement(shown);
	return shown;
};

/** Hovering the info icon shows its tooltip; the icon's accessible name is the same text. */
const theTooltipShownFor = async (
	user: ReturnType<typeof renderTheRefinementTab>["user"],
	tooltip: string,
) => {
	const icon = screen.getByLabelText(tooltip);
	await user.hover(icon);
	return await screen.findByRole("tooltip");
};

/**
 * Everything between the heading and the grid, sibling by sibling. Only the next Refinement belongs
 * there, on the heading's own row; the question has moved into the vote column's header.
 */
const whatSitsBetweenTheHeadingAndTheList = () => {
	const heading = screen.getByRole("heading", { level: 2 });
	const grid = screen.getByRole("grid");
	const between: Element[] = [];

	for (
		let sibling = heading.nextElementSibling;
		sibling !== null && !sibling.contains(grid);
		sibling = sibling.nextElementSibling
	) {
		between.push(sibling);
	}

	return between;
};

describe("The Refinement tab asks one question against one number", () => {
	beforeEach(() => {
		localStorage.clear();
		terms.current = { ...defaultRefinementTerms };
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	// @us-10 @slice-10 @driving_port @contract-shape:pure-function
	it("asks whether a Work Item is doable within the Team's SLE and says the SLE in the tooltip", async () => {
		const { user } = renderWithTheYardstick(SLE_75_WITHIN_7);

		expect(await theQuestion("Doable within 7 days?")).toBeVisible();
		expect(
			await theTooltipShownFor(user, "SLE 75% of work items in 7 days or less"),
		).toHaveTextContent("SLE 75% of work items in 7 days or less");
	});

	// @us-10 @slice-10 @contract-shape:pure-function
	it("without an SLE asks against the fallback and says in the tooltip that it is one", async () => {
		const { user } = renderWithTheYardstick(FALLBACK_OF_12);

		expect(await theQuestion("Doable within 12 days?")).toBeVisible();
		expect(
			await theTooltipShownFor(
				user,
				"No SLE set, based off 85% of historical cycle time",
			),
		).toHaveTextContent("No SLE set, based off 85% of historical cycle time");
	});

	// @us-10 @slice-10 @error @contract-shape:pure-function
	it("without an SLE or finished Work Items asks without a number and says why in the tooltip", async () => {
		const { user } = renderWithTheYardstick(NOTHING_TO_GO_ON);

		expect(await theQuestion("Doable within our SLE?")).toBeVisible();
		expect(
			await theTooltipShownFor(
				user,
				"No SLE is set and no Work Items have finished yet",
			),
		).toHaveTextContent("No SLE is set and no Work Items have finished yet");
	});

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it.each([
		["the Team's SLE", SLE_75_WITHIN_7, "Doable within 7 days?"],
		["the fallback", FALLBACK_OF_12, "Doable within 12 days?"],
		["no number", NOTHING_TO_GO_ON, "Doable within our SLE?"],
	])(
		"with %s asks the question in the vote column's header, puts only the next Refinement between the heading and the list, and no link",
		async (_, yardstick, question) => {
			renderWithTheYardstick(yardstick);
			await theQuestion(question);

			const between = whatSitsBetweenTheHeadingAndTheList();

			expect(between).toHaveLength(1);
			expect(between[0].textContent?.trim()).toBe("No Refinement cadence");
			expect(theVoteColumnHeader()).toHaveTextContent(question);
			expect(screen.queryByRole("link", { name: /sle|setting/i })).toBeNull();
		},
	);

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it("says one day in the singular", async () => {
		renderWithTheYardstick({ source: "Sle", days: 1, probability: 85 });

		expect(await theQuestion("Doable within 1 day?")).toBeVisible();
	});

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it("says one day in the singular in the tooltip too", async () => {
		const { user } = renderWithTheYardstick({
			source: "Sle",
			days: 1,
			probability: 85,
		});
		await theQuestion("Doable within 1 day?");

		expect(
			await theTooltipShownFor(user, "SLE 85% of work items in 1 day or less"),
		).toHaveTextContent("SLE 85% of work items in 1 day or less");
	});

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it.each([
		[
			"a source it does not know",
			{ source: "Somethingelse" as never, days: null, probability: null },
		],
		["an SLE of no days", { source: "Sle", days: 0, probability: 85 }],
		[
			"a fallback without days",
			{ source: "CycleTimeFallback", days: null, probability: 85 },
		],
		[
			"a source it does not know even when days come with it",
			{ source: "Somethingelse" as never, days: 5, probability: 85 },
		],
	] as [string, IYardstick][])(
		"treats %s as having no number",
		async (_, yardstick) => {
			const { user } = renderWithTheYardstick(yardstick);

			expect(await theQuestion("Doable within our SLE?")).toBeVisible();
			expect(
				await theTooltipShownFor(
					user,
					"No SLE is set and no Work Items have finished yet",
				),
			).toHaveTextContent("No SLE is set and no Work Items have finished yet");
		},
	);

	// @us-10 @slice-10 @contract-shape:pure-function
	it("lets a screen reader hear the tooltip once, as the icon's name", async () => {
		const { user } = renderWithTheYardstick(SLE_75_WITHIN_7);
		const tooltip = "SLE 75% of work items in 7 days or less";
		await theQuestion("Doable within 7 days?");

		await theTooltipShownFor(user, tooltip);

		const icon = screen.getByRole("button", { name: tooltip });
		expect(icon).toHaveAccessibleName(tooltip);
		expect(icon).not.toHaveAttribute("aria-describedby");
		expect(icon).toHaveAccessibleDescription("");
	});

	// @us-10 @slice-10 @contract-shape:pure-function
	it("opens the tooltip when the icon is reached with the keyboard from the grid's first header", async () => {
		const { user } = renderWithTheYardstick(SLE_75_WITHIN_7);
		const tooltip = "SLE 75% of work items in 7 days or less";
		await theQuestion("Doable within 7 days?");
		const icon = screen.getByRole("button", { name: tooltip });
		// jsdom only guesses whether focus came from the keyboard, and once any earlier button on the
		// page has been tabbed through, it guesses wrong for every later one; a browser does not. So the
		// keyboard walk starts in the grid's header rather than at the top of the page.
		act(() =>
			screen.getByRole("columnheader", { name: "Work Item Name" }).focus(),
		);

		for (
			let presses = 0;
			presses < 20 && icon !== document.activeElement;
			presses++
		) {
			await user.tab();
		}

		expect(icon).toHaveFocus();
		expect(await screen.findByRole("tooltip")).toHaveTextContent(tooltip);
	});

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it("opens the tooltip when the icon in the header is clicked, and the click neither sorts nor takes focus away", async () => {
		const { user } = renderWithTheYardstick(SLE_75_WITHIN_7);
		const tooltip = "SLE 75% of work items in 7 days or less";
		await theQuestion("Doable within 7 days?");
		const rowsBefore = screen.getAllByRole("row").map((row) => row.textContent);
		const icon = screen.getByRole("button", { name: tooltip });

		await user.click(icon);

		expect(await screen.findByRole("tooltip")).toHaveTextContent(tooltip);
		expect(icon).toHaveFocus();
		expect(theVoteColumnHeader()).not.toHaveAttribute("aria-sort", "ascending");
		expect(theVoteColumnHeader()).not.toHaveAttribute(
			"aria-sort",
			"descending",
		);
		expect(screen.getAllByRole("row").map((row) => row.textContent)).toEqual(
			rowsBefore,
		);
	});

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it("asks nothing once nothing is in refinement any more", async () => {
		renderWithTheYardstick(SLE_75_WITHIN_7);
		expect(await theQuestion("Doable within 7 days?")).toBeVisible();
		cleanup();

		renderTheRefinementTab(
			gravitysRefinement({ yardstick: SLE_75_WITHIN_7 }, []),
		);
		await screen.findByText("No Work Items in Refinement states right now");

		expect(screen.queryByText(/^Doable within/)).toBeNull();
	});

	// @us-10 @slice-10 @boundary @contract-shape:pure-function
	it.each([
		[
			SLE_75_WITHIN_7,
			"Doable within 7 days?",
			"Delivery promise 75% of tickets in 7 days or less",
		],
		[
			FALLBACK_OF_12,
			"Doable within 12 days?",
			"No Delivery promise set, based off 85% of historical lead time",
		],
		[
			NOTHING_TO_GO_ON,
			"Doable within our Delivery promise?",
			"No Delivery promise is set and no Tickets have finished yet",
		],
	])(
		"says the Team's own words in the question and the tooltip (%#)",
		async (yardstick, question, tooltip) => {
			terms.current = {
				...defaultRefinementTerms,
				[TERMINOLOGY_KEYS.SLE]: "Delivery promise",
				[TERMINOLOGY_KEYS.CYCLE_TIME]: "Lead Time",
				[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			};
			const { user } = renderWithTheYardstick(yardstick);

			expect(await theQuestion(question)).toBeVisible();
			expect(await theTooltipShownFor(user, tooltip)).toHaveTextContent(
				tooltip,
			);
		},
	);
});
