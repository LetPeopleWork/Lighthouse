import { render, screen, waitFor } from "@testing-library/react";
import { vi } from "vitest";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import WorkTrackingSystemConnectionSettings from "./WorkTrackingSystemConnectionSettings";

// Mock the child components
vi.mock("./ConnectionDetailTable", () => ({
	default: ({
		workTrackingSystemConnections,
	}: {
		workTrackingSystemConnections: IWorkTrackingSystemConnection[];
	}) => (
		<div data-testid="connection-detail-table">
			{workTrackingSystemConnections.map((conn) => (
				<div
					key={conn.id}
					data-testid={`connection-${conn.workTrackingSystem}`}
				>
					{conn.name}
				</div>
			))}
		</div>
	),
}));

vi.mock("./ModifyTrackingSystemConnectionDialog", () => ({
	default: () => <div data-testid="modify-dialog" />,
}));

vi.mock("./WorkTrackingSystemSettings", () => ({
	default: () => <div data-testid="work-tracking-system-settings" />,
}));

vi.mock(
	"../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog",
	() => ({
		default: () => <div data-testid="delete-confirmation-dialog" />,
	}),
);

describe("WorkTrackingSystemConnectionSettings", () => {
	const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();

	const mockConnections: IWorkTrackingSystemConnection[] = [
		new WorkTrackingSystemConnection({
			name: "Jira Connection",
			workTrackingSystem: "Jira",
			options: [],
			dataSourceType: "Query",
			id: 1,
		}),
		new WorkTrackingSystemConnection({
			name: "CSV",
			workTrackingSystem: "Csv",
			options: [],
			dataSourceType: "File",
			id: 2,
		}),
		new WorkTrackingSystemConnection({
			name: "Azure DevOps",
			workTrackingSystem: "AzureDevOps",
			options: [],
			dataSourceType: "Query",
			id: 3,
		}),
	];

	beforeEach(() => {
		mockWorkTrackingSystemService.getWorkTrackingSystems = vi
			.fn()
			.mockResolvedValue([]);
		mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems = vi
			.fn()
			.mockResolvedValue(mockConnections);
		mockWorkTrackingSystemService.validateWorkTrackingSystemConnection = vi
			.fn()
			.mockResolvedValue(true);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("should *not* filter out CSV connections from the settings UI", async () => {
		const apiServiceContext = createMockApiServiceContext({
			workTrackingSystemService: mockWorkTrackingSystemService,
		});

		render(
			<ApiServiceContext.Provider value={apiServiceContext}>
				<WorkTrackingSystemConnectionSettings />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.getConfiguredWorkTrackingSystems,
			).toHaveBeenCalled();
		});

		// Should display non-CSV connections
		expect(screen.getByTestId("connection-Jira")).toBeInTheDocument();
		expect(screen.getByTestId("connection-AzureDevOps")).toBeInTheDocument();

		// Should display CSV connection
		expect(screen.queryByTestId("connection-Csv")).toBeInTheDocument();
	});

	it("should display Add Connection button", () => {
		const apiServiceContext = createMockApiServiceContext({
			workTrackingSystemService: mockWorkTrackingSystemService,
		});

		render(
			<ApiServiceContext.Provider value={apiServiceContext}>
				<WorkTrackingSystemConnectionSettings />
			</ApiServiceContext.Provider>,
		);

		expect(screen.getByText("Add Connection")).toBeInTheDocument();
	});

	it("should render all required components", async () => {
		const apiServiceContext = createMockApiServiceContext({
			workTrackingSystemService: mockWorkTrackingSystemService,
		});

		render(
			<ApiServiceContext.Provider value={apiServiceContext}>
				<WorkTrackingSystemConnectionSettings />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("connection-detail-table")).toBeInTheDocument();
		});

		expect(
			screen.getByTestId("work-tracking-system-settings"),
		).toBeInTheDocument();
	});
});
