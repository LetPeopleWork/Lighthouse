import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ConditionPrompt from "./ConditionPrompt";

const WORK_ITEM = "GR-051 Advanced reporting module";
const CONDITION = "only if the PDF export moves to its own Work Item";

const renderThePrompt = () => {
	const onCancel = vi.fn();
	const onConfirm = vi.fn();
	const user = userEvent.setup();
	render(
		<ConditionPrompt
			workItem={WORK_ITEM}
			onCancel={onCancel}
			onConfirm={onConfirm}
		/>,
	);
	return { user, onCancel, onConfirm };
};

const theCondition = () => screen.getByRole("textbox", { name: "Condition" });
const theVoteButton = () => screen.getByRole("button", { name: "Vote" });

describe("asking for the condition of a Yes, if…", () => {
	it("names the Work Item and asks what has to be true, with room for 2000 characters", () => {
		renderThePrompt();

		expect(screen.getByRole("dialog", { name: "Yes, if…" })).toHaveTextContent(
			WORK_ITEM,
		);
		expect(theCondition()).toHaveAttribute(
			"placeholder",
			"What has to be true for a Yes?",
		);
		expect(theCondition()).toHaveAttribute("maxlength", "2000");
	});

	it("confirms a padded pasted condition once, trimmed", async () => {
		const { user, onConfirm, onCancel } = renderThePrompt();

		await user.click(theCondition());
		await user.paste(`  ${CONDITION}  `);
		await user.click(theVoteButton());

		expect(onConfirm).toHaveBeenCalledExactlyOnceWith(CONDITION);
		expect(onCancel).not.toHaveBeenCalled();
	});

	it("keeps Vote disabled for a blank condition and confirms nothing", async () => {
		const { user, onConfirm } = renderThePrompt();

		expect(theVoteButton()).toBeDisabled();
		await user.type(theCondition(), "   ");
		expect(theVoteButton()).toBeDisabled();
		fireEvent.click(theVoteButton());

		expect(onConfirm).not.toHaveBeenCalled();
	});

	it("calls only onCancel when it is cancelled, even with a condition typed", async () => {
		const { user, onConfirm, onCancel } = renderThePrompt();

		await user.type(theCondition(), CONDITION);
		await user.click(screen.getByRole("button", { name: "Cancel" }));

		expect(onCancel).toHaveBeenCalledOnce();
		expect(onConfirm).not.toHaveBeenCalled();
	});
});
