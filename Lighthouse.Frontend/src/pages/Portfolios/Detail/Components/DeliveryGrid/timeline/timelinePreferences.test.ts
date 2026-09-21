import { beforeEach, describe, expect, it } from "vitest";
import {
	showStatusStore,
	showTeamsStore,
	showWarningsStore,
} from "./timelinePreferences";

// The preferences this chart actually keeps, as opposed to how a preference works - which is
// `pagePreference.test.ts`. What is left here is the part that is specific to each one: the key it
// is remembered under, and what a reader who has never answered is shown.

const everyPreference = [
	{
		name: "Teams",
		store: showTeamsStore,
		key: "lighthouse:deliveryTimeline:showTeams",
	},
	{
		name: "status",
		store: showStatusStore,
		key: "lighthouse:deliveryTimeline:showStatus",
	},
	{
		name: "warnings",
		store: showWarningsStore,
		key: "lighthouse:deliveryTimeline:showWarnings",
	},
];

beforeEach(() => {
	localStorage.clear();

	for (const { store } of everyPreference) {
		store.forget();
	}
});

describe("the Delivery timeline's preferences", () => {
	it.each(everyPreference)(
		"shows the $name to nobody who has not asked",
		({ store }) => {
			expect(store.read()).toBe(false);
		},
	);

	it.each(everyPreference)(
		"remembers the $name under a key of its own",
		({ store, key }) => {
			// Pinned against the literal, because a renamed key forgets every reader's existing
			// choice while every round trip through the store keeps passing. The Teams key has
			// shipped, so for that one the forgetting would happen to people rather than to a
			// fixture.
			store.set(true);

			expect(localStorage.getItem(key)).toBe("true");
		},
	);

	it("keeps all three apart", () => {
		// Three keys, three answers. Two preferences sharing a key would look like one switch
		// dragging another with it, which reads as a decision rather than as a fault.
		showStatusStore.set(true);

		expect(showStatusStore.read()).toBe(true);
		expect(showTeamsStore.read()).toBe(false);
		expect(showWarningsStore.read()).toBe(false);
	});
});
