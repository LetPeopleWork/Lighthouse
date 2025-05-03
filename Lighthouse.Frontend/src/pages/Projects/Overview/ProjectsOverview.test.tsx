import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Project } from "../../../models/Project/Project";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IProjectService } from "../../../services/Api/ProjectService";
import {
	createMockApiServiceContext,
	createMockProjectService,
} from "../../../tests/MockApiServiceProvider";
import ProjectsOverview from "./ProjectsOverview";

// Mock the react-router-dom's useNavigate function
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: () => mockNavigate,
	};
});

// Mock the components used by ProjectsOverview
vi.mock(
	"../../../components/Common/DataOverviewTable/DataOverviewTable",
	() => ({
		default: vi.fn(
			({ data, api, onDelete, initialFilterText, onFilterChange }) => (
				<div data-testid="data-overview-table">
					<div data-testid="api-type">{api}</div>
					<div data-testid="initial-filter">{initialFilterText}</div>
					<input
						data-testid="filter-input"
						value={initialFilterText}
						onChange={(e) => onFilterChange(e.target.value)}
					/>
					<ul>
						{data.map((item: { id: number; name: string }) => (
							<li key={item.id} data-testid={`project-${item.id}`}>
								{item.name}
								<button
									data-testid={`delete-${item.id}`}
									onClick={() => onDelete(item)}
									type="button"
								>
									Delete
								</button>
							</li>
						))}
					</ul>
				</div>
			),
		),
	}),
);

vi.mock(
	"../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog",
	() => ({
		default: vi.fn(({ open, itemName, onClose }) =>
			open ? (
				<div data-testid="delete-dialog">
					<div data-testid="delete-item-name">{itemName}</div>
					<button
						data-testid="confirm-delete"
						onClick={() => onClose(true)}
						type="button"
					>
						Confirm
					</button>
					<button
						data-testid="cancel-delete"
						onClick={() => onClose(false)}
						type="button"
					>
						Cancel
					</button>
				</div>
			) : null,
		),
	}),
);

vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: vi.fn(({ children, isLoading, hasError }) => (
		<div>
			{isLoading && (
				<div
					role="progressbar"
					aria-valuenow={0}
					aria-valuemin={0}
					aria-valuemax={100}
					tabIndex={0}
				>
					Loading...
				</div>
			)}
			{hasError && <div>Error loading data</div>}
			{!isLoading && !hasError && children}
		</div>
	)),
}));

describe("ProjectsOverview component", () => {
	const mockProjects = [
		new Project("Project Alpha", 1, [], [], [], new Date()),
		new Project("Project Beta", 2, [], [], [], new Date()),
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	const setupTest = (mockProjectService: IProjectService) => {
		const mockContext = createMockApiServiceContext({
			projectService: mockProjectService,
		});

		return render(
			<ApiServiceContext.Provider value={mockContext}>
				<MemoryRouter initialEntries={["/projects"]}>
					<Routes>
						<Route path="/projects" element={<ProjectsOverview />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);
	};

	it("should render loading indicator while fetching data", () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi
			.fn()
			.mockReturnValue(new Promise(() => {})); // Never resolves

		setupTest(mockProjectService);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("should fetch and display projects data", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);

		setupTest(mockProjectService);

		await waitFor(() => {
			expect(mockProjectService.getProjects).toHaveBeenCalledTimes(1);
		});

		// Check that the DataOverviewTable is rendering with the correct data
		expect(screen.getByTestId("data-overview-table")).toBeInTheDocument();
		expect(screen.getByTestId("api-type").textContent).toBe("projects");

		// Check that all projects are displayed
		expect(screen.getByText("Project Alpha")).toBeInTheDocument();
		expect(screen.getByText("Project Beta")).toBeInTheDocument();
	});

	it("should handle API fetch error", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi
			.fn()
			.mockRejectedValue(new Error("API error"));

		setupTest(mockProjectService);

		await waitFor(() => {
			expect(screen.getByText(/Error loading data/i)).toBeInTheDocument();
		});
	});

	it("should open delete dialog when delete is clicked", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);

		setupTest(mockProjectService);

		await waitFor(() => {
			expect(screen.getByText("Project Alpha")).toBeInTheDocument();
		});

		// Click delete on Project Alpha
		fireEvent.click(screen.getByTestId("delete-1"));

		// Verify delete dialog is shown with correct project name
		expect(screen.getByTestId("delete-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("delete-item-name").textContent).toBe(
			"Project Alpha",
		);
	});

	it("should delete project when confirmation is approved", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);
		mockProjectService.deleteProject = vi.fn().mockResolvedValue(undefined);

		setupTest(mockProjectService);

		await waitFor(() => {
			expect(screen.getByText("Project Alpha")).toBeInTheDocument();
		});

		// Click delete on Project Alpha
		fireEvent.click(screen.getByTestId("delete-1"));

		// Confirm deletion
		fireEvent.click(screen.getByTestId("confirm-delete"));

		await waitFor(() => {
			expect(mockProjectService.deleteProject).toHaveBeenCalledWith(1);
			expect(mockProjectService.getProjects).toHaveBeenCalledTimes(2); // Called again after deletion
		});
	});

	it("should not delete project when confirmation is canceled", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);
		mockProjectService.deleteProject = vi.fn().mockResolvedValue(undefined);

		setupTest(mockProjectService);

		await waitFor(() => {
			expect(screen.getByText("Project Alpha")).toBeInTheDocument();
		});

		// Click delete on Project Alpha
		fireEvent.click(screen.getByTestId("delete-1"));

		// Cancel deletion
		fireEvent.click(screen.getByTestId("cancel-delete"));

		await waitFor(() => {
			expect(mockProjectService.deleteProject).not.toHaveBeenCalled();
			expect(mockProjectService.getProjects).toHaveBeenCalledTimes(1); // Not called again
		});
	});

	it("should update URL with filter when filter changes", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);

		setupTest(mockProjectService);

		await waitFor(() => {
			expect(screen.getByTestId("filter-input")).toBeInTheDocument();
		});

		// Change filter value
		fireEvent.change(screen.getByTestId("filter-input"), {
			target: { value: "Project Alpha" },
		});

		// Check that navigate was called with the correct path and search params
		await waitFor(() => {
			expect(mockNavigate).toHaveBeenCalledWith(
				expect.objectContaining({
					pathname: "/projects",
					search: expect.stringContaining("filter=Project"),
				}),
				{ replace: true },
			);
		});
	});

	it("should initialize with filter from URL", async () => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue(mockProjects);

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					projectService: mockProjectService,
				})}
			>
				<MemoryRouter initialEntries={["/projects?filter=Project%20Beta"]}>
					<Routes>
						<Route path="/projects" element={<ProjectsOverview />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("initial-filter").textContent).toBe(
				"Project Beta",
			);
		});
	});
});
