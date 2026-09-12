import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
	UsageDataIndicator,
	type UsageDataSendingState,
} from "./UsageDataIndicator";

// The journey fixes this copy verbatim (US-01 elevator pitch). Asserting the exact strings rather
// than a loose pattern is deliberate: RTL matches a regex unanchored against the whole accessible
// name, so /sending usage data/i ALSO matches "not sending usage data" — the positive and negative
// cases would both pass against the same wrong render.
const SENDING_LABEL = "Usage data: being sent from this browser";
const NOT_SENDING_LABEL = "Usage data: not being sent";

const renderIndicator = (
	state: UsageDataSendingState,
	onOpenDecision = vi.fn(),
) => {
	render(<UsageDataIndicator state={state} onOpenDecision={onOpenDecision} />);
	return { onOpenDecision, button: screen.getByRole("button") };
};

describe("UsageDataIndicator", () => {
	it("says it is sending, in words rather than as a symbol to interpret", () => {
		const { button } = renderIndicator("sending");

		expect(button).toHaveAccessibleName(SENDING_LABEL);
	});

	it("says it is not sending, in words rather than as a symbol to interpret", () => {
		const { button } = renderIndicator("not-sending");

		expect(button).toHaveAccessibleName(NOT_SENDING_LABEL);
	});

	it("never reads as sending when it is not, and never as not-sending when it is", () => {
		const { unmount } = render(
			<UsageDataIndicator state="sending" onOpenDecision={vi.fn()} />,
		);
		expect(screen.getByRole("button")).not.toHaveAccessibleName(
			NOT_SENDING_LABEL,
		);
		unmount();

		render(<UsageDataIndicator state="not-sending" onOpenDecision={vi.fn()} />);
		expect(screen.getByRole("button")).not.toHaveAccessibleName(SENDING_LABEL);
	});

	it("tells the two states apart by their accessible name, so the difference survives greyscale", () => {
		const { unmount } = render(
			<UsageDataIndicator state="sending" onOpenDecision={vi.fn()} />,
		);
		expect(screen.getByRole("button")).toHaveAccessibleName(SENDING_LABEL);
		unmount();

		render(<UsageDataIndicator state="not-sending" onOpenDecision={vi.fn()} />);
		expect(screen.getByRole("button")).toHaveAccessibleName(NOT_SENDING_LABEL);
	});

	it("says nothing is being sent when it could not find out, because a privacy control fails closed", () => {
		const { button } = renderIndicator("unknown");

		expect(button).toHaveAccessibleName(NOT_SENDING_LABEL);
	});

	it.each<UsageDataSendingState>(["sending", "not-sending", "unknown"])(
		"is there in the %s state too, because an indicator that only appears sometimes answers nothing",
		(state) => {
			const { button } = renderIndicator(state);

			expect(button).toBeVisible();
		},
	);

	it("reopens the decision when it is clicked", async () => {
		const { onOpenDecision, button } = renderIndicator("sending");

		await userEvent.click(button);

		expect(onOpenDecision).toHaveBeenCalledTimes(1);
	});

	it("reopens the decision from the not-sending state too, so a refusal can be changed", async () => {
		const { onOpenDecision, button } = renderIndicator("not-sending");

		await userEvent.click(button);

		expect(onOpenDecision).toHaveBeenCalledTimes(1);
	});
});
