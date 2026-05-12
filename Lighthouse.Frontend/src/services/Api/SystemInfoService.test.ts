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
			authenticationEnabled: true,
			authorizationEnabled: true,
			emergencyAdminSubjects: ["alice@example.com"],
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
			authenticationEnabled: false,
			authorizationEnabled: false,
			emergencyAdminSubjects: [],
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await systemInfoService.getSystemInfo();

		expect(result.logPath).toBeNull();
	});

	it("deserialises authentication, authorization, and emergency admin fields", async () => {
		const mockResponse: SystemInfo = {
			os: "Linux 5.15.0",
			runtime: ".NET 10.0.0",
			architecture: "X64",
			processId: 1,
			databaseProvider: "sqlite",
			databaseConnection: null,
			logPath: null,
			authenticationEnabled: true,
			authorizationEnabled: true,
			emergencyAdminSubjects: ["alice@example.com", "bob@example.com"],
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await systemInfoService.getSystemInfo();

		expect(result.authenticationEnabled).toBe(true);
		expect(result.authorizationEnabled).toBe(true);
		expect(result.emergencyAdminSubjects).toEqual([
			"alice@example.com",
			"bob@example.com",
		]);
	});

	it("reads auth flags from the backend wire-format JSON (regression: bugfix-wire-format)", async () => {
		const wireFormatResponse = {
			os: "Linux 5.15.0",
			runtime: ".NET 10.0.7",
			architecture: "X64",
			processId: 1,
			databaseProvider: "postgres",
			databaseConnection: "Host=postgres;Database=lighthouse",
			logPath: "/app/logs",
			authenticationEnabled: true,
			authorizationEnabled: false,
			emergencyAdminSubjects: [],
		};
		mockedAxios.get.mockResolvedValueOnce({ data: wireFormatResponse });

		const result = await systemInfoService.getSystemInfo();

		expect(result.authenticationEnabled).toBe(true);
		expect(result.authorizationEnabled).toBe(false);
	});
});
