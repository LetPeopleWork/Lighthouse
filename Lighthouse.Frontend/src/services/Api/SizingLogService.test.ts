import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IRefinementRow,
	ISizingLog,
} from "../../models/Refinement/Refinement";
import { SizingLogService } from "./SizingLogService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

/**
 * What the sizing log's calls put on the wire: which address, which body, and the browser's voter key in
 * its header. A browser without a key - every signed-in one - sends no header at all rather than an
 * empty one.
 */

const VOTER_KEY = "k".repeat(64);
const VOTER_KEY_HEADER = "X-Lighthouse-Voter-Key";
const WITH_THE_KEY = { headers: { [VOTER_KEY_HEADER]: VOTER_KEY } };
const VOTES_ADDRESS = "/teams/7/refinement/work-items/GR-073/votes";

const theRow: IRefinementRow = {
	referenceId: "GR-073",
	name: "Configuration management",
	url: null,
	state: "Backlog",
	parentReferenceId: "",
	voteCount: 1,
	myVote: "Yes",
};

describe("SizingLogService", () => {
	let sizingLogService: SizingLogService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		sizingLogService = new SizingLogService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	// @us-11 @slice-11 @driving_port @contract-shape:bounded-change
	it("casts a vote to the Work Item's address with the voter's key and answers the row as it now stands", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: theRow });

		const row = await sizingLogService.castVote(
			7,
			"GR-073",
			{ answer: "Yes", channel: "Web", voterName: "Jonas Weber" },
			VOTER_KEY,
		);

		expect(row).toEqual(theRow);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			VOTES_ADDRESS,
			{ answer: "Yes", channel: "Web", voterName: "Jonas Weber" },
			WITH_THE_KEY,
		);
	});

	// @us-15 @slice-15 @boundary @contract-shape:bounded-change
	it("sends no voter key header when the browser holds none", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: theRow });

		await sizingLogService.castVote(
			7,
			"GR-073",
			{ answer: "No", channel: "Web" },
			null,
		);

		const [, , options] = mockedAxios.post.mock.calls[0];
		expect(options?.headers?.[VOTER_KEY_HEADER]).toBeUndefined();
	});

	// @us-11 @slice-11 @boundary @contract-shape:bounded-change
	it("keeps a Work Item reference that needs escaping in one piece of the address", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: theRow });

		await sizingLogService.castVote(
			7,
			"OPS/12 #3",
			{ answer: "Yes", channel: "Web" },
			VOTER_KEY,
		);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/teams/7/refinement/work-items/OPS%2F12%20%233/votes",
			{ answer: "Yes", channel: "Web" },
			WITH_THE_KEY,
		);
	});

	// @us-11 @slice-11 @error @contract-shape:unbounded-preservation
	it("passes a refused vote on to the caller", async () => {
		mockedAxios.post.mockRejectedValueOnce(
			new Error("Request failed with status code 409"),
		);

		await expect(
			sizingLogService.castVote(
				7,
				"GR-040",
				{ answer: "Yes", channel: "Web" },
				VOTER_KEY,
			),
		).rejects.toThrow("409");
	});

	// @us-12 @slice-12 @driving_port @contract-shape:bounded-change
	it("sends a question to the Work Item's comments with the voter's key", async () => {
		mockedAxios.post.mockResolvedValueOnce({ data: theRow });

		await sizingLogService.addComment(
			7,
			"GR-054",
			{
				comment: "Which API version?",
				channel: "Web",
				voterName: "Jonas Weber",
			},
			VOTER_KEY,
		);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/teams/7/refinement/work-items/GR-054/comments",
			{
				comment: "Which API version?",
				channel: "Web",
				voterName: "Jonas Weber",
			},
			WITH_THE_KEY,
		);
	});

	// @us-12 @slice-12 @driving_port @contract-shape:pure-function
	it("reads a Work Item's log with the voter's key", async () => {
		const log: ISizingLog = {
			entries: [
				{
					kind: "Vote",
					answer: "YesBut",
					comment: "only if the PDF export moves out",
					voterName: "Ana Lima",
					channel: "Web",
					recordedAt: "2026-10-07T09:00:00Z",
					isMine: false,
					isOpenQuestion: false,
				},
			],
			voters: { yes: [], yesBut: ["Ana Lima"], no: [] },
		};
		mockedAxios.get.mockResolvedValueOnce({ data: log });

		const answer = await sizingLogService.getLog(7, "GR-051", VOTER_KEY);

		expect(answer).toEqual(log);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/refinement/work-items/GR-051/log",
			WITH_THE_KEY,
		);
	});

	// @us-16 @slice-16 @driving_port @contract-shape:bounded-change
	it.each(["Yes", "YesBut", "No"] as const)(
		"takes back the voter's own %s with their key, naming the answer it takes back",
		async (answer) => {
			mockedAxios.delete.mockResolvedValueOnce({
				data: { ...theRow, voteCount: 0, myVote: null },
			});

			const row = await sizingLogService.takeBackMyVote(
				7,
				"GR-073",
				answer,
				VOTER_KEY,
			);

			expect(row.voteCount).toBe(0);
			expect(mockedAxios.delete).toHaveBeenCalledWith(
				`${VOTES_ADDRESS}/mine?answer=${answer}`,
				WITH_THE_KEY,
			);
		},
	);
});
