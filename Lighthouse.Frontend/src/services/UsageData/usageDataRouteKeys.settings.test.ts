import { describe, expect, it } from "vitest";
import { usageDataPageOpeningFor } from "./usageDataRouteKeys";

describe("the settings and connection pages are not counted as a page somebody opened", () => {
	it.each([
		"/settings/system/connections",
		"/settings/system/connections/new",
		"/settings/system/connections/4/edit",
	])("%s raises no page opening", (pathname) => {
		expect(usageDataPageOpeningFor(pathname)).toBeUndefined();
	});
});
