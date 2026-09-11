import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
	UsageDataIndicator,
	type UsageDataSendingState,
} from "./UsageDataIndicator";

const renderIndicator = (
	state: UsageDataSendingState,
	onOpenDecision = vi.fn(),
) => {
	render(<UsageDataIndicator state={state} onOpenDecision={onOpenDecision} />);
	return { onOpenDecision };
};

describe.skip("UsageDataIndicator", () => {
	it("says it is sending, in words rather than in colour", () => {
		renderIndicator("sending");

		expect(
			screen.getByRole("button", { name: /sending usage data/i }),
		).toBeInTheDocument();
	});

	it("says it is not sending, in words rather than in colour", () => {
		renderIndicator("not-sending");

		expect(
			screen.getByRole("button", { name: /not sending usage data/i }),
		).toBeInTheDocument();
	});

	it("tells the two states apart by their accessible name, so the difference survives greyscale", () => {
		const { unmount } = render(
			<UsageDataIndicator state="sending" onOpenDecision={vi.fn()} />,
		);
		const sendingName = screen.getByRole("button").getAttribute("aria-label");
		unmount();

		render(<UsageDataIndicator state="not-sending" onOpenDecision={vi.fn()} />);
		const notSendingName = screen
			.getByRole("button")
			.getAttribute("aria-label");

		expect(sendingName).not.toBeNull();
		expect(sendingName).not.toEqual(notSendingName);
	});

	it("says nothing is being sent when it could not find out, because a privacy control fails closed", () => {
		renderIndicator("unknown");

		expect(
			screen.getByRole("button", { name: /not sending usage data/i }),
		).toBeInTheDocument();
	});

	it("is there whether or not anything is being sent, because absence answers nothing", () => {
		renderIndicator("not-sending");

		expect(screen.getByRole("button")).toBeVisible();
	});

	it("reopens the decision when it is clicked", async () => {
		const { onOpenDecision } = renderIndicator("sending");

		await userEvent.click(screen.getByRole("button"));

		expect(onOpenDecision).toHaveBeenCalledTimes(1);
	});

	it("reopens the decision from the not-sending state too, so a refusal can be changed", async () => {
		const { onOpenDecision } = renderIndicator("not-sending");

		await userEvent.click(screen.getByRole("button"));

		expect(onOpenDecision).toHaveBeenCalledTimes(1);
	});
});
