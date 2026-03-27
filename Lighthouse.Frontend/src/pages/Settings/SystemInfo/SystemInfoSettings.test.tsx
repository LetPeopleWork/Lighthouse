import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { SystemInfo } from "../../../models/SystemInfo/SystemInfo";

vi.mock("@melloware/react-logviewer", () => ({
	LazyLog: ({ text }: { text: string }) => (
		<div data-testid="log-viewer">{text}</div>
	),
}));

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		BarChart: vi.fn(() => <div data-testid="mock-bar-chart" />),
	};
});

import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ILogService } from "../../../services/Api/LogService";
import type { ISystemInfoService } from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockLogService,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import SystemInfoSettings from "./SystemInfoSettings";

const mockGetSystemInfo = vi.fn();
const mockSystemInfoService: ISystemInfoService = createMockSystemInfoService();
mockSystemInfoService.getSystemInfo = mockGetSystemInfo;
mockSystemInfoService.getRefreshLogs = vi.fn().mockResolvedValue([]);

const mockLogService: ILogService = createMockLogService();
mockLogService.getLogs = vi.fn().mockResolvedValue("sample logs");
mockLogService.getLogLevel = vi.fn().mockResolvedValue("info");
mockLogService.getSupportedLogLevels = vi
	.fn()
	.mockResolvedValue(["info", "warn", "error"]);

const mockSystemInfo: SystemInfo = {
	os: "Linux 5.15.0",
	runtime: ".NET 10.0.1",
	architecture: "X64",
	processId: 1,
	databaseProvider: "sqlite",
	databaseConnection: "/data/lighthouse.db",
	logPath: "/var/logs",
};

const MockProvider = ({ children }: { children: React.ReactNode }) => (
	<ApiServiceContext.Provider
		value={createMockApiServiceContext({
			systemInfoService: mockSystemInfoService,
			logService: mockLogService,
		})}
	>
		{children}
	</ApiServiceContext.Provider>
);

describe("SystemInfoSettings", () => {
	it("renders System Info section", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoSettings />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("System Info")).toBeInTheDocument();
		});
	});

	it("renders Logs section", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoSettings />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Logs")).toBeInTheDocument();
		});
	});

	it("renders Refresh History section", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoSettings />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Refresh History")).toBeInTheDocument();
		});
	});

	it("renders system info data", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoSettings />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Linux 5.15.0")).toBeInTheDocument();
		});
	});
});
