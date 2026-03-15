import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { SystemInfo } from "../../../models/SystemInfo/SystemInfo";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ISystemInfoService } from "../../../services/Api/SystemInfoService";
import {
	createMockApiServiceContext,
	createMockSystemInfoService,
} from "../../../tests/MockApiServiceProvider";
import SystemInfoDisplay from "./SystemInfoDisplay";

const mockGetSystemInfo = vi.fn();
const mockSystemInfoService: ISystemInfoService = createMockSystemInfoService();
mockSystemInfoService.getSystemInfo = mockGetSystemInfo;

const mockSystemInfo: SystemInfo = {
	os: "Linux 5.15.0-58-generic",
	runtime: ".NET 10.0.1",
	architecture: "X64",
	processId: 42,
	databaseProvider: "sqlite",
	databaseConnection: "/data/lighthouse.db",
	logPath: "/var/lighthouse/logs",
};

const MockProvider = ({ children }: { children: React.ReactNode }) => (
	<ApiServiceContext.Provider
		value={createMockApiServiceContext({
			systemInfoService: mockSystemInfoService,
		})}
	>
		{children}
	</ApiServiceContext.Provider>
);

describe("SystemInfoDisplay", () => {
	let writeTextMock: ReturnType<typeof vi.fn>;

	beforeEach(() => {
		vi.clearAllMocks();
		writeTextMock = vi.fn().mockResolvedValue(undefined);
		Object.defineProperty(navigator, "clipboard", {
			value: { writeText: writeTextMock },
			writable: true,
			configurable: true,
		});
	});

	afterEach(() => {
		vi.resetAllMocks();
	});
	it("renders all system info fields after loading", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Linux 5.15.0-58-generic")).toBeInTheDocument();
		});

		expect(screen.getByText(".NET 10.0.1")).toBeInTheDocument();
		expect(screen.getByText("X64")).toBeInTheDocument();
		expect(screen.getByText("42")).toBeInTheDocument();
		expect(screen.getByText("sqlite")).toBeInTheDocument();
		expect(screen.getByText("/data/lighthouse.db")).toBeInTheDocument();
		expect(screen.getByText("/var/lighthouse/logs")).toBeInTheDocument();
	});

	it("shows labels for each system info field", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Operating System")).toBeInTheDocument();
		});

		expect(screen.getByText("Runtime")).toBeInTheDocument();
		expect(screen.getByText("Architecture")).toBeInTheDocument();
		expect(screen.getByText("Process ID")).toBeInTheDocument();
		expect(screen.getByText("Database")).toBeInTheDocument();
		expect(screen.getByText("Database Connection")).toBeInTheDocument();
		expect(screen.getByText("Log Path")).toBeInTheDocument();
	});

	it("does not show Database Connection row when databaseConnection is null", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			databaseConnection: null,
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Operating System")).toBeInTheDocument();
		});

		expect(screen.queryByText("Database Connection")).not.toBeInTheDocument();
	});

	it("does not show Log Path row when logPath is null", async () => {
		mockGetSystemInfo.mockResolvedValue({ ...mockSystemInfo, logPath: null });

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Operating System")).toBeInTheDocument();
		});

		expect(screen.queryByText("Log Path")).not.toBeInTheDocument();
	});

	it("renders the API documentation link", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(
				screen.getByRole("link", { name: /api documentation/i }),
			).toBeInTheDocument();
		});

		const link = screen.getByRole("link", { name: /api documentation/i });
		expect(link).toHaveAttribute("href", "/api/docs");
		expect(link).toHaveAttribute("target", "_blank");
	});

	it("calls getSystemInfo on mount", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(mockGetSystemInfo).toHaveBeenCalledTimes(1);
		});
	});

	it("copies value to clipboard when a value is clicked", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Linux 5.15.0-58-generic")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByText("Linux 5.15.0-58-generic"));

		await waitFor(() => {
			expect(writeTextMock).toHaveBeenCalledWith("Linux 5.15.0-58-generic");
		});
	});

	it("copies the correct value when different fields are clicked", async () => {
		mockGetSystemInfo.mockResolvedValue(mockSystemInfo);

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("/var/lighthouse/logs")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByText("/var/lighthouse/logs"));

		await waitFor(() => {
			expect(writeTextMock).toHaveBeenCalledWith("/var/lighthouse/logs");
		});
	});
});
