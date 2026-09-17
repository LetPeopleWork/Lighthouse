import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LogService } from "./LogService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("LogService", () => {
	let logService: LogService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		logService = new LogService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get supported log levels", async () => {
		const mockResponse = ["info", "warn", "error"];
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const logLevels = await logService.getSupportedLogLevels();

		expect(logLevels).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/logs/level/supported");
	});

	it("should get log level", async () => {
		const mockResponse = "info";
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const logLevel = await logService.getLogLevel();

		expect(logLevel).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/logs/level");
	});

	it("should set log level", async () => {
		mockedAxios.post.mockResolvedValueOnce({});

		await logService.setLogLevel("error");

		expect(mockedAxios.post).toHaveBeenCalledWith("/logs/level", {
			level: "error",
		});
	});

	it("should get logs", async () => {
		const mockResponse = "Log data";
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const logs = await logService.getLogs();

		expect(logs).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/logs");
	});

	// Bug #6020. A follower asks every few seconds and must not be handed the whole file each time.
	it("should ask for only the end of the log when a tail is wanted", async () => {
		mockedAxios.get.mockResolvedValueOnce({ data: "the last few lines" });

		const logs = await logService.getLogs(4096);

		expect(logs).toEqual("the last few lines");
		expect(mockedAxios.get).toHaveBeenCalledWith("/logs?tailBytes=4096");
	});

	it("should download logs", async () => {
		const blob = new Blob(["test logs"], { type: "text/plain" });

		mockedAxios.get.mockResolvedValueOnce({ data: blob });

		const createObjectURL = vi.fn().mockReturnValue("blobUrl");
		const revokeObjectURL = vi.fn();
		const appendChild = vi.fn();

		URL.createObjectURL = createObjectURL;
		URL.revokeObjectURL = revokeObjectURL;
		document.body.appendChild = appendChild;

		await logService.downloadLogs();

		expect(mockedAxios.get).toHaveBeenCalledWith("/logs/download", {
			responseType: "blob",
		});
		expect(createObjectURL).toHaveBeenCalled();
		expect(appendChild).toHaveBeenCalled();
		expect(revokeObjectURL).toHaveBeenCalled();
	});
});
