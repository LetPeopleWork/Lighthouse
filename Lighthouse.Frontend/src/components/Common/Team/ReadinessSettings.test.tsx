import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IReadinessSetting } from "../../../models/Refinement/Refinement";
import { renderRefinementSection } from "../../../tests/renderRefinementSection";
import ReadinessSettings, { DISCUSSION_RULE_ERROR } from "./ReadinessSettings";

/**
 * What the readiness part of a Team's Refinement settings shows for a value and what it hands back
 * when the admin edits it: how many Yes votes, from how many voters, make a Work Item Ready, and how
 * many No votes, or how many "Yes, if…" votes, send it to discussion instead. Each of the two
 * discussion rules can be switched off on its own. The form's own tests prove that the form hands the
 * section its value and saves what the section hands back.
 */

const YES_VOTES_NEEDED = "Yes votes needed";
const VOTERS_NEEDED = "Voters needed";
const NO_RULE = "Send to discussion on No votes";
const YES_IF_RULE = "Send to discussion on “Yes, if…” votes";
const NO_THRESHOLD = "No votes that send to discussion";
const YES_IF_THRESHOLD = "“Yes, if…” votes that send to discussion";

const THE_DEFAULTS: IReadinessSetting = {
	minYes: 3,
	minVoters: 3,
	discussWhen: { no: 1, yesIf: 2 },
};

// Readiness stored before the discussion rules existed carries none.
const STORED_WITHOUT_DISCUSSION_RULES: IReadinessSetting = {
	minYes: 3,
	minVoters: 3,
};

const renderReadiness = (readiness: IReadinessSetting = THE_DEFAULTS) => {
	const rendered = renderRefinementSection(readiness, (value, onChange) => (
		<ReadinessSettings readiness={value} onChange={onChange} />
	));

	const replaceTheNumberIn = async (label: string, value: string) => {
		const field = screen.getByRole("spinbutton", { name: label });
		await rendered.user.clear(field);
		await rendered.user.type(field, value);
	};

	return { ...rendered, replaceTheNumberIn };
};

describe("Readiness in the Refinement section of a Team's settings", () => {
	it("shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them", () => {
		renderReadiness(STORED_WITHOUT_DISCUSSION_RULES);

		expect(
			screen.getByRole("spinbutton", { name: YES_VOTES_NEEDED }),
		).toHaveValue(3);
		expect(screen.getByRole("spinbutton", { name: VOTERS_NEEDED })).toHaveValue(
			3,
		);
		expect(screen.getByText("Send to discussion when")).toBeVisible();
		expect(screen.getByRole("checkbox", { name: NO_RULE })).toBeChecked();
		expect(screen.getByRole("spinbutton", { name: NO_THRESHOLD })).toHaveValue(
			1,
		);
		expect(screen.getByText("or more No votes")).toBeVisible();
		expect(screen.getByRole("checkbox", { name: YES_IF_RULE })).toBeChecked();
		expect(
			screen.getByRole("spinbutton", { name: YES_IF_THRESHOLD }),
		).toHaveValue(2);
		expect(screen.getByText("or more “Yes, if…” votes")).toBeVisible();
	});

	it("refuses fewer than one Yes vote, says why and saves nothing", async () => {
		const { replaceTheNumberIn } = renderReadiness();

		await replaceTheNumberIn(YES_VOTES_NEEDED, "0");

		expect(screen.getByText("At least one Yes vote is needed")).toBeVisible();
	});

	it("refuses fewer voters than Yes votes, says why and saves nothing", async () => {
		const { replaceTheNumberIn } = renderReadiness();

		await replaceTheNumberIn(VOTERS_NEEDED, "2");

		expect(
			screen.getByText("Voters needed cannot be fewer than Yes votes needed"),
		).toBeVisible();
	});

	it("switches off the No rule on its own, keeping the “Yes, if…” rule", async () => {
		const { user, latest } = renderReadiness();

		await user.click(screen.getByRole("checkbox", { name: NO_RULE }));

		expect(latest()).toEqual({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: null, yesIf: 2 },
		});
	});

	it("saves a changed number of “Yes, if…” votes that send to discussion", async () => {
		const { replaceTheNumberIn, latest } = renderReadiness();

		await replaceTheNumberIn(YES_IF_THRESHOLD, "3");

		expect(latest()).toEqual({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: 1, yesIf: 3 },
		});
	});

	it("switches a rule back on at the threshold it had before", async () => {
		const { user, latest } = renderReadiness({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: 4, yesIf: 2 },
		});

		const noRule = screen.getByRole("checkbox", { name: NO_RULE });
		await user.click(noRule);
		expect(latest().discussWhen).toEqual({
			no: null,
			yesIf: 2,
		});
		await user.click(noRule);

		expect(latest()).toEqual({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: 4, yesIf: 2 },
		});
	});

	it("switches on a rule stored as off at its default threshold", async () => {
		const { user, latest } = renderReadiness({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: 1, yesIf: null },
		});

		await user.click(screen.getByRole("checkbox", { name: YES_IF_RULE }));

		expect(latest()).toEqual({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: 1, yesIf: 2 },
		});
	});

	it.each([
		{ field: NO_THRESHOLD, typed: "0" },
		{ field: NO_THRESHOLD, typed: "" },
		{ field: YES_IF_THRESHOLD, typed: "0" },
		{ field: YES_IF_THRESHOLD, typed: "" },
	])(
		"refuses '$typed' in $field, says why and saves nothing",
		async ({ field, typed }) => {
			const { user } = renderReadiness();

			const threshold = screen.getByRole("spinbutton", { name: field });
			await user.clear(threshold);
			if (typed !== "") {
				await user.type(threshold, typed);
			}

			expect(
				screen.getByText("A discussion rule needs at least 1 vote"),
			).toBeVisible();
		},
	);

	it.each([
		{ refused: YES_VOTES_NEEDED, typed: "0", accepted: VOTERS_NEEDED },
		{ refused: VOTERS_NEEDED, typed: "2", accepted: YES_VOTES_NEEDED },
	])(
		"marks $refused as refused and leaves $accepted unmarked",
		async ({ refused, typed, accepted }) => {
			const { replaceTheNumberIn } = renderReadiness();

			await replaceTheNumberIn(refused, typed);

			expect(screen.getByRole("spinbutton", { name: refused })).toHaveAttribute(
				"aria-invalid",
				"true",
			);
			expect(
				screen.getByRole("spinbutton", { name: accepted }),
			).toHaveAttribute("aria-invalid", "false");
		},
	);

	it.each([
		{ refused: NO_THRESHOLD, accepted: YES_IF_THRESHOLD },
		{ refused: YES_IF_THRESHOLD, accepted: NO_THRESHOLD },
	])(
		"marks $refused as refused, describes it by why, and leaves $accepted unmarked",
		async ({ refused, accepted }) => {
			const { replaceTheNumberIn } = renderReadiness();

			await replaceTheNumberIn(refused, "0");

			const refusedField = screen.getByRole("spinbutton", { name: refused });
			expect(refusedField).toHaveAttribute("aria-invalid", "true");
			expect(refusedField).toHaveAccessibleDescription(DISCUSSION_RULE_ERROR);
			const acceptedField = screen.getByRole("spinbutton", { name: accepted });
			expect(acceptedField).toHaveAttribute("aria-invalid", "false");
			expect(acceptedField).not.toHaveAttribute("aria-describedby");
		},
	);

	it("does not let any of its numbers step below one vote", () => {
		renderReadiness();

		for (const name of [
			YES_VOTES_NEEDED,
			VOTERS_NEEDED,
			NO_THRESHOLD,
			YES_IF_THRESHOLD,
		]) {
			const field = screen.getByRole("spinbutton", { name });
			expect(field).toHaveAttribute("min", "1");
			expect(field).toHaveAttribute("step", "1");
		}
	});

	it("switches a rule that was refused when switched off back on at its last allowed threshold", async () => {
		const { user, latest, replaceTheNumberIn } = renderReadiness({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: 4, yesIf: 2 },
		});
		const noRule = screen.getByRole("checkbox", { name: NO_RULE });
		await user.click(noRule);
		expect(latest().discussWhen).toEqual({
			no: null,
			yesIf: 2,
		});
		await user.click(noRule);
		expect(latest().discussWhen).toEqual({
			no: 4,
			yesIf: 2,
		});

		await replaceTheNumberIn(NO_THRESHOLD, "0");
		await user.click(noRule);
		expect(latest().discussWhen).toEqual({
			no: null,
			yesIf: 2,
		});
		await user.click(noRule);

		expect(latest().discussWhen).toEqual({
			no: 4,
			yesIf: 2,
		});
	});

	it("switches on a No rule stored as off at its default threshold", async () => {
		const { user, latest } = renderReadiness({
			minYes: 3,
			minVoters: 3,
			discussWhen: { no: null, yesIf: 2 },
		});

		await user.click(screen.getByRole("checkbox", { name: NO_RULE }));

		expect(latest().discussWhen).toEqual({
			no: 1,
			yesIf: 2,
		});
	});
});
