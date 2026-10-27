import { afterEach, describe, expect, it, vi } from "vitest";
import { isTauriOnMac } from "./tauri";

const MAC =
	"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15";
const LINUX = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/605.1.15";
const WINDOWS =
	"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/605.1.15";

const readingAs = (userAgent: string, inTauri: boolean) => {
	vi.spyOn(navigator, "userAgent", "get").mockReturnValue(userAgent);
	if (inTauri) {
		vi.stubGlobal("__TAURI_INTERNALS__", {});
	}
};

describe("isTauriOnMac", () => {
	afterEach(() => {
		vi.restoreAllMocks();
		vi.unstubAllGlobals();
	});

	it("is true with Tauri internals present and a Mac user agent", () => {
		readingAs(MAC, true);

		expect(isTauriOnMac()).toBe(true);
	});

	it.each([
		["a browser on a Mac", MAC, false],
		["Tauri on Linux", LINUX, true],
		["Tauri on Windows", WINDOWS, true],
		["a browser on Linux", LINUX, false],
	])("is false in %s", (_name, userAgent, inTauri) => {
		readingAs(userAgent, inTauri);

		expect(isTauriOnMac()).toBe(false);
	});
});
