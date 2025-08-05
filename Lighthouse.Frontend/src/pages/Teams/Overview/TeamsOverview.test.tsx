import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import React from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import {
	createMockApiServiceContext,
	createMockProjectService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import TeamsOverview from "./TeamsOverview";

// Mock the react-router-dom's useNavigate function
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: () => mockNavigate,
	};
});

// Mock the useLicenseRestrictions hook
const mockUseLicenseRestrictions = vi.fn();
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => mockUseLicenseRestrictions(),
}));

// Create mock components outside the describe block to reduce nesting
const mockDeleteButton = vi.fn();
const DeleteButton = ({ item }: { item: IFeatureOwner }) => {
	const handleClick = React.useCallback(() => {
		mockDeleteButton(item);
	}, [item]);

	return (
		<button type="button" onClick={handleClick}>
			Delete
		</button>
	);
};

const ConfirmButton = ({
	onClose,
}: {
	onClose: (confirmed: boolean) => void;
}) => {
	const handleClick = React.useCallback(() => {
		onClose(true);
	}, [onClose]);

	return (
		<button type="button" data-testid="confirm-delete" onClick={handleClick}>
			Confirm
		</button>
	);
};

const CancelButton = ({
	onClose,
}: {
	onClose: (confirmed: boolean) => void;
}) => {
	const handleClick = React.useCallback(() => {
		onClose(false);
	}, [onClose]);

	return (
		<button type="button" data-testid="cancel-delete" onClick={handleClick}>
			Cancel
		</button>
	);
};

// Mock components outside the describe block
vi.mock(
	"../../../components/Common/DataOverviewTable/DataOverviewTable",
	() => ({
		default: ({
			data,
			api,
			onDelete,
			initialFilterText,
			onFilterChange,
			disableAdd,
			addButtonTooltip,
		}: {
			data: IFeatureOwner[];
			api: string;
			onDelete: (item: IFeatureOwner) => void;
			initialFilterText: string;
			onFilterChange: (value: string) => void;
			disableAdd?: boolean;
			addButtonTooltip?: string;
		}) => {
			mockDeleteButton.mockImplementation((item) => {
				onDelete(item);
			});

			const handleFilterChange = (e: React.ChangeEvent<HTMLInputElement>) => {
				onFilterChange(e.target.value);
			};

			return (
				<div data-testid="data-overview-table">
					<div data-testid="api-type">{api}</div>
					<div data-testid="initial-filter">{initialFilterText}</div>
					<input
						data-testid="filter-input"
						value={initialFilterText}
						onChange={handleFilterChange}
					/>
					<button
						type="button"
						disabled={disableAdd}
						aria-label={addButtonTooltip || `Add New ${api.slice(0, -1)}`}
					>
						Add New {api.slice(0, -1)}
					</button>
					<ul>
						{data.map((item: IFeatureOwner) => (
							<li key={item.id} data-testid={`team-${item.id}`}>
								{item.name}
								<DeleteButton item={item} />
							</li>
						))}
					</ul>
				</div>
			);
		},
	}),
);

vi.mock(
	"../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog",
	() => ({
		default: ({
			open,
			itemName,
			onClose,
		}: {
			open: boolean;
			itemName: string;
			onClose: (confirmed: boolean) => void;
		}) => {
			if (!open) return null;

			return (
				<div data-testid="delete-dialog">
					<div data-testid="delete-item-name">{itemName}</div>
					<ConfirmButton onClose={onClose} />
					<CancelButton onClose={onClose} />
				</div>
			);
		},
	}),
);

describe("TeamsOverview component", () => {
	const team1 = new Team();
	team1.name = "Team A";
	team1.id = 1;

	const team2 = new Team();
	team2.name = "Team B";
	team2.id = 2;

	const mockTeams = [team1, team2];

	beforeEach(() => {
		vi.clearAllMocks();

		// Default mock for useLicenseRestrictions (premium user with no restrictions)
		mockUseLicenseRestrictions.mockReturnValue({
			canCreateTeam: true,
			canUpdateTeamData: true,
			canUpdateTeamSettings: true,
			canCreateProject: true,
			canUpdateProjectData: true,
			canUpdateProjectSettings: true,
			teamCount: 0,
			projectCount: 0,
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
			createTeamTooltip: "",
			updateTeamDataTooltip: "",
			updateTeamSettingsTooltip: "",
			createProjectTooltip: "",
			updateProjectDataTooltip: "",
			updateProjectSettingsTooltip: "",
		});
	});

	const setupTest = (mockTeamService: ITeamService) => {
		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue([]);

		const mockContext = createMockApiServiceContext({
			teamService: mockTeamService,
			projectService: mockProjectService,
			licensingService: {
				getLicenseStatus: vi.fn().mockResolvedValue({
					canUsePremiumFeatures: true, // Default to premium to avoid restrictions in basic tests
				}),
				importLicense: vi.fn(),
			},
		});

		return render(
			<ApiServiceContext.Provider value={mockContext}>
				<MemoryRouter initialEntries={["/teams"]}>
					<Routes>
						<Route path="/teams" element={<TeamsOverview />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);
	};

	it("should render loading indicator while fetching data", () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockReturnValue(new Promise(() => {})); // Never resolves

		setupTest(mockTeamService);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("should fetch and display teams data", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);

		setupTest(mockTeamService);

		await waitFor(() => {
			expect(mockTeamService.getTeams).toHaveBeenCalledTimes(1);
		});

		// Check that the DataOverviewTable is rendering with the correct data
		expect(screen.getByTestId("data-overview-table")).toBeInTheDocument();
		expect(screen.getByTestId("api-type").textContent).toBe("teams");

		// Check that all teams are displayed
		expect(screen.getByText("Team A")).toBeInTheDocument();
		expect(screen.getByText("Team B")).toBeInTheDocument();
	});

	it("should handle API fetch error", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi
			.fn()
			.mockRejectedValue(new Error("API error"));

		setupTest(mockTeamService);

		await waitFor(() => {
			// Should show error state in LoadingAnimation
			expect(screen.getByText(/error/i)).toBeInTheDocument();
		});
	});

	it("should open delete dialog when delete is clicked", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);

		setupTest(mockTeamService);

		await waitFor(() => {
			expect(screen.getByText("Team A")).toBeInTheDocument();
		});

		// Click delete on Team A
		fireEvent.click(screen.getAllByText("Delete")[0]);

		// Verify delete dialog is shown with correct team name
		expect(screen.getByTestId("delete-dialog")).toBeInTheDocument();
		expect(screen.getByTestId("delete-item-name").textContent).toBe("Team A");
	});

	it("should delete team when confirmation is approved", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);
		mockTeamService.deleteTeam = vi.fn().mockResolvedValue(undefined);

		setupTest(mockTeamService);

		await waitFor(() => {
			expect(screen.getByText("Team A")).toBeInTheDocument();
		});

		// Click delete on Team A
		fireEvent.click(screen.getAllByText("Delete")[0]);

		// Confirm deletion
		fireEvent.click(screen.getByTestId("confirm-delete"));

		await waitFor(() => {
			expect(mockTeamService.deleteTeam).toHaveBeenCalledWith(1);
			expect(mockTeamService.getTeams).toHaveBeenCalledTimes(2); // Called again after deletion
		});
	});

	it("should not delete team when confirmation is canceled", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);
		mockTeamService.deleteTeam = vi.fn().mockResolvedValue(undefined);

		setupTest(mockTeamService);

		await waitFor(() => {
			expect(screen.getByText("Team A")).toBeInTheDocument();
		});

		// Click delete on Team A
		fireEvent.click(screen.getAllByText("Delete")[0]);

		// Cancel deletion
		fireEvent.click(screen.getByTestId("cancel-delete"));

		await waitFor(() => {
			expect(mockTeamService.deleteTeam).not.toHaveBeenCalled();
			expect(mockTeamService.getTeams).toHaveBeenCalledTimes(1); // Not called again
		});
	});

	it("should update URL with filter when filter changes", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);

		setupTest(mockTeamService);

		await waitFor(() => {
			expect(screen.getByTestId("filter-input")).toBeInTheDocument();
		});

		// Change filter value
		fireEvent.change(screen.getByTestId("filter-input"), {
			target: { value: "Team A" },
		});

		// Check that navigate was called with the correct path and search params
		await waitFor(() => {
			expect(mockNavigate).toHaveBeenCalledWith(
				expect.objectContaining({
					pathname: "/teams",
					search: expect.stringContaining("filter=Team"),
				}),
				{ replace: true },
			);
		});
	});

	it("should initialize with filter from URL", async () => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeams = vi.fn().mockResolvedValue(mockTeams);

		const mockProjectService = createMockProjectService();
		mockProjectService.getProjects = vi.fn().mockResolvedValue([]);

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					teamService: mockTeamService,
					projectService: mockProjectService,
					licensingService: {
						getLicenseStatus: vi.fn().mockResolvedValue({
							canUsePremiumFeatures: true,
						}),
						importLicense: vi.fn(),
					},
				})}
			>
				<MemoryRouter initialEntries={["/teams?filter=Team%20B"]}>
					<Routes>
						<Route path="/teams" element={<TeamsOverview />} />
					</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("initial-filter").textContent).toBe("Team B");
		});
	});

	describe("License restrictions", () => {
		it("should disable add team button when non-premium user has reached team limit", async () => {
			const team1 = new Team();
			team1.name = "Team A";
			team1.id = 1;

			const team2 = new Team();
			team2.name = "Team B";
			team2.id = 2;

			const team3 = new Team();
			team3.name = "Team C";
			team3.id = 3;

			const threeTeams = [team1, team2, team3];

			// Mock useLicenseRestrictions for non-premium user at team limit
			mockUseLicenseRestrictions.mockReturnValue({
				canCreateTeam: false,
				canUpdateTeamData: true,
				canUpdateTeamSettings: true,
				canCreateProject: true,
				canUpdateProjectData: true,
				canUpdateProjectSettings: true,
				teamCount: 3,
				projectCount: 0,
				licenseStatus: { canUsePremiumFeatures: false },
				isLoading: false,
				createTeamTooltip:
					"Free users can only create up to 3 teams. You currently have 3 teams. Please obtain a premium license to create more teams.",
				updateTeamDataTooltip: "",
				updateTeamSettingsTooltip: "",
				createProjectTooltip: "",
				updateProjectDataTooltip: "",
				updateProjectSettingsTooltip: "",
			});

			const mockTeamService = createMockTeamService();
			mockTeamService.getTeams = vi.fn().mockResolvedValue(threeTeams);

			const mockProjectService = createMockProjectService();
			mockProjectService.getProjects = vi.fn().mockResolvedValue([]);

			const mockContext = createMockApiServiceContext({
				teamService: mockTeamService,
				projectService: mockProjectService,
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
							<Route path="/" element={<TeamsOverview />} />
						</Routes>
					</MemoryRouter>
				</ApiServiceContext.Provider>,
			);

			await waitFor(() => {
				expect(screen.getByText("Team C")).toBeInTheDocument();
			});

			// Find the add button and check it's disabled
			const addButton = screen.getByText("Add New team");
			expect(addButton).toBeDisabled();

			// Check tooltip is present
			expect(
				screen.getByLabelText(
					"Free users can only create up to 3 teams. You currently have 3 teams. Please obtain a premium license to create more teams.",
				),
			).toBeInTheDocument();
		});

		it("should enable add team button for premium users regardless of team count", async () => {
			const team1 = new Team();
			team1.name = "Team A";
			team1.id = 1;

			const team2 = new Team();
			team2.name = "Team B";
			team2.id = 2;

			const team3 = new Team();
			team3.name = "Team C";
			team3.id = 3;

			const threeTeams = [team1, team2, team3];

			// Mock useLicenseRestrictions for premium user (no restrictions)
			mockUseLicenseRestrictions.mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canUpdateTeamSettings: true,
				canCreateProject: true,
				canUpdateProjectData: true,
				canUpdateProjectSettings: true,
				teamCount: 3,
				projectCount: 0,
				licenseStatus: { canUsePremiumFeatures: true },
				isLoading: false,
				createTeamTooltip: "",
				updateTeamDataTooltip: "",
				updateTeamSettingsTooltip: "",
				createProjectTooltip: "",
				updateProjectDataTooltip: "",
				updateProjectSettingsTooltip: "",
			});

			const mockTeamService = createMockTeamService();
			mockTeamService.getTeams = vi.fn().mockResolvedValue(threeTeams);

			const mockProjectService = createMockProjectService();
			mockProjectService.getProjects = vi.fn().mockResolvedValue([]);

			const mockContext = createMockApiServiceContext({
				teamService: mockTeamService,
				projectService: mockProjectService,
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
							<Route path="/" element={<TeamsOverview />} />
						</Routes>
					</MemoryRouter>
				</ApiServiceContext.Provider>,
			);

			await waitFor(() => {
				expect(screen.getByText("Team C")).toBeInTheDocument();
			});

			// Find the add button and check it's enabled
			const addButton = screen.getByText("Add New team");
			expect(addButton).not.toBeDisabled();
		});
	});
});
