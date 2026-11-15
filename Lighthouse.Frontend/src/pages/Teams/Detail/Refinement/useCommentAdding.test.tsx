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

const renderTheCommentingAs = (
	addComment: ISizingLogService["addComment"],
	{ asksForName }: { asksForName: boolean },
) => {
	const sizingLogService = {
		castVote: vi.fn(),
		addComment: vi.fn(addComment),
		takeBackMyVote: vi.fn(),
		getLog: vi.fn(),
	} satisfies ISizingLogService;
	const onAnswered = vi.fn();
	const onFailure = vi.fn();
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ sizingLogService })}
		>
			{children}
		</ApiServiceContext.Provider>
	);
	const commenter = {
		voter: asksForName ? null : JONAS,
		asksForName,
		declareName: vi.fn((name: string) => ({ ...JONAS, name })),
		commentFor: vi.fn((comment: string, declared: IStoredVoter | null) => ({
			comment: { comment, channel: "Web" as const, voterName: declared?.name },
			voterKey: declared?.key ?? null,
		})),
	};
	const hook = renderHook(
		() => useCommentAdding(7, commenter, onAnswered, onFailure),
		{ wrapper },
	);
	return { ...hook, sizingLogService, onAnswered, onFailure };
};

describe("a comment the server refuses, and a comment from a browser without a name", () => {
	it("reports a refused comment, never says it was sent, and stops counting it as being sent", async () => {
		const refusal = new Error("Network Error");
		const { result, onFailure, onAnswered } = renderTheCommentingAs(
			() => Promise.reject(refusal),
			{ asksForName: false },
		);
		const comment = aComment("Which API version?");

		act(() => {
			result.current.addComment(comment);
		});
		expect(result.current.commentsBeingSent.has(ADVANCED_REPORTING)).toBe(true);

		await waitFor(() =>
			expect(onFailure).toHaveBeenCalledExactlyOnceWith(refusal),
		);
		await waitFor(() => expect(result.current.commentsBeingSent.size).toBe(0));
		expect(comment.onSent).not.toHaveBeenCalled();
		expect(onAnswered).not.toHaveBeenCalled();
	});

	it("asks a browser without a name for it and sends the comment under it once", async () => {
		const { result, sizingLogService } = renderTheCommentingAs(
			() => Promise.resolve(theRow),
			{ asksForName: true },
		);
		const comment = aComment("Which API version?");

		act(() => {
			result.current.addComment(comment);
		});
		expect(result.current.isAskingForName).toBe(true);
		expect(sizingLogService.addComment).not.toHaveBeenCalled();

		act(() => {
			result.current.commentUnderName("Ana");
		});

		expect(result.current.isAskingForName).toBe(false);
		await waitFor(() => expect(comment.onSent).toHaveBeenCalledOnce());
		expect(sizingLogService.addComment).toHaveBeenCalledExactlyOnceWith(
			7,
			ADVANCED_REPORTING,
			{ comment: "Which API version?", channel: "Web", voterName: "Ana" },
			JONAS.key,
		);
	});
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
