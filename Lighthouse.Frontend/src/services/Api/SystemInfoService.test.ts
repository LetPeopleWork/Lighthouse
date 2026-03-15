import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { SystemInfoService } from "./SystemInfoService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("SystemInfoService", () => {
	let systemInfoService: SystemInfoService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		systemInfoService = new SystemInfoService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get system info", async () => {
		const mockResponse: SystemInfo = {
			os: "Linux 5.15.0",
			runtime: ".NET 10.0.0",
			architecture: "X64",
			processId: 12345,
			databaseProvider: "sqlite",
			databaseConnection: "/data/lighthouse.db",
			logPath: "/var/log/lighthouse",
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await systemInfoService.getSystemInfo();

		expect(result).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/systeminfo");
	});

	it("should handle null log path", async () => {
		const mockResponse: SystemInfo = {
			os: "Windows 11",
			runtime: ".NET 10.0.0",
			architecture: "X64",
			processId: 99,
			databaseProvider: "postgresql",
			databaseConnection:
				"Host=dbserver;Port=5432;Database=lighthouse;User Id=app",
			logPath: null,
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await systemInfoService.getSystemInfo();

		expect(result.logPath).toBeNull();
	});
});
