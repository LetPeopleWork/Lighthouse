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

	// AC-04.6, pinned against literals below rather than by comparing two renders to each other -
	// two equally wrong but distinct sentences would satisfy that, and it would teach the next reader
	// that difference-testing is enough here.
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

	// The label comes from a fetched list, and an empty list is exactly what a connection that has
	// stopped offering the source produces - so the cause guaranteed to arrive without a label is the
	// one whose sentence names the connection. Falling back to the stored key would print
	// "no longer offers the jira-release" at somebody who never types keys.
	it.each([
		"SourceNotFound",
		"SourceHasNoDate",
		"CapabilityWithdrawn",
	] as const)(
		"says something a person would say when %s has no label",
		(reason) => {
			renderNotice({ reason, sourceLabel: "" });

			expect(screen.getByText(/the source this follows/i)).toBeInTheDocument();
		},
	);

	// AC-04.2. A date with no "since when" beside it is indistinguishable from a live one, which is
	// the whole failure this state exists to remove.
	//
	// The instant is late in the UTC day on purpose: this suite runs in Europe/Zurich, where 23:30Z is
	// already the next day locally. Asserting the UTC day AND the absence of the local one is what
	// makes this fail if the component ever reads the day off the viewer's clock — recomputing
	// production's own expression and comparing it to itself could not see that.
	it("says the day the source was last read, in the day the rest of the screen uses", () => {
		renderNotice({ lastSyncedOn: "2026-08-20T23:30:00.000Z" });

		const theUtcDay = new Date("2026-08-20T12:00:00.000Z").toLocaleDateString(
			undefined,
			{ timeZone: "UTC" },
		);
		const theDayItIsAlreadyLocally = new Date(
			"2026-08-21T12:00:00.000Z",
		).toLocaleDateString(undefined, { timeZone: "UTC" });

		const notice = screen.getByText(/Showing the values it last gave/);

		expect(notice.textContent).toContain(theUtcDay);
		expect(notice.textContent).not.toContain(theDayItIsAlreadyLocally);
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
