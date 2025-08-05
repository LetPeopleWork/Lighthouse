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
			({
				data,
				api,
				onDelete,
				initialFilterText,
				onFilterChange,
				disableAdd,
				addButtonTooltip,
			}) => (
				<div data-testid="data-overview-table">
					<div data-testid="api-type">{api}</div>
					<div data-testid="initial-filter">{initialFilterText}</div>
					<input
						data-testid="filter-input"
						value={initialFilterText}
						onChange={(e) => onFilterChange(e.target.value)}
					/>
					<button
						type="button"
						disabled={disableAdd}
						aria-label={addButtonTooltip || `Add New ${api.slice(0, -1)}`}
					>
						Add New {api.slice(0, -1)}
					</button>
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
				<progress value={0} max={100}>
					Loading...
				</progress>
			)}
			{hasError && <div>Error loading data</div>}
			{!isLoading && !hasError && children}
		</div>
	)),
}));

describe("ProjectsOverview component", () => {
	const project1 = new Project();
	project1.name = "Project Alpha";
	project1.id = 1;

	const project2 = new Project();
	project2.name = "Project Beta";
	project2.id = 2;

	const mockProjects = [project1, project2];

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

	describe("License restrictions", () => {
		it("should disable add project button when non-premium user has reached project limit", async () => {
			const singleProject = new Project();
			singleProject.name = "Single Project";
			singleProject.id = 1;

			const mockProjectService = createMockProjectService();
			mockProjectService.getProjects = vi
				.fn()
				.mockResolvedValue([singleProject]);

			const mockContext = createMockApiServiceContext({
				projectService: mockProjectService,
				teamService: {
					getTeams: vi.fn().mockResolvedValue([]),
					getTeam: vi.fn(),
					deleteTeam: vi.fn(),
					getTeamSettings: vi.fn(),
					validateTeamSettings: vi.fn(),
					updateTeam: vi.fn(),
					createTeam: vi.fn(),
					updateTeamData: vi.fn(),
				},
				licensingService: {
					getLicenseStatus: vi.fn().mockResolvedValue({
						canUsePremiumFeatures: false,
					}),
					importLicense: vi.fn(),
				},
			});

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<MemoryRouter>
						<Routes>
							<Route path="/" element={<ProjectsOverview />} />
						</Routes>
					</MemoryRouter>
				</ApiServiceContext.Provider>,
			);

			await waitFor(() => {
				expect(screen.getByText("Single Project")).toBeInTheDocument();
			});

			// Find the add button and check it's disabled
			const addButton = screen.getByText("Add New project");
			expect(addButton).toBeDisabled();

			// Check tooltip is present
			expect(
				screen.getByLabelText(
					"Free users can only create up to 1 project. You currently have 1 project. Please obtain a premium license to create more projects.",
				),
			).toBeInTheDocument();
		});

		it("should enable add project button for premium users regardless of project count", async () => {
			const multipleProjects = [
				{ ...new Project(), name: "Project 1", id: 1 },
				{ ...new Project(), name: "Project 2", id: 2 },
			];

			const mockProjectService = createMockProjectService();
			mockProjectService.getProjects = vi
				.fn()
				.mockResolvedValue(multipleProjects);

			const mockContext = createMockApiServiceContext({
				projectService: mockProjectService,
				teamService: {
					getTeams: vi.fn().mockResolvedValue([]),
					getTeam: vi.fn(),
					deleteTeam: vi.fn(),
					getTeamSettings: vi.fn(),
					validateTeamSettings: vi.fn(),
					updateTeam: vi.fn(),
					createTeam: vi.fn(),
					updateTeamData: vi.fn(),
				},
				licensingService: {
					getLicenseStatus: vi.fn().mockResolvedValue({
						canUsePremiumFeatures: true,
					}),
					importLicense: vi.fn(),
				},
			});

			render(
				<ApiServiceContext.Provider value={mockContext}>
					<MemoryRouter>
						<Routes>
							<Route path="/" element={<ProjectsOverview />} />
						</Routes>
					</MemoryRouter>
				</ApiServiceContext.Provider>,
			);

			await waitFor(() => {
				expect(screen.getByText("Project 2")).toBeInTheDocument();
			});

			// Find the add button and check it's enabled
			const addButton = screen.getByText("Add New project");
			expect(addButton).not.toBeDisabled();
		});
	});
});
