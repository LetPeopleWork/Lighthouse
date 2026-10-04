import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { readStoredVoter, readVoterKey, rememberVoter } from "./voterStore";

const VOTER_STORAGE_KEY = "lighthouse:refinement:voter";
const STORED_KEY = "c".repeat(64);
const A_HEX_KEY = /^[\da-f]{64}$/;

const storeRaw = (value: string) =>
	localStorage.setItem(VOTER_STORAGE_KEY, value);

describe("the voter this browser keeps", () => {
	beforeEach(() => {
		localStorage.clear();
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	it("reads the name and key it stored", () => {
		storeRaw(JSON.stringify({ name: "Jonas", key: STORED_KEY }));

		expect(readStoredVoter()).toEqual({ name: "Jonas", key: STORED_KEY });
		expect(readVoterKey()).toBe(STORED_KEY);
	});

	it.each([
		["nothing stored", null],
		["text that is not JSON", "{not json"],
		["JSON null", "null"],
		["a number", "42"],
		["a string", '"Jonas"'],
		["no name", JSON.stringify({ key: STORED_KEY })],
		["a name that is not text", JSON.stringify({ name: 7, key: STORED_KEY })],
		["no key", JSON.stringify({ name: "Jonas" })],
		["a key that is not text", JSON.stringify({ name: "Jonas", key: 7 })],
		["an empty key", JSON.stringify({ name: "Jonas", key: "" })],
	])("counts %s as no voter", (_what, stored) => {
		if (stored !== null) {
			storeRaw(stored);
		}

		expect(readStoredVoter()).toBeNull();
		expect(readVoterKey()).toBeNull();
	});

	it("counts storage it may not read as no voter", () => {
		vi.stubGlobal("localStorage", {
			getItem: () => {
				throw new DOMException("The operation is insecure.", "SecurityError");
			},
		});

		expect(readStoredVoter()).toBeNull();
	});

	it("keeps the key it already stored when the voter is renamed, whatever key the page holds", () => {
		storeRaw(JSON.stringify({ name: "Jonas", key: STORED_KEY }));

		const renamed = rememberVoter("Jonas Weber", "d".repeat(64));

		expect(renamed).toEqual({ name: "Jonas Weber", key: STORED_KEY });
		expect(readStoredVoter()).toEqual(renamed);
	});

	it("keeps the stored key when the page holds none", () => {
		storeRaw(JSON.stringify({ name: "Jonas", key: STORED_KEY }));

		expect(rememberVoter("Jonas Weber")).toEqual({
			name: "Jonas Weber",
			key: STORED_KEY,
		});
	});

	it("takes the key the page holds when storage has none", () => {
		const pageKey = "d".repeat(64);

		expect(rememberVoter("Jonas", pageKey)).toEqual({
			name: "Jonas",
			key: pageKey,
		});
	});

	it("mints a long random key the first time", () => {
		const first = rememberVoter("Jonas");
		localStorage.clear();
		const second = rememberVoter("Jonas");

		expect(first.key).toMatch(A_HEX_KEY);
		expect(second.key).toMatch(A_HEX_KEY);
		expect(second.key).not.toBe(first.key);
	});
});
