import { cleanup, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IYardstick } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import {
	defaultRefinementTerms,
	gravitysRefinement,
	renderTheRefinementTab,
} from "../../../../tests/RefinementTabTestKit";

/**
 * The question every voter answers, and the number it is answered against. Under the heading the tab
 * shows exactly one more line: the question, followed by an info icon whose tooltip says where the
 * number comes from - the Team's SLE, a fallback from its cycle time, or nothing at all. There is no
 * other hint and no link. The server sends facts; the words, including every renameable term, are put
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

/** The question line, and the info icon that sits on it, found by the words it carries. */
const theQuestion = async (question: string) =>
	await screen.findByText(question, { exact: true });

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
 * Everything between the heading and the grid, sibling by sibling. The maintainer's rule is one line
 * there and never more, so the list must hold the question line alone.
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
		"with %s puts exactly one line between the heading and the list, and no link",
		async (_, yardstick, question) => {
			renderWithTheYardstick(yardstick);
			await theQuestion(question);

			const between = whatSitsBetweenTheHeadingAndTheList();

			expect(between).toHaveLength(1);
			expect(between[0].textContent?.trim()).toBe(question);
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
