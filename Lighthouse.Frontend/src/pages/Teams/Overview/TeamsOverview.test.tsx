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
		}: {
			data: IFeatureOwner[];
			api: string;
			onDelete: (item: IFeatureOwner) => void;
			initialFilterText: string;
			onFilterChange: (value: string) => void;
		}) => {
			mockDeleteButton.mockImplementation((item) => {
				onDelete(item);
			});

			const handleFilterChange = React.useCallback(
				(e: React.ChangeEvent<HTMLInputElement>) => {
					onFilterChange(e.target.value);
				},
				[onFilterChange],
			);

			return (
				<div data-testid="data-overview-table">
					<div data-testid="api-type">{api}</div>
					<div data-testid="initial-filter">{initialFilterText}</div>
					<input
						data-testid="filter-input"
						value={initialFilterText}
						onChange={handleFilterChange}
					/>
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
	});

	const setupTest = (mockTeamService: ITeamService) => {
		const mockContext = createMockApiServiceContext({
			teamService: mockTeamService,
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

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					teamService: mockTeamService,
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
});
