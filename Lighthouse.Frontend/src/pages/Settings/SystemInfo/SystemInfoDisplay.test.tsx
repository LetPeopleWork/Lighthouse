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
	authenticationEnabled: true,
	authorizationEnabled: true,
	emergencyAdminSubjects: ["alice@example.com"],
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

	it("renders Authentication, Authorization, and Emergency Admin rows when enabled with subjects", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: true,
			authorizationEnabled: true,
			emergencyAdminSubjects: ["alice@example.com"],
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authentication")).toBeInTheDocument();
		});

		expect(screen.getByText("Authorization")).toBeInTheDocument();
		expect(screen.getByText("Emergency Admin")).toBeInTheDocument();
		expect(screen.getAllByText("Enabled")).toHaveLength(2);
		expect(screen.getByText("alice@example.com")).toBeInTheDocument();
	});

	it("renders Authentication and Authorization rows as Disabled when both flags false", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: false,
			authorizationEnabled: false,
			emergencyAdminSubjects: [],
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authentication")).toBeInTheDocument();
		});

		expect(screen.getAllByText("Disabled")).toHaveLength(2);
	});

	it("hides Emergency Admin row when authorization is enabled but no subjects are configured", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: true,
			authorizationEnabled: true,
			emergencyAdminSubjects: [],
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authorization")).toBeInTheDocument();
		});

		expect(screen.queryByText("Emergency Admin")).not.toBeInTheDocument();
	});

	it("hides Emergency Admin row when authorization is disabled even if subjects are configured", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: true,
			authorizationEnabled: false,
			emergencyAdminSubjects: ["alice@example.com"],
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authorization")).toBeInTheDocument();
		});

		expect(screen.queryByText("Emergency Admin")).not.toBeInTheDocument();
	});

	it("renders multiple emergency admins as a comma-separated value", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: true,
			authorizationEnabled: true,
			emergencyAdminSubjects: [
				"alice@example.com",
				"bob@example.com",
				"carol@example.com",
			],
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Emergency Admin")).toBeInTheDocument();
		});

		expect(
			screen.getByText("alice@example.com, bob@example.com, carol@example.com"),
		).toBeInTheDocument();
	});

	it("renders Authentication as Disabled when authenticationEnabled is undefined", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: undefined,
			authorizationEnabled: false,
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authentication")).toBeInTheDocument();
		});

		const authenticationRow = screen.getByText("Authentication").closest("tr");
		expect(authenticationRow).toHaveTextContent("Disabled");
	});

	it("renders Authorization as Disabled when authorizationEnabled is undefined", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: false,
			authorizationEnabled: undefined,
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authorization")).toBeInTheDocument();
		});

		const authorizationRow = screen.getByText("Authorization").closest("tr");
		expect(authorizationRow).toHaveTextContent("Disabled");
	});

	it("hides Emergency Admin row when emergencyAdminSubjects is undefined", async () => {
		mockGetSystemInfo.mockResolvedValue({
			...mockSystemInfo,
			authenticationEnabled: true,
			authorizationEnabled: true,
			emergencyAdminSubjects: undefined,
		});

		render(
			<MockProvider>
				<SystemInfoDisplay />
			</MockProvider>,
		);

		await waitFor(() => {
			expect(screen.getByText("Authorization")).toBeInTheDocument();
		});

		expect(screen.queryByText("Emergency Admin")).not.toBeInTheDocument();
	});
});
