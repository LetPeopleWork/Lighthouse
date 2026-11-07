import { screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementBandSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { renderRefinementSection } from "../../../tests/renderRefinementSection";
import RefinementBandSettings from "./RefinementBandSettings";

/**
 * What the band part of a Team's Refinement settings shows for a value and what it hands back when
 * the admin edits it: the two likelihoods the range is read at, which ends are marked as refused and
 * why, and the explanation behind the info icon. The form's own tests prove that the form hands the
 * section its value, saves what the section hands back, and saves nothing while a likelihood is refused.
 */

const { terms } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.REFINEMENT]: "Refinement",
	[TERMINOLOGY_KEYS.REFINEMENTS]: "Refinements",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
};

const LOW_END = /^Low end likelihood/;
const HIGH_END = /^High end likelihood/;

const THE_DEFAULT_BAND: IRefinementBandSetting = {
	lowPercentile: 50,
	highPercentile: 85,
};

const renderBand = (band: IRefinementBandSetting = THE_DEFAULT_BAND) => {
	const rendered = renderRefinementSection(band, (value, onChange) => (
		<RefinementBandSettings band={value} onChange={onChange} />
	));

	const replaceTheLikelihood = async (label: RegExp, value: string) => {
		const field = screen.getByRole("spinbutton", { name: label });
		await rendered.user.clear(field);
		await rendered.user.type(field, value);
	};

	// What the info icon beside the band's heading says when the admin points at it.
	const theBandExplanation = async () => {
		await rendered.user.hover(
			screen.getByRole("button", { name: /^Based on the Team's / }),
		);
		return (await screen.findByRole("tooltip")).textContent ?? "";
	};

	return { ...rendered, replaceTheLikelihood, theBandExplanation };
};

describe("The band in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
	});

	it("shows the range read at 50% and 85% until the admin changes it", () => {
		renderBand();

		expect(screen.getByRole("spinbutton", { name: LOW_END })).toHaveValue(50);
		expect(screen.getByRole("spinbutton", { name: HIGH_END })).toHaveValue(85);
	});

	it.each([
		["90", "85"],
		["85", "85"],
	])(
		"refuses a low end of %s against a high end of %s, naming both, and saves nothing",
		async (low, high) => {
			const { replaceTheLikelihood } = renderBand({
				lowPercentile: 50,
				highPercentile: Number(high),
			});

			await replaceTheLikelihood(LOW_END, low);

			expect(
				screen.getByText(
					`The low end (${low}%) must be below the high end (${high}%).`,
				),
			).toBeVisible();
		},
	);

	it.each([
		["Low end likelihood", "49"],
		["High end likelihood", "96"],
	])(
		"refuses %s at %s, outside 50 to 95, and saves nothing",
		async (fieldName, value) => {
			const label = new RegExp(`^${fieldName}`);
			const { replaceTheLikelihood } = renderBand();

			await replaceTheLikelihood(label, value);

			const field = screen.getByRole("spinbutton", { name: label });
			expect(field).toHaveAttribute("aria-invalid", "true");
			expect(field).toHaveAccessibleDescription("Between 50% and 95%.");
		},
	);

	it("marks the high end being typed in when it falls below the low end, and explains why there", async () => {
		const { replaceTheLikelihood } = renderBand({
			lowPercentile: 60,
			highPercentile: 85,
		});

		await replaceTheLikelihood(HIGH_END, "55");

		const inverted = "The low end (60%) must be below the high end (55%).";
		const high = screen.getByRole("spinbutton", { name: HIGH_END });
		expect(high).toHaveAttribute("aria-invalid", "true");
		expect(high).toHaveAccessibleDescription(inverted);
		const low = screen.getByRole("spinbutton", { name: LOW_END });
		expect(low).toHaveAttribute("aria-invalid", "true");
		expect(low).toHaveAccessibleDescription(inverted);
		expect(screen.getAllByText(inverted)).toHaveLength(1);
	});

	it("heads the band with the Team's words for work items and Refinement", () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
		};
		renderBand();

		expect(
			screen.getByText("Tickets needed before the next Grooming"),
		).toBeVisible();
	});

	it("explains the high end at 95 as reached with only 5% likelihood", async () => {
		const { replaceTheLikelihood, theBandExplanation } = renderBand();

		await replaceTheLikelihood(HIGH_END, "95");

		expect(await theBandExplanation()).toContain(
			"The Team pulls at least the low end with 50% likelihood, " +
				"and more than the high end with only 5% likelihood.",
		);
	});

	it("keeps explaining the last valid band while the low end is cleared", async () => {
		const { user, theBandExplanation } = renderBand();

		await user.clear(screen.getByRole("spinbutton", { name: LOW_END }));

		const explanation = await theBandExplanation();
		expect(explanation).toContain(
			"low end with 50% likelihood, and more than the high end with only 15% likelihood.",
		);
		expect(explanation).not.toContain("NaN");
	});

	it("explains the band as last edited, not as it was when the form opened", async () => {
		const { user, replaceTheLikelihood, theBandExplanation } = renderBand();

		await replaceTheLikelihood(HIGH_END, "90");
		const afterTheEdit = "more than the high end with only 10% likelihood.";
		expect(await theBandExplanation()).toContain(afterTheEdit);

		await user.clear(screen.getByRole("spinbutton", { name: LOW_END }));
		expect(await theBandExplanation()).toContain(afterTheEdit);
	});

	it("explains the band as last edited when only the low end changed", async () => {
		const { user, replaceTheLikelihood, theBandExplanation } = renderBand();

		await replaceTheLikelihood(LOW_END, "60");
		const afterTheEdit = "pulls at least the low end with 40% likelihood";
		expect(await theBandExplanation()).toContain(afterTheEdit);

		await user.clear(screen.getByRole("spinbutton", { name: HIGH_END }));
		expect(await theBandExplanation()).toContain(afterTheEdit);
	});

	// The band is read over one Refinement cycle, so the explanation names that cycle, not the days until the next.
	it("explains the range as a How Many forecast between the next Refinement and the one after, in the Team's word", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
		};
		const { theBandExplanation } = renderBand();

		const explanation = await theBandExplanation();
		expect(explanation).toContain(
			"a How Many forecast for the working days between the next Grooming and the one after. ",
		);
		expect(explanation).not.toContain("until the next");
	});

	it("shows a cleared low end as empty, not as 0, and says what it takes", async () => {
		const { user } = renderBand();

		const low = screen.getByRole("spinbutton", { name: LOW_END });
		await user.clear(low);

		expect(low).toHaveValue(null);
		expect(low).toHaveAccessibleDescription("Between 50% and 95%.");
	});

	it("marks neither end while the band is valid, and shows each end in percent", () => {
		renderBand();

		for (const label of [LOW_END, HIGH_END]) {
			const field = screen.getByRole("spinbutton", { name: label });
			expect(field).not.toHaveAttribute("aria-invalid", "true");
			expect(
				within(field.parentElement as HTMLElement).getByText("%"),
			).toBeVisible();
		}
	});

	it("marks only the low end when it is out of range above the high end, since that is not an inverted band", async () => {
		const { replaceTheLikelihood } = renderBand();

		await replaceTheLikelihood(LOW_END, "96");

		const low = screen.getByRole("spinbutton", { name: LOW_END });
		expect(low).toHaveAttribute("aria-invalid", "true");
		expect(low).toHaveAccessibleDescription("Between 50% and 95%.");
		expect(
			screen.getByRole("spinbutton", { name: HIGH_END }),
		).not.toHaveAttribute("aria-invalid", "true");
	});
});
