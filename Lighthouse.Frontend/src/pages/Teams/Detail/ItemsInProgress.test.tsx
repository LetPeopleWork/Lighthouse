import { fireEvent, render, screen } from "@testing-library/react";
import { vi } from "vitest";

// Mock the WorkItemsDialog used by the component so we can assert props
vi.mock("../../../components/Common/WorkItemsDialog/WorkItemsDialog", () => ({
	default: ({
		title,
		items,
		open,
		highlightColumn,
		sle,
	}: {
		title?: string;
		items?: unknown[];
		open?: boolean;
		highlightColumn?: HighlightColumnDefinition;
		sle?: number;
	}) => {
		if (!open) return null;
		return (
			<div data-testid="mock-dialog">
				<div>Title: {title}</div>
				<div>Items: {Array.isArray(items) ? items.length : 0}</div>
				<div>Highlighted Column: {highlightColumn?.title}</div>
				<div>SLE: {String(sle)}</div>
			</div>
		);
	},
}));

// Mock the terminology hook to return a predictable term
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({ getTerm: () => "Age" }),
}));

import type { HighlightColumnDefinition } from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { IWorkItem } from "../../../models/WorkItem";
import ItemsInProgress from "./ItemsInProgress";

describe("ItemsInProgress", () => {
	test("renders nothing when no entries provided", () => {
		const { container } = render(<ItemsInProgress entries={[]} />);
		// When there are no entries the component should render an empty fragment
		expect(container).toBeEmptyDOMElement();
	});

	test("renders entries and opens dialog on click", async () => {
		const makeItem = (id: number, age?: number): IWorkItem =>
			({
				id: id.toString(),
				name: `Item ${id}`,
				state: "New",
				stateCategory: "InProgress",
				type: "Task",
				featureId: undefined,
				teamId: undefined,
				workItemAge: age ?? 0,
				// include optional fields used elsewhere; keep minimal
			}) as unknown as IWorkItem;

		const entries: {
			title: string;
			items: IWorkItem[];
			idealWip?: number;
			sle?: number;
		}[] = [
			{
				title: "Todo",
				items: [makeItem(1, 5)],
				idealWip: 2,
				sle: 7,
			},
			{
				title: "Doing",
				items: [],
				idealWip: 1,
			},
		];

		render(<ItemsInProgress entries={entries} />);

		// Titles and counts should be visible
		expect(screen.getByText("Todo")).toBeInTheDocument();
		expect(screen.getByText("Doing")).toBeInTheDocument();

		expect(screen.getByText("1")).toBeInTheDocument();
		// Goal chip should appear
		expect(screen.getByText("Goal: 2")).toBeInTheDocument();

		// Click the first entry to open the dialog
		fireEvent.click(screen.getByText("Todo"));

		// The mocked dialog should be rendered with the expected props
		const dialog = await screen.findByTestId("mock-dialog");
		expect(dialog).toBeInTheDocument();
		expect(dialog).toHaveTextContent("Title: Todo");
		expect(dialog).toHaveTextContent("Items: 1");
		// Highlighted column title is provided by the mocked terminology hook
		expect(dialog).toHaveTextContent("Highlighted Column: Age");
		expect(dialog).toHaveTextContent("SLE: 7");
	});
});
