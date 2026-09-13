import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { UsageDataRouteKey } from "../../models/UsageData/UsageData";
import { UsageDataEventName } from "../Api/UsageDataService";
import {
	usageDataPageOpeningFor,
	usageDataRouteKeyFor,
} from "./usageDataRouteKeys";

const here = dirname(fileURLToPath(import.meta.url));

// The server's list and the browser's list are the same list written twice, and two lists that
// disagree are invisible to either stack's own tests: the browser would keep answering with a name
// the server refuses, and nothing on this side would notice. So the server's copy is read here as
// source text and the expectations below are derived from it rather than typed out again.
const backendEnum = readFileSync(
	resolve(
		here,
		"../../../../Lighthouse.Backend/Lighthouse.Backend/Models/UsageData/UsageDataRouteKey.cs",
	),
	"utf8",
);

const backendAddresses = readFileSync(
	resolve(
		here,
		"../../../../Lighthouse.Backend/Lighthouse.Backend/Models/UsageData/UsageDataRoutePatterns.cs",
	),
	"utf8",
);

const moduleSource = readFileSync(
	resolve(here, "./usageDataRouteKeys.ts"),
	"utf8",
);

const serverMembers = [
	...backendEnum.matchAll(/^\s*(\w+)\s*=\s*\d+\s*,/gm),
].map((match) => match[1]);

const everyPageWithAKey = [
	...backendAddresses.matchAll(/\[UsageDataRouteKey\.(\w+)\]\s*=\s*"([^"]+)"/g),
].map((match) => [match[2], match[1]] as const);

const aTeamSomebodyOwns = "42";

describe("a page somebody opened becomes a key", () => {
	it("answers the Team metrics page with the member that names it", () => {
		expect(usageDataRouteKeyFor("/teams/42/metrics")).toBe(
			UsageDataRouteKey.TeamDetail_Metrics,
		);
	});

	it("answers a Portfolio tab with a member of its own", () => {
		expect(usageDataRouteKeyFor("/portfolios/7/deliveries")).toBe(
			UsageDataRouteKey.PortfolioDetail_Deliveries,
		);
	});

	it.each(everyPageWithAKey)(
		"answers every page the server publishes an address for: %s",
		(pattern, member) => {
			expect(
				usageDataRouteKeyFor(pattern.replace(":id", aTeamSomebodyOwns)),
			).toBe(member);
		},
	);
});

describe("the identifier in the address stays in the browser", () => {
	it.each(["1", "42", "9007199254740991", "acme-platform-team"])(
		"gives the same answer whichever Team is open: %s",
		(identifier) => {
			expect(usageDataRouteKeyFor(`/teams/${identifier}/metrics`)).toBe(
				UsageDataRouteKey.TeamDetail_Metrics,
			);
		},
	);

	it("gives back a member of the list and nothing that could hold an identifier", () => {
		const answer = usageDataRouteKeyFor("/teams/42/metrics");

		expect(Object.values(UsageDataRouteKey)).toContain(answer);
		expect(answer).not.toMatch(/\d/);
	});
});

describe("an address naming no tab is the first tab of that page", () => {
	// Most links into these pages are this shape - the Details button on the overview, and every
	// place a Team's name is a link - so answering them with nothing would leave the tab people
	// arrive on uncounted while counting every tab they moved to afterwards.
	it.each([
		["/teams/42", UsageDataRouteKey.TeamDetail_Features],
		["/portfolios/7", UsageDataRouteKey.PortfolioDetail_Features],
	])("answers %s with the first tab", (pathname, expected) => {
		expect(usageDataRouteKeyFor(pathname)).toBe(expected);
	});

	// The page for creating one is that same shape. Reading it as a page opening would count a Team
	// nobody has - and would do it on the one screen where no Team exists to be opened.
	it.each(["/teams/new", "/portfolios/new"])(
		"does not read %s as a page somebody opened",
		(pathname) => {
			expect(usageDataRouteKeyFor(pathname)).toBeUndefined();
		},
	);
});

describe("a page with no key is answered with nothing, never with a stand-in", () => {
	it.each([
		"/",
		"/features",
		"/settings",
		"/oauth/popup-complete",
		"/connections/new",
		"/connections/5/edit",
		"/teams",
		"/teams/new",
		"/teams/edit/5",
		"/portfolios",
		"/portfolios/new",
		"/portfolios/edit/5",
		// A tab that belongs to the other kind of page. Two families share a shape, and a map keyed
		// on the tab alone would answer both with the same member.
		"/teams/42/deliveries",
		"/portfolios/7/forecasts",
		"/teams/42/metrics/something-else",
	])("answers %s with nothing", (pathname) => {
		expect(usageDataRouteKeyFor(pathname)).toBeUndefined();
	});
});

describe("an opening's name and its page never disagree", () => {
	// The server refuses a message where they do, so this is the guard that keeps one from being
	// sent in the first place. Both are read off one entry, and this is what says so: every address
	// this product has for a Team is answered with the Team opening, and likewise for Portfolios.
	it.each([
		["/teams/42", UsageDataEventName.TeamTabOpened],
		["/teams/42/features", UsageDataEventName.TeamTabOpened],
		["/teams/42/forecasts", UsageDataEventName.TeamTabOpened],
		["/teams/42/metrics", UsageDataEventName.TeamTabOpened],
		["/teams/42/settings", UsageDataEventName.TeamTabOpened],
		["/teams/42/access", UsageDataEventName.TeamTabOpened],
		["/portfolios/7", UsageDataEventName.PortfolioTabOpened],
		["/portfolios/7/features", UsageDataEventName.PortfolioTabOpened],
		["/portfolios/7/metrics", UsageDataEventName.PortfolioTabOpened],
		["/portfolios/7/deliveries", UsageDataEventName.PortfolioTabOpened],
		["/portfolios/7/settings", UsageDataEventName.PortfolioTabOpened],
		["/portfolios/7/access", UsageDataEventName.PortfolioTabOpened],
	])("answers %s with %s and a page of that kind", (pathname, name) => {
		const opening = usageDataPageOpeningFor(pathname);

		expect(opening?.name).toBe(name);
		expect(opening?.route).toBe(usageDataRouteKeyFor(pathname));
	});

	it("answers a page it has no name for with nothing at all", () => {
		expect(usageDataPageOpeningFor("/settings")).toBeUndefined();
	});
});

describe("the browser list and the server list name the same members", () => {
	it("has a counterpart here for every member the server declares, and none besides", () => {
		expect(Object.keys(UsageDataRouteKey).sort()).toEqual(
			[...serverMembers].sort(),
		);
	});

	// Numbering the members here instead would compare false against every response: these names
	// travel as text in both directions.
	it("carries those names as their own values", () => {
		expect(Object.values(UsageDataRouteKey).sort()).toEqual(
			[...serverMembers].sort(),
		);
	});
});

describe("the module only answers the question it was asked", () => {
	it("gives an answer that does not depend on what was asked before it", () => {
		const askedOnItsOwn = usageDataRouteKeyFor("/teams/42/metrics");

		usageDataRouteKeyFor("/portfolios/7/access");
		usageDataRouteKeyFor("/nowhere-in-particular");

		expect(usageDataRouteKeyFor("/teams/42/metrics")).toBe(askedOnItsOwn);
	});

	// Reading the address off the browser instead of being handed it is what turns this into a
	// general-purpose tracker, and it is a one-line change away at all times. So this reads the
	// module as text: a reviewer cannot be the only thing standing between the two.
	//
	// Mutation testing rewrites this file where it sits and its own scaffolding mentions
	// globalThis, so under it the text on disk is no longer the text we wrote. The check stands
	// down rather than reporting on somebody else's code; every ordinary run still makes it.
	it
		.runIf(!moduleSource.includes("stryNS_"))
		.each([
			"window.",
			"globalThis.",
			"location",
			"localStorage",
			"sessionStorage",
			"document.",
			"fetch(",
		])("never reaches for %s", (forbidden) => {
		expect(moduleSource).not.toContain(forbidden);
	});
});
