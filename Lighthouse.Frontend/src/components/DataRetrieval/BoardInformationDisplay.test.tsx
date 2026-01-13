import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import BoardInformationDisplay from "./BoardInformationDisplay";

describe("BoardInformationDisplay", () => {
	const mockBoardInformation: IBoardInformation = {
		dataRetrievalValue: "project = PROJ AND sprint = 123",
		workItemTypes: ["Story", "Bug", "Task"],
		toDoStates: ["To Do", "Backlog"],
		doingStates: ["In Progress", "In Review"],
		doneStates: ["Done", "Closed"],
	};

	it("shows loading state with 'Loading Board Information' text", () => {
		render(<BoardInformationDisplay boardInformation={null} loading={true} />);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(screen.getByText("Loading Board Information")).toBeInTheDocument();
	});

	it("shows nothing when boardInformation is null and not loading", () => {
		render(<BoardInformationDisplay boardInformation={null} loading={false} />);

		expect(screen.queryByText("Board Information")).not.toBeInTheDocument();
	});

	it("displays board information when loaded", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		expect(screen.getByText("Board Information")).toBeInTheDocument();
		expect(
			screen.getByText("project = PROJ AND sprint = 123"),
		).toBeInTheDocument();
	});

	it("displays JQL in code block style", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		const codeElement = screen.getByText("project = PROJ AND sprint = 123");
		expect(codeElement.tagName).toBe("CODE");
	});

	it("displays work item types as chips", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		expect(screen.getByText("Work Item Types")).toBeInTheDocument();
		expect(screen.getByText("Story")).toBeInTheDocument();
		expect(screen.getByText("Bug")).toBeInTheDocument();
		expect(screen.getByText("Task")).toBeInTheDocument();
	});

	it("displays To Do states as chips", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		expect(screen.getByText("To Do States")).toBeInTheDocument();
		expect(screen.getByText("To Do")).toBeInTheDocument();
		expect(screen.getByText("Backlog")).toBeInTheDocument();
	});

	it("displays Doing states as chips", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		expect(screen.getByText("Doing States")).toBeInTheDocument();
		expect(screen.getByText("In Progress")).toBeInTheDocument();
		expect(screen.getByText("In Review")).toBeInTheDocument();
	});

	it("displays Done states as chips", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		expect(screen.getByText("Done States")).toBeInTheDocument();
		expect(screen.getByText("Done")).toBeInTheDocument();
		expect(screen.getByText("Closed")).toBeInTheDocument();
	});

	it("shows 'None Configured' when work item types array is empty", () => {
		const emptyWorkItemTypes: IBoardInformation = {
			...mockBoardInformation,
			workItemTypes: [],
		};

		render(
			<BoardInformationDisplay
				boardInformation={emptyWorkItemTypes}
				loading={false}
			/>,
		);

		const noneConfiguredElements = screen.getAllByText("None Configured");
		expect(noneConfiguredElements.length).toBeGreaterThan(0);
	});

	it("shows 'None Configured' when To Do states array is empty", () => {
		const emptyToDoStates: IBoardInformation = {
			...mockBoardInformation,
			toDoStates: [],
		};

		render(
			<BoardInformationDisplay
				boardInformation={emptyToDoStates}
				loading={false}
			/>,
		);

		const noneConfiguredElements = screen.getAllByText("None Configured");
		expect(noneConfiguredElements.length).toBeGreaterThan(0);
	});

	it("shows 'None Configured' when Doing states array is empty", () => {
		const emptyDoingStates: IBoardInformation = {
			...mockBoardInformation,
			doingStates: [],
		};

		render(
			<BoardInformationDisplay
				boardInformation={emptyDoingStates}
				loading={false}
			/>,
		);

		const noneConfiguredElements = screen.getAllByText("None Configured");
		expect(noneConfiguredElements.length).toBeGreaterThan(0);
	});

	it("shows 'None Configured' when Done states array is empty", () => {
		const emptyDoneStates: IBoardInformation = {
			...mockBoardInformation,
			doneStates: [],
		};

		render(
			<BoardInformationDisplay
				boardInformation={emptyDoneStates}
				loading={false}
			/>,
		);

		const noneConfiguredElements = screen.getAllByText("None Configured");
		expect(noneConfiguredElements.length).toBeGreaterThan(0);
	});

	it("displays info message about adjusting data after closing wizard", () => {
		render(
			<BoardInformationDisplay
				boardInformation={mockBoardInformation}
				loading={false}
			/>,
		);

		expect(
			screen.getByText(
				/You can adjust this configuration after closing the wizard/i,
			),
		).toBeInTheDocument();
	});

	it("shows 'None Configured' for empty JQL string", () => {
		const emptyJql: IBoardInformation = {
			...mockBoardInformation,
			dataRetrievalValue: "",
		};

		render(
			<BoardInformationDisplay boardInformation={emptyJql} loading={false} />,
		);

		const noneConfiguredElements = screen.getAllByText("None Configured");
		expect(noneConfiguredElements.length).toBeGreaterThan(0);
	});

	it("shows all sections with 'None Configured' when all arrays are empty", () => {
		const allEmpty: IBoardInformation = {
			dataRetrievalValue: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
		};

		render(
			<BoardInformationDisplay boardInformation={allEmpty} loading={false} />,
		);

		const noneConfiguredElements = screen.getAllByText("None Configured");
		expect(noneConfiguredElements.length).toBe(5); // JQL, WorkItemTypes, To Do, Doing, Done
	});
});
