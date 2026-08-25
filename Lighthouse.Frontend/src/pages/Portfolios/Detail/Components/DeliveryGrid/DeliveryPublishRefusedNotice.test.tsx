import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import DeliveryPublishRefusedNotice from "./DeliveryPublishRefusedNotice";

/**
 * The notice a Delivery shows when the source would not take its forecast. It exists because the
 * alternative is a switch somebody turned on that appears to do nothing at all: the Release simply
 * never changes, and nothing anywhere says why.
 *
 * What it has to get right is that this Delivery is fine. Everything on the row beside it is current;
 * one optional thing it does somewhere else did not happen.
 */

const A_JIRA_RELEASE = "Jira Release";
const WHAT_JIRA_SAID =
	"You must have global or project administrator rights in order to modify versions.";
const REFUSED_ON = "2026-08-25T00:00:00.000Z";

function renderNotice(
	overrides: Partial<
		React.ComponentProps<typeof DeliveryPublishRefusedNotice>
	> = {},
) {
	return render(
		<DeliveryPublishRefusedNotice
			reason={WHAT_JIRA_SAID}
			sourceLabel={A_JIRA_RELEASE}
			refusedOn={REFUSED_ON}
			{...overrides}
		/>,
	);
}

describe("DeliveryPublishRefusedNotice (US-06)", () => {
	it("says the forecast was not published", () => {
		renderNotice();

		expect(screen.getByText("Forecast not published")).toBeInTheDocument();
	});

	/**
	 * AC-06.1. Quoted rather than paraphrased: the remote's own sentence names what to fix in the
	 * words the reader will search for, and a Lighthouse rewording loses exactly that.
	 */
	it("quotes what the source said, word for word", () => {
		renderNotice();

		expect(screen.getByText(WHAT_JIRA_SAID)).toBeInTheDocument();
	});

	it("names the source the way the connection names it", () => {
		renderNotice({ sourceLabel: "Jira Fix Version" });

		expect(screen.getByText(/Jira Fix Version/)).toBeInTheDocument();
	});

	/**
	 * The label comes from a fetched list and is not always there. Falling back to the stored key
	 * would print "the jira-release" at somebody who never types keys.
	 */
	it("still reads as a sentence when nobody could name the source", () => {
		renderNotice({ sourceLabel: "" });

		expect(
			screen.getByText(/could not be written to the source this follows/),
		).toBeInTheDocument();
	});

	// AC-06.1 again: an administrator has to know whether this is happening now or happened once,
	// months ago, before something else was fixed.
	it("says when it was last tried", () => {
		renderNotice();

		expect(screen.getByText(/last tried on/)).toBeInTheDocument();
	});

	it("says nothing about a day nobody recorded", () => {
		renderNotice({ refusedOn: null });

		expect(screen.queryByText(/last tried on/)).not.toBeInTheDocument();
	});

	/**
	 * The half that stops this reading as a broken Delivery. Everything on the row beside it was read
	 * from the source in the ordinary way and is current; the refusal is about a write, not a read.
	 */
	it("says the rest of the delivery is fine", () => {
		renderNotice();

		expect(
			screen.getByText(/Everything else about this delivery is up to date/),
		).toBeInTheDocument();
	});
});
