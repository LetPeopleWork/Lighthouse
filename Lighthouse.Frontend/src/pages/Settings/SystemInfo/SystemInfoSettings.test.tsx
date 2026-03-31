import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
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

vi.mock("@mui/x-data-grid", async () => {
	const actual = await vi.importActual("@mui/x-data-grid");
	return { ...actual };
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
mockSystemInfoService.getBackendSbom = vi
	.fn()
	.mockResolvedValue({ packages: [], documentDescribes: [] });
mockSystemInfoService.getFrontendSbom = vi
	.fn()
	.mockResolvedValue({ components: [], metadata: {} });

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
	beforeEach(() => {
		Object.defineProperty(globalThis, "matchMedia", {
			writable: true,
			value: vi.fn().mockImplementation((query: string) => ({
				matches: false,
				media: query,
				onchange: null,
				addListener: vi.fn(),
				removeListener: vi.fn(),
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
				dispatchEvent: vi.fn(),
			})),
		});
		localStorage.clear();
	});

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

	it("renders Third Party Packages section", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoSettings />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Third Party Packages")).toBeInTheDocument();
		});
	});
});
