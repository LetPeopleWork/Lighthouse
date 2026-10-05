import { act, renderHook } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type {
	ISizingLog,
	ISizingLogEntry,
} from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { ISizingLogService } from "../../../../services/Api/SizingLogService";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import { useSizingLog } from "./useSizingLog";

const ADVANCED_REPORTING = "GR-051";
const API_VERSIONING = "GR-054";

const aLogSaying = (comment: string): ISizingLog => ({
	entries: [
		{
			kind: "Comment",
			answer: null,
			comment,
			voterName: "Ana Lima",
			channel: "Web",
			recordedAt: "2026-10-07T09:00:00Z",
			isMine: false,
			isOpenQuestion: false,
		} satisfies ISizingLogEntry,
	],
	voters: { yes: [], yesBut: [], no: [] },
});

/** Every read stays on its way until the test lets the server answer it, in whatever order it likes. */
const aServerHoldingEveryRead = () => {
	const reads: {
		referenceId: string;
		answer: (log: ISizingLog) => Promise<void>;
		fail: (error: unknown) => Promise<void>;
	}[] = [];
	const getLog = vi.fn(
		(_teamId: number, referenceId: string) =>
			new Promise<ISizingLog>((resolve, reject) => {
				reads.push({
					referenceId,
					answer: (log) => act(async () => resolve(log)),
					fail: (error) => act(async () => reject(error)),
				});
			}),
	);
	return { getLog, reads };
};

interface ShownFor {
	team: number;
	referenceId: string | null;
}

const renderTheLog = (
	getLog: ISizingLogService["getLog"],
	initialProps: ShownFor,
) => {
	const sizingLogService = {
		castVote: vi.fn(),
		addComment: vi.fn(),
		takeBackMyVote: vi.fn(),
		getLog,
	} satisfies ISizingLogService;
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ sizingLogService })}
		>
			{children}
		</ApiServiceContext.Provider>
	);
	return renderHook(
		({ team, referenceId }: ShownFor) => useSizingLog(team, referenceId, null),
		{ wrapper, initialProps },
	);
};

describe("the log of the Work Item whose votes and comments are open", () => {
	it("drops an answer for the Work Item open before, even while the new one is still being read", async () => {
		const { getLog, reads } = aServerHoldingEveryRead();
		const { result, rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});

		rerender({ team: 7, referenceId: API_VERSIONING });
		await reads[0].answer(aLogSaying("about advanced reporting"));

		expect(result.current.log).toEqual({ status: "reading" });

		await reads[1].answer(aLogSaying("about API versioning"));
		expect(result.current.log).toEqual({
			status: "read",
			...aLogSaying("about API versioning"),
		});
	});

	it("keeps the open Work Item's log when the one open before fails after it", async () => {
		const { getLog, reads } = aServerHoldingEveryRead();
		const { result, rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});

		rerender({ team: 7, referenceId: API_VERSIONING });
		await reads[1].answer(aLogSaying("about API versioning"));
		await reads[0].fail(new Error("Network Error"));

		expect(result.current.log).toEqual({
			status: "read",
			...aLogSaying("about API versioning"),
		});
	});

	it("drops an answer that arrives after the log was closed", async () => {
		const { getLog, reads } = aServerHoldingEveryRead();
		const { result, rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});

		rerender({ team: 7, referenceId: null });
		await reads[0].answer(aLogSaying("about advanced reporting"));

		expect(result.current.log).toEqual({ status: "reading" });
	});

	it("shows nothing of the Work Item open before while the next one is read", async () => {
		const { getLog, reads } = aServerHoldingEveryRead();
		const { result, rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});
		await reads[0].answer(aLogSaying("about advanced reporting"));

		rerender({ team: 7, referenceId: API_VERSIONING });

		expect(result.current.log).toEqual({ status: "reading" });
	});

	it("drops a second read of the Work Item open before, once another one is open", async () => {
		const { getLog, reads } = aServerHoldingEveryRead();
		const { result, rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});
		act(() => {
			result.current.readAgain(ADVANCED_REPORTING);
		});

		rerender({ team: 7, referenceId: API_VERSIONING });
		await reads[1].answer(aLogSaying("about advanced reporting, again"));

		expect(result.current.log).toEqual({ status: "reading" });
	});

	it("reads the open Work Item's log again for the Team the tab moved to", () => {
		const { getLog } = aServerHoldingEveryRead();
		const { rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});

		rerender({ team: 9, referenceId: ADVANCED_REPORTING });

		expect(getLog).toHaveBeenLastCalledWith(9, ADVANCED_REPORTING, null);
	});

	it("reads again on request from the Team the tab shows now", () => {
		const { getLog } = aServerHoldingEveryRead();
		const { result, rerender } = renderTheLog(getLog, {
			team: 7,
			referenceId: ADVANCED_REPORTING,
		});
		rerender({ team: 9, referenceId: ADVANCED_REPORTING });
		getLog.mockClear();

		act(() => {
			result.current.readAgain(ADVANCED_REPORTING);
		});

		expect(getLog).toHaveBeenCalledExactlyOnceWith(9, ADVANCED_REPORTING, null);
	});
});
