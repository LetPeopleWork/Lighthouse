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
			/>,
		);

		await userEvent.click(screen.getByRole("button", { name: "Yes, if…" }));

		expect(onVote).not.toHaveBeenCalled();
	});
});
