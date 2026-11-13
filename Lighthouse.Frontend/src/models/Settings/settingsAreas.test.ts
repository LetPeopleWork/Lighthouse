import { describe, expect, it } from "vitest";
import { isSettingsPath, tabByAddress } from "./settingsAreas";

describe("isSettingsPath", () => {
	it.each([
		["/settings", true],
		["/settings/system", true],
		["/settings/system/connections", true],
		["/settingsandmore", false],
		["/overview", false],
		["/teams/settings/", false],
	])("says %s is a settings page: %s", (pathname, expected) => {
		expect(isSettingsPath(pathname)).toBe(expected);
	});
});

describe("tabByAddress", () => {
	it("finds a tab by its current slug", () => {
		expect(tabByAddress("organization", "calendar")).toEqual({
			tab: expect.objectContaining({ slug: "calendar" }),
			isEarlierSlug: false,
		});
	});

	it("finds a tab by an earlier slug and says so", () => {
		expect(tabByAddress("organization", "configuration")).toEqual({
			tab: expect.objectContaining({ slug: "terminology" }),
			isEarlierSlug: true,
		});
	});

	it("finds no tab of another area, even when its slug is known", () => {
		expect(tabByAddress("organization", "api-keys")).toBeUndefined();
		expect(tabByAddress("organization", "system-admins")).toBeUndefined();
	});

	it("finds no tab when no slug is given", () => {
		expect(tabByAddress("access", undefined)).toBeUndefined();
	});
});
