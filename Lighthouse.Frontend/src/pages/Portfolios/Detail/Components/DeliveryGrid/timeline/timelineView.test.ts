import { beforeEach, describe, expect, it } from "vitest";
import { timelineViewStore } from "./timelineView";

// The one choice this chart keeps, as opposed to how a choice works - which is `pageChoice.test.ts`.
// What is left here is the part specific to this one: the key it is remembered under, and what a
// reader who has never answered is shown.

beforeEach(() => {
	localStorage.clear();
	timelineViewStore.forget();
});

describe("what the Delivery timeline shows", () => {
	it("shows the status to a reader who has never chosen", () => {
		// The default is not "nothing". Status is the only one of the four whose answer a reader
		// cannot get anywhere else in the product - the Teams and the warnings are both on the
		// Feature table - so it is the one worth spending a first impression on.
		expect(timelineViewStore.read()).toBe("status");
	});

	it("remembers the choice under a key of its own", () => {
		// Pinned against the literal, because a renamed key forgets every reader's choice while
		// every round trip through the store keeps passing.
		timelineViewStore.set("teams");

		expect(localStorage.getItem("lighthouse:deliveryTimeline:view")).toBe(
			"teams",
		);
	});

	it("takes nothing for an answer", () => {
		// "Nothing" is a choice like any other and has to survive a reload, or a reader who has
		// asked for a plain chart gets the status back every morning.
		timelineViewStore.set("none");
		timelineViewStore.forget();

		expect(timelineViewStore.read()).toBe("none");
	});
});
