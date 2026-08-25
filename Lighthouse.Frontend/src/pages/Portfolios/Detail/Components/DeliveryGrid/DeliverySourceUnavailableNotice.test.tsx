import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DeliverySourceUnavailableNotice from "./DeliverySourceUnavailableNotice";

/**
 * The notice a Delivery shows once the source it follows has stopped answering for good. What it has
 * to get right is that the values on the row beside it are real - they were true when they were last
 * read - so the sentence is about who is maintaining them now, and since when.
 */

const A_JIRA_RELEASE = "Jira Release";
const LAST_READ = "2026-08-20T00:00:00.000Z";

function renderNotice(
	overrides: Partial<
		React.ComponentProps<typeof DeliverySourceUnavailableNotice>
	> = {},
) {
	return render(
		<DeliverySourceUnavailableNotice
			reason="SourceNotFound"
			sourceLabel={A_JIRA_RELEASE}
			lastSyncedOn={LAST_READ}
			{...overrides}
		/>,
	);
}

describe("DeliverySourceUnavailableNotice (US-04)", () => {
	it("says the source is unavailable", () => {
		renderNotice();

		expect(screen.getByText("Source unavailable")).toBeInTheDocument();
	});

	// AC-04.6. A Release that lost its date is sitting right there and can be put right in a minute;
	// telling its owner it no longer exists sends them looking for something that is not missing.
	it("tells a Release that is gone apart from one that has merely lost its date", () => {
		const { unmount } = renderNotice({ reason: "SourceNotFound" });
		const whenDeleted = screen.getByText(/no longer/).textContent;
		unmount();

		renderNotice({ reason: "SourceHasNoDate" });

		expect(screen.getByText(/no longer/).textContent).not.toBe(whenDeleted);
	});

	it.each([
		["SourceNotFound", /no longer exists/],
		["SourceHasNoDate", /no longer has a date/],
		["CapabilityWithdrawn", /no longer offers/],
	] as const)("names %s as its own cause", (reason, expected) => {
		renderNotice({ reason });

		expect(screen.getByText(expected)).toBeInTheDocument();
	});

	it("names the source the way the connection names it", () => {
		renderNotice({ sourceLabel: "Jira Fix Version" });

		expect(screen.getByText(/Jira Fix Version/)).toBeInTheDocument();
	});

	// AC-04.2. A date with no "since when" beside it is indistinguishable from a live one, which is
	// the whole failure this state exists to remove.
	it("says when the source was last read successfully", () => {
		renderNotice();

		const asItIsPrinted = new Date(LAST_READ).toLocaleDateString(undefined, {
			timeZone: "UTC",
		});

		expect(
			screen.getByText(new RegExp(`from ${asItIsPrinted}`)),
		).toBeInTheDocument();
	});

	// A Delivery bound to a Release that was already broken when it was created has values somebody
	// typed, not values a source gave it. Printing a date there would name one that never happened.
	it("says so plainly when the source was never read successfully at all", () => {
		renderNotice({ lastSyncedOn: null });

		expect(
			screen.getByText(/not been read successfully since it was set up/),
		).toBeInTheDocument();
	});

	// AC-04.3. The way out is offered where the problem is reported.
	it("offers the way out to somebody who may take it", async () => {
		const stopFollowing = vi.fn();
		renderNotice({ onUnbind: stopFollowing });

		await userEvent.click(
			screen.getByRole("button", { name: /stop following/i }),
		);

		expect(stopFollowing).toHaveBeenCalledOnce();
	});

	it("offers nothing to press to somebody who may only look", () => {
		renderNotice({ onUnbind: undefined });

		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});
});
