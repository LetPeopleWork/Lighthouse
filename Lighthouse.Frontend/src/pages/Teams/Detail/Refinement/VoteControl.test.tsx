import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import VoteControl from "./VoteControl";

describe("a row's answers", () => {
	it.each([
		["Yes", "Yes"],
		["Yes, if…", "YesBut"],
		["No", "No"],
	])("casts '%s' as %s", async (label, answer) => {
		const onVote = vi.fn();
		render(
			<VoteControl
				referenceId="GR-073"
				myVote={null}
				isSending={false}
				onVote={onVote}
				onTakeBack={vi.fn()}
			/>,
		);

		await userEvent.click(screen.getByRole("button", { name: label }));

		expect(onVote).toHaveBeenCalledExactlyOnceWith(answer);
	});

	it("casts nothing when the voter clicks the answer they already gave", async () => {
		const onVote = vi.fn();
		render(
			<VoteControl
				referenceId="GR-073"
				myVote="YesBut"
				isSending={false}
				onVote={onVote}
				onTakeBack={vi.fn()}
			/>,
		);

		await userEvent.click(screen.getByRole("button", { name: "Yes, if…" }));

		expect(onVote).not.toHaveBeenCalled();
	});

	it.each([
		["Yes", "Yes"],
		["Yes, if…", "YesBut"],
		["No", "No"],
	] as const)(
		"takes back the '%s' the voter clicks again, naming it",
		async (label, answer) => {
			const onTakeBack = vi.fn();
			render(
				<VoteControl
					referenceId="GR-073"
					myVote={answer}
					isSending={false}
					onVote={vi.fn()}
					onTakeBack={onTakeBack}
				/>,
			);

			await userEvent.click(screen.getByRole("button", { name: label }));

			expect(onTakeBack).toHaveBeenCalledExactlyOnceWith(answer);
		},
	);
});

describe("the take-back hint", () => {
	const CLICK_AGAIN = "Click again to take back your vote";

	it("sits on the voter's own answer and on no other", () => {
		render(
			<VoteControl
				referenceId="GR-073"
				myVote="Yes"
				isSending={false}
				onVote={vi.fn()}
				onTakeBack={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Yes" }),
		).toHaveAccessibleDescription(CLICK_AGAIN);
		expect(
			screen.getByRole("button", { name: "Yes, if…" }),
		).not.toHaveAccessibleDescription();
		expect(
			screen.getByRole("button", { name: "No" }),
		).not.toHaveAccessibleDescription();
	});

	it("is not offered while the vote is being sent", () => {
		render(
			<VoteControl
				referenceId="GR-073"
				myVote="Yes"
				isSending
				onVote={vi.fn()}
				onTakeBack={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Yes" }),
		).not.toHaveAccessibleDescription();
	});
});
