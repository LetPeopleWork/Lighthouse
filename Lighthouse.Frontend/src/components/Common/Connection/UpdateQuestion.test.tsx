import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import UpdateQuestion, {
	type WhatChanges,
	type WhyUpdate,
} from "./UpdateQuestion";

const { words } = vi.hoisted(() => ({
	words: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => words.current[key] ?? key,
	}),
}));

const TheDefaultWords = {
	team: "Team",
	teams: "Teams",
	portfolio: "Portfolio",
	portfolios: "Portfolios",
};

const SquadsAndPrograms = {
	team: "Squad",
	teams: "Squads",
	portfolio: "Program",
	portfolios: "Programs",
};

const UntilUpdated =
	"Their parents change only once they have been updated. Save only: they change at their next scheduled update.";

const UntilDependenciesUpdated =
	"Their dependencies change only once they have been updated. Save only: they change at their next scheduled update.";

const UntilWhatTheyReadUpdated =
	"What they read changes only once they have been updated. Save only: they change at their next scheduled update.";

const askBecause = (why: WhyUpdate) => {
	const answers = {
		onUpdate: vi.fn(async () => {}),
		onSaveOnly: vi.fn(async () => {}),
		onCancel: vi.fn(),
	};
	render(<UpdateQuestion open why={why} {...answers} />);
	return answers;
};

const theDefaultsChanged = (
	teams: number,
	portfolios: number,
	defaults = ["Parent Override Field"],
	changes: WhatChanges[] = ["parents"],
): WhyUpdate => ({ because: "defaults", teams, portfolios, defaults, changes });

describe("UpdateQuestion", () => {
	beforeEach(() => {
		words.current = TheDefaultWords;
	});

	describe("when a followed default changed", () => {
		it.each([
			{
				case: "one Team",
				terms: TheDefaultWords,
				why: theDefaultsChanged(1, 0),
				title: "Update 1 Team and 0 Portfolios?",
				says: `1 Team follows the Parent Override Field default. ${UntilUpdated}`,
			},
			{
				case: "one Portfolio",
				terms: TheDefaultWords,
				why: theDefaultsChanged(0, 1),
				title: "Update 0 Teams and 1 Portfolio?",
				says: `1 Portfolio follows the Parent Override Field default. ${UntilUpdated}`,
			},
			{
				case: "Teams and Portfolios",
				terms: TheDefaultWords,
				why: theDefaultsChanged(3, 2),
				title: "Update 3 Teams and 2 Portfolios?",
				says: `3 Teams and 2 Portfolios follow the Parent Override Field default. ${UntilUpdated}`,
			},
			{
				case: "the Dependency Field default",
				terms: TheDefaultWords,
				why: theDefaultsChanged(0, 2, ["Dependency Field"], ["dependencies"]),
				title: "Update 0 Teams and 2 Portfolios?",
				says: `2 Portfolios follow the Dependency Field default. ${UntilDependenciesUpdated}`,
			},
			{
				case: "two defaults",
				terms: TheDefaultWords,
				why: theDefaultsChanged(
					0,
					2,
					["Parent Override Field", "Dependency Field"],
					["parents", "dependencies"],
				),
				title: "Update 0 Teams and 2 Portfolios?",
				says: `2 Portfolios follow the Parent Override Field and Dependency Field defaults. ${UntilWhatTheyReadUpdated}`,
			},
			{
				case: "the Teams' and the Portfolios' Parent Override defaults",
				terms: TheDefaultWords,
				why: theDefaultsChanged(1, 2, ["Parent Override Field"], ["parents"]),
				title: "Update 1 Team and 2 Portfolios?",
				says: `1 Team and 2 Portfolios follow the Parent Override Field default. ${UntilUpdated}`,
			},
			{
				case: "renamed Teams and Portfolios",
				terms: SquadsAndPrograms,
				why: theDefaultsChanged(1, 2),
				title: "Update 1 Squad and 2 Programs?",
				says: `1 Squad and 2 Programs follow the Parent Override Field default. ${UntilUpdated}`,
			},
		])("$case: asks $title", ({ terms, why, title, says }) => {
			words.current = terms;

			askBecause(why);

			expect(
				screen.getByRole("dialog", { name: title }),
			).toHaveAccessibleDescription(says);
		});
	});

	describe("its answers", () => {
		it.each([
			{ answer: "Cancel", called: "onCancel" },
			{ answer: "Save only", called: "onSaveOnly" },
			{ answer: "Save and update", called: "onUpdate" },
		] as const)("$answer calls $called alone", async ({ answer, called }) => {
			const answers = askBecause(theDefaultsChanged(3, 0));

			fireEvent.click(
				screen.getByRole("button", { name: new RegExp(`^${answer}$`) }),
			);

			await waitFor(() => expect(answers[called]).toHaveBeenCalledTimes(1));
			for (const [name, callback] of Object.entries(answers)) {
				if (name !== called) {
					expect(callback).not.toHaveBeenCalled();
				}
			}
		});
	});
});
