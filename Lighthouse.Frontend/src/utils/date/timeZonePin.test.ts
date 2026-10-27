import { describe, expect, it } from "vitest";

describe("test time zone", () => {
	// At UTC a day key read through UTC and the same key read locally agree, so every date test
	// would pass without noticing the bug it is there to catch. The pin is set on the pnpm
	// script (`TZ=Europe/Zurich`); a runner that skips the script leaves it inert and fails here.
	it("runs in Europe/Zurich, where a UTC day key and a local one differ", () => {
		expect(Intl.DateTimeFormat().resolvedOptions().timeZone).toBe(
			"Europe/Zurich",
		);
	});
});
