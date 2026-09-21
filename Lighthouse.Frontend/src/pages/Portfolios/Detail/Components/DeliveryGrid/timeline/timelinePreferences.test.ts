import { beforeEach, describe, expect, it } from "vitest";
import { showTeamsStore } from "./timelinePreferences";

// The preferences this chart actually keeps, as opposed to how a preference works - which is
// `pagePreference.test.ts`. What is left here is the part that is specific to each one: the key it
// is remembered under, and what a reader who has never answered is shown.

beforeEach(() => {
	localStorage.clear();
	showTeamsStore.forget();
});

describe("the Delivery timeline's preferences", () => {
	it("shows the Teams to nobody who has not asked", () => {
		expect(showTeamsStore.read()).toBe(false);
	});

	it("remembers the Teams under the key the shipped version used", () => {
		// Pinned against the literal, because a renamed key forgets every reader's existing choice
		// while every round trip through the store keeps passing. This one has shipped, so the
		// forgetting would happen to people rather than to a fixture.
		showTeamsStore.set(true);

		expect(localStorage.getItem("lighthouse:deliveryTimeline:showTeams")).toBe(
			"true",
		);
	});
});
