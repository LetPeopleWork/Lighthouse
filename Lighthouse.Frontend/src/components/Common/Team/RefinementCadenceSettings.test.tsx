import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IRefinementCadenceSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { renderRefinementSection } from "../../../tests/renderRefinementSection";
import RefinementCadenceSettings from "./RefinementCadenceSettings";

/**
 * What the cadence part of a Team's Refinement settings shows for a value and what it hands back
 * when the admin edits it: the weekdays, how many weeks apart, and which fields are marked as refused.
 * The form's own tests prove that the form hands the section its value, saves what the section hands
 * back, and saves nothing while the cadence has a mistake.
 */

const TERMS: Record<string, string> = {
	[TERMINOLOGY_KEYS.REFINEMENT]: "Refinement",
	[TERMINOLOGY_KEYS.REFINEMENTS]: "Refinements",
};

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => TERMS[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

const REPEAT_EVERY = "Repeat every (weeks)";

const THURSDAYS_EVERY_WEEK: IRefinementCadenceSetting = {
	weekdays: ["Thursday"],
	intervalWeeks: 1,
	anchorWeek: null,
};

const renderCadence = (cadence: IRefinementCadenceSetting | null = null) => {
	const rendered = renderRefinementSection(cadence, (value, onChange) => (
		<RefinementCadenceSettings cadence={value} onChange={onChange} />
	));

	const replaceTheNumberIn = async (label: string, value: string) => {
		const field = screen.getByRole("spinbutton", { name: label });
		await rendered.user.clear(field);
		await rendered.user.type(field, value);
	};

	return { ...rendered, replaceTheNumberIn };
};

describe("The Refinement cadence in the Refinement section of a Team's settings", () => {
	it("offers the weekdays Monday to Sunday and every week, with no starting week asked for", () => {
		renderCadence();

		expect(screen.getByText("Refinement cadence")).toBeVisible();
		const weekdays = [
			"Monday",
			"Tuesday",
			"Wednesday",
			"Thursday",
			"Friday",
			"Saturday",
			"Sunday",
		];
		expect(
			screen
				.getAllByRole("checkbox")
				.map((box) => box.closest("label")?.textContent ?? "")
				.filter((label) => weekdays.includes(label)),
		).toEqual(weekdays);
		expect(screen.getByRole("spinbutton", { name: REPEAT_EVERY })).toHaveValue(
			1,
		);
		expect(screen.queryByLabelText("Starting week")).not.toBeInTheDocument();
	});

	it("saves nothing for fewer than one week between Refinements", async () => {
		const { replaceTheNumberIn } = renderCadence(THURSDAYS_EVERY_WEEK);

		await replaceTheNumberIn(REPEAT_EVERY, "0");

		expect(
			screen.getByRole("spinbutton", { name: REPEAT_EVERY }),
		).toHaveAttribute("aria-invalid", "true");
	});

	// The server looks a year ahead for the next Refinement, so a longer gap could leave it none.
	it("saves nothing for more than 52 weeks between Refinements, and says why", async () => {
		const { replaceTheNumberIn } = renderCadence(THURSDAYS_EVERY_WEEK);

		await replaceTheNumberIn(REPEAT_EVERY, "53");

		expect(
			screen.getByRole("spinbutton", { name: REPEAT_EVERY }),
		).toHaveAttribute("aria-invalid", "true");
		expect(
			screen.getByText(/^Refinements are at most 52 weeks apart\.$/),
		).toBeVisible();
	});

	it("leaves the Team without a cadence when the last weekday is unticked", async () => {
		const { user, latest } = renderCadence(THURSDAYS_EVERY_WEEK);

		await user.click(screen.getByRole("checkbox", { name: "Thursday" }));

		expect(latest()?.weekdays).toEqual([]);
	});
});
