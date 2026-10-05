import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { VoterIdentity } from "../models/Refinement/Refinement";
import { useVoterIdentity } from "./useVoterIdentity";

const VOTER_STORAGE_KEY = "lighthouse:refinement:voter";
const STORED_KEY = "c".repeat(64);
const JONAS = { name: "Jonas", key: STORED_KEY };

const aBrowserThatKeeps = (voter: { name: string; key: string }) =>
	localStorage.setItem(VOTER_STORAGE_KEY, JSON.stringify(voter));

const aBrowserThatRefusesToStore = () =>
	vi.stubGlobal("localStorage", {
		getItem: () => null,
		setItem: () => {
			throw new DOMException("Storage is full", "QuotaExceededError");
		},
	});

describe("who a vote from this browser is cast as", () => {
	beforeEach(() => {
		localStorage.clear();
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	it("asks a voter without sign-in for a name until the browser keeps one", () => {
		const { result } = renderHook(() => useVoterIdentity("SelfDeclared"));

		expect(result.current.asksForName).toBe(true);
		expect(result.current.changeableName).toBeNull();
	});

	it("casts as the name and key the browser keeps", () => {
		aBrowserThatKeeps(JONAS);
		const { result } = renderHook(() => useVoterIdentity("SelfDeclared"));

		expect(result.current.asksForName).toBe(false);
		expect(result.current.changeableName).toBe("Jonas");
		expect(result.current.ballotFor("YesBut", result.current.voter)).toEqual({
			vote: { answer: "YesBut", channel: "Web", voterName: "Jonas" },
			voterKey: STORED_KEY,
		});
	});

	it("casts a signed-in voter as their account, sending no name and no key", () => {
		aBrowserThatKeeps(JONAS);
		const { result } = renderHook(() => useVoterIdentity("Account"));

		expect(result.current.asksForName).toBe(false);
		expect(result.current.changeableName).toBeNull();
		expect(result.current.ballotFor("No", JONAS)).toEqual({
			vote: { answer: "No", channel: "Web" },
			voterKey: null,
		});
	});

	it("follows the instance once the tab says it signs voters in", () => {
		aBrowserThatKeeps(JONAS);
		const { result, rerender } = renderHook(
			({ identity }: { identity: VoterIdentity | undefined }) =>
				useVoterIdentity(identity),
			{ initialProps: { identity: undefined as VoterIdentity | undefined } },
		);

		rerender({ identity: "Account" });

		expect(result.current.ballotFor("Yes", JONAS)).toEqual({
			vote: { answer: "Yes", channel: "Web" },
			voterKey: null,
		});
	});

	it("comments as the account once the tab says it signs voters in", () => {
		aBrowserThatKeeps(JONAS);
		const { result, rerender } = renderHook(
			({ identity }: { identity: VoterIdentity | undefined }) =>
				useVoterIdentity(identity),
			{ initialProps: { identity: undefined as VoterIdentity | undefined } },
		);

		rerender({ identity: "Account" });

		expect(result.current.commentFor("Which API version?", JONAS)).toEqual({
			comment: { comment: "Which API version?", channel: "Web" },
			voterKey: null,
		});
	});

	it("sends no name and no key for a vote with nobody declared", () => {
		const { result } = renderHook(() => useVoterIdentity("SelfDeclared"));

		expect(result.current.ballotFor("Yes", null)).toEqual({
			vote: { answer: "Yes", channel: "Web" },
			voterKey: null,
		});
	});

	it("declares the name without the spaces around it", () => {
		const { result } = renderHook(() => useVoterIdentity("SelfDeclared"));

		act(() => {
			result.current.declareName("  Jonas Weber  ");
		});

		expect(result.current.changeableName).toBe("Jonas Weber");
		expect(result.current.asksForName).toBe(false);
	});

	it("keeps the key the page holds across a rename when the browser refuses to store it", () => {
		aBrowserThatRefusesToStore();
		const { result } = renderHook(() => useVoterIdentity("SelfDeclared"));

		let first = { name: "", key: "" };
		act(() => {
			first = result.current.declareName("Jonas");
		});
		let renamed = { name: "", key: "" };
		act(() => {
			renamed = result.current.declareName("Jonas Weber");
		});

		expect(renamed).toEqual({ name: "Jonas Weber", key: first.key });
	});
});
