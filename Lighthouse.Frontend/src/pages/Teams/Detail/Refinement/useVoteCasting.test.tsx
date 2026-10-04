import { act, renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	IVotedRow,
} from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { ISizingLogService } from "../../../../services/Api/SizingLogService";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import { sizingMomentOf, useVoteCasting } from "./useVoteCasting";

vi.mock("../../../../services/UsageData/usageDataReporter", () => ({
	useUsageDataReporter: () => () => {},
}));

const JONAS: IStoredVoter = { name: "Jonas", key: "c".repeat(64) };

const theRow: IVotedRow = {
	referenceId: "GR-073",
	name: "Configuration management",
	url: null,
	state: "Backlog",
	parentReferenceId: "",
	voteCount: 1,
	myVote: "Yes",
	madeReady: false,
};

const aVoterIdentity = (asksForName: boolean) => ({
	voter: asksForName ? null : JONAS,
	asksForName,
	declareName: vi.fn((name: string) => ({ ...JONAS, name })),
	ballotFor: vi.fn(
		(answer: IRefinementRow["myVote"], declared: IStoredVoter | null) => ({
			vote: {
				answer: answer ?? "Yes",
				channel: "Web" as const,
				voterName: declared?.name,
			},
			voterKey: declared?.key ?? null,
		}),
	),
});

const renderTheCasting = (
	castVote: ISizingLogService["castVote"],
	{ teamId = 7, asksForName = false } = {},
) => {
	const sizingLogService = {
		castVote: vi.fn(castVote),
		addComment: vi.fn(),
		takeBackMyVote: vi.fn(),
		getLog: vi.fn(),
	} satisfies ISizingLogService;
	const identity = aVoterIdentity(asksForName);
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ sizingLogService })}
		>
			{children}
		</ApiServiceContext.Provider>
	);
	const hook = renderHook(
		({ team }: { team: number }) =>
			useVoteCasting(team, identity, vi.fn(), vi.fn(), null),
		{ wrapper, initialProps: { team: teamId } },
	);
	return { ...hook, sizingLogService };
};

describe("casting a vote from the tab", () => {
	it("sends a second vote on a row only once the first has been answered", async () => {
		let answer: (row: IVotedRow) => void = () => {};
		const { result, sizingLogService } = renderTheCasting(
			() => new Promise<IVotedRow>((resolve) => (answer = resolve)),
		);

		act(() => {
			result.current.onVote({ referenceId: "GR-073", answer: "Yes" });
			result.current.onVote({ referenceId: "GR-073", answer: "No" });
		});

		expect(sizingLogService.castVote).toHaveBeenCalledOnce();
		await act(async () => answer(theRow));
		await waitFor(() => expect(result.current.votesBeingSent.size).toBe(0));
	});

	it("casts for the Team the tab shows now, not the one it opened on", async () => {
		const { result, rerender, sizingLogService } = renderTheCasting(() =>
			Promise.resolve(theRow),
		);

		rerender({ team: 9 });
		act(() => {
			result.current.onVote({ referenceId: "GR-073", answer: "Yes" });
		});

		await waitFor(() =>
			expect(sizingLogService.castVote).toHaveBeenCalledWith(
				9,
				"GR-073",
				expect.anything(),
				JONAS.key,
			),
		);
	});

	it("casts nothing when a name is given with no vote waiting for it", () => {
		const { result, sizingLogService } = renderTheCasting(
			() => Promise.resolve(theRow),
			{ asksForName: true },
		);

		act(() => {
			result.current.voteUnderName("Jonas");
		});

		expect(sizingLogService.castVote).not.toHaveBeenCalled();
		expect(result.current.isAskingForName).toBe(false);
	});
});

describe("when a vote counts as cast, relative to the Team's Refinement", () => {
	it.each([
		[null, false, "NoCadence"],
		[null, true, "NoCadence"],
		[undefined, undefined, "NoCadence"],
		["2026-10-08", true, "OnRefinementDay"],
		["2026-10-08", false, "OnOtherDay"],
		["2026-10-08", undefined, "OnOtherDay"],
	])(
		"with the next Refinement on %s and a Refinement day %s is %s",
		(nextRefinementDate, isRefinementDay, moment) => {
			expect(sizingMomentOf({ nextRefinementDate, isRefinementDay })).toBe(
				moment,
			);
		},
	);

	it("is NoCadence before the tab has loaded", () => {
		expect(sizingMomentOf(null)).toBe("NoCadence");
	});
});
