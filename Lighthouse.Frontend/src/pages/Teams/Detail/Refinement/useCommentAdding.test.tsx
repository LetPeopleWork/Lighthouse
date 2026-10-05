import { act, renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { IRefinementRow } from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { ISizingLogService } from "../../../../services/Api/SizingLogService";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import { useCommentAdding } from "./useCommentAdding";

const JONAS: IStoredVoter = { name: "Jonas", key: "c".repeat(64) };
const ADVANCED_REPORTING = "GR-051";

const theRow: IRefinementRow = {
	referenceId: ADVANCED_REPORTING,
	name: "Advanced reporting module",
	url: null,
	state: "Analysing",
	parentReferenceId: "",
	voteCount: 2,
	myVote: null,
	hasComments: true,
};

const aCommenter = () => ({
	voter: JONAS,
	asksForName: false,
	declareName: vi.fn(),
	commentFor: vi.fn((comment: string, declared: IStoredVoter | null) => ({
		comment: { comment, channel: "Web" as const, voterName: declared?.name },
		voterKey: declared?.key ?? null,
	})),
});

const renderTheCommenting = (addComment: ISizingLogService["addComment"]) => {
	const sizingLogService = {
		castVote: vi.fn(),
		addComment: vi.fn(addComment),
		takeBackMyVote: vi.fn(),
		getLog: vi.fn(),
	} satisfies ISizingLogService;
	const onAnswered = vi.fn();
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ sizingLogService })}
		>
			{children}
		</ApiServiceContext.Provider>
	);
	const commenter = aCommenter();
	const hook = renderHook(
		({ team }: { team: number }) =>
			useCommentAdding(team, commenter, onAnswered, vi.fn()),
		{ wrapper, initialProps: { team: 7 } },
	);
	return { ...hook, sizingLogService, onAnswered };
};

const aComment = (comment: string) => ({
	referenceId: ADVANCED_REPORTING,
	comment,
	onSent: vi.fn(),
});

describe("adding a comment to a Work Item without voting", () => {
	it("sends a second comment on a Work Item only once the first has been answered", async () => {
		let answer: (row: IRefinementRow) => void = () => {};
		const { result, sizingLogService } = renderTheCommenting(
			() => new Promise<IRefinementRow>((resolve) => (answer = resolve)),
		);

		act(() => {
			result.current.addComment(aComment("Which API version?"));
			result.current.addComment(aComment("Which API version?"));
		});

		expect(sizingLogService.addComment).toHaveBeenCalledOnce();
		await act(async () => answer(theRow));
		await waitFor(() => expect(result.current.commentsBeingSent.size).toBe(0));
	});

	it("never counts a commented Work Item as just made ready, whatever the server's row says", async () => {
		const { result, onAnswered } = renderTheCommenting(() =>
			Promise.resolve({ ...theRow, madeReady: true } as IRefinementRow),
		);

		act(() => {
			result.current.addComment(aComment("Which API version?"));
		});

		await waitFor(() =>
			expect(onAnswered).toHaveBeenCalledWith(
				expect.objectContaining({ madeReady: false }),
			),
		);
	});

	it("comments on the Team the tab shows now, not the one it opened on", async () => {
		const { result, rerender, sizingLogService } = renderTheCommenting(() =>
			Promise.resolve(theRow),
		);

		rerender({ team: 9 });
		act(() => {
			result.current.addComment(aComment("Which API version?"));
		});

		await waitFor(() =>
			expect(sizingLogService.addComment).toHaveBeenCalledWith(
				9,
				ADVANCED_REPORTING,
				expect.anything(),
				JONAS.key,
			),
		);
	});
});
