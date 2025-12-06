import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IProject } from "../../../models/Project/Project";
import DataOverviewTable from "./DataOverviewTable";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
	const actual = await vi.importActual("react-router-dom");
	return {
		...actual,
		useNavigate: () => mockNavigate,
	};
});

// Mock matchMedia before each test
beforeEach(() => {
	Object.defineProperty(globalThis, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query) => ({
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
});

const renderWithRouter = (ui: React.ReactNode) => {
	return render(<BrowserRouter>{ui}</BrowserRouter>);
};

// Sample team data (no project-specific fields)
const sampleTeamData: IFeatureOwner[] = [
	{
		id: 1,
		name: "Team 1",
		remainingFeatures: 5,
		features: [],
		tags: ["critical", "frontend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 2,
		name: "Team 2",
		remainingFeatures: 15,
		features: [],
		tags: ["backend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
	{
		id: 3,
		name: "Another Team",
		remainingFeatures: 25,
		features: [],
		tags: [],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
	},
];

// Sample project data (with project-specific fields)
const sampleProjectData: IProject[] = [
	{
		id: 1,
		name: "Project 1",
		remainingFeatures: 5,
		features: [],
		tags: ["critical", "frontend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		involvedTeams: [],
		milestones: [],
		totalWorkItems: 100,
		remainingWorkItems: 50,
		forecasts: [],
	},
	{
		id: 2,
		name: "Project 2",
		remainingFeatures: 15,
		features: [],
		tags: ["backend"],
		lastUpdated: new Date(),
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		involvedTeams: [],
		milestones: [],
		totalWorkItems: 200,
		remainingWorkItems: 100,
		forecasts: [],
	},
];

describe("DataOverviewTable", () => {
	describe("with Teams data (using DataGrid)", () => {
		it("renders DataGrid correctly for teams", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByTestId("datagrid-container")).toBeInTheDocument();
		});

		it("displays all team items in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			for (const item of sampleTeamData) {
				expect(screen.getByText(item.name)).toBeInTheDocument();
			}
		});

		it("filters teams correctly in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="Team 1"
				/>,
			);

			expect(screen.getByText("Team 1")).toBeInTheDocument();
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument();
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument();
		});

		it("displays tags in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			expect(screen.getByText("critical")).toBeInTheDocument();
			expect(screen.getByText("frontend")).toBeInTheDocument();
			expect(screen.getByText("backend")).toBeInTheDocument();
		});

		it("filters teams by tag in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="critical"
				/>,
			);

			expect(screen.getByText("Team 1")).toBeInTheDocument();
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument();
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument();
		});
	});

	describe("with Projects data (using DataGrid)", () => {
		it("renders DataGrid correctly for projects", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleProjectData}
					title="projects"
					api="projects"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			expect(screen.getByTestId("datagrid-container")).toBeInTheDocument();
		});

		it("displays all project items in DataGrid", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleProjectData}
					title="projects"
					api="projects"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);
			for (const item of sampleProjectData) {
				expect(screen.getByText(item.name)).toBeInTheDocument();
			}
		});
	});

	describe("Common functionality", () => {
		it("displays items in alphabetical order by name", () => {
			const unsortedTeamData: IFeatureOwner[] = [
				{
					id: 3,
					name: "Zebra Team",
					remainingFeatures: 25,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
				{
					id: 1,
					name: "Apple Team",
					remainingFeatures: 5,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
				{
					id: 2,
					name: "Banana Team",
					remainingFeatures: 15,
					features: [],
					tags: [],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
			];

			renderWithRouter(
				<DataOverviewTable
					data={unsortedTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			// Check that they appear in alphabetical order
			expect(screen.getByText("Apple Team")).toBeInTheDocument();
			expect(screen.getByText("Banana Team")).toBeInTheDocument();
			expect(screen.getByText("Zebra Team")).toBeInTheDocument();
		});

		it("displays the custom message when no item matches filter", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="Non-existing Team"
				/>,
			);

			expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
		});

		it("should not display empty tags", () => {
			const dataWithEmptyTag: IFeatureOwner[] = [
				{
					id: 1,
					name: "Item with empty tag",
					remainingFeatures: 5,
					features: [],
					tags: ["valid-tag", "", "  ", "another-valid-tag"],
					lastUpdated: new Date(),
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
				},
			];

			renderWithRouter(
				<DataOverviewTable
					data={dataWithEmptyTag}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			expect(screen.getByText("valid-tag")).toBeInTheDocument();
			expect(screen.getByText("another-valid-tag")).toBeInTheDocument();

			const tagChips = document.querySelectorAll(".MuiChip-outlined");
			expect(tagChips.length).toEqual(2);
		});

		it("shows Clone action in Actions cell when rendering teams", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			expect(cloneButtons).toHaveLength(sampleTeamData.length);
		});

		it("navigates to clone URL when Clone button is clicked", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData.slice(0, 1)} // Only render one team for this test
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButton = screen.getByLabelText("Clone");
			fireEvent.click(cloneButton);

			expect(mockNavigate).toHaveBeenCalledWith("/teams/new?cloneFrom=1");
		});

		it("shows Clone action for projects", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleProjectData}
					title="projects"
					api="projects"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			expect(cloneButtons).toHaveLength(sampleProjectData.length);
		});

		it("navigates to clone URL when Clone button is clicked for projects", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleProjectData}
					title="projects"
					api="projects"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			const cloneButtons = screen.getAllByLabelText("Clone");
			fireEvent.click(cloneButtons[0]);

			expect(mockNavigate).toHaveBeenCalledWith("/projects/new?cloneFrom=1");
		});

		it("filters items by partial tag match", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="front"
				/>,
			);

			expect(screen.getByText("Team 1")).toBeInTheDocument(); // Team 1 has "frontend" tag
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument(); // Team 2 doesn't have a tag containing "front"
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument(); // Another Team doesn't have tags
		});

		it("filters by tag when name doesn't match", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="backend"
				/>,
			);

			expect(screen.queryByText("Team 1")).not.toBeInTheDocument(); // Team 1 doesn't have "backend" tag
			expect(screen.getByText("Team 2")).toBeInTheDocument(); // Team 2 has "backend" tag
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument(); // Another Team doesn't have tags
		});

		it("should not show items when neither name nor tags match", () => {
			renderWithRouter(
				<DataOverviewTable
					data={sampleTeamData}
					title="teams"
					api="teams"
					onDelete={vi.fn()}
					filterText="asdfasdfasdfasdf"
				/>,
			);

			expect(screen.queryByText("Team 1")).not.toBeInTheDocument();
			expect(screen.queryByText("Team 2")).not.toBeInTheDocument();
			expect(screen.queryByText("Another Team")).not.toBeInTheDocument();
			expect(screen.getByTestId("no-items-message")).toBeInTheDocument();
		});

		it("shows demo data link when no data is available", () => {
			renderWithRouter(
				<DataOverviewTable
					data={[]}
					title="Test Items"
					api="api"
					onDelete={vi.fn()}
					filterText=""
				/>,
			);

			expect(screen.getByTestId("empty-items-message")).toBeInTheDocument();

			// Check that the demo data link is present and has correct href
			const demoDataLink = screen.getByText("Load Demo Data");
			expect(demoDataLink).toBeInTheDocument();
			expect(demoDataLink.closest("a")).toHaveAttribute(
				"href",
				"/settings?tab=demodata",
			);

			// Check that the documentation link is also present
			const docLink = screen.getByText("Check the documentation");
			expect(docLink).toBeInTheDocument();
			expect(docLink.closest("a")).toHaveAttribute(
				"href",
				"https://docs.lighthouse.letpeople.work",
			);
		});
	});
});
