import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import type { UserEvent } from "@testing-library/user-event";
import { assert } from "vitest";
import WorkItemsDialog, {
	type WorkItemsDialogProps,
} from "../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { IFeature } from "../models/Feature";
import type { IWorkItem, IWorkItemEstimate } from "../models/WorkItem";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IFeatureService } from "../services/Api/FeatureService";
import {
	createMockApiServiceContext,
	createMockFeatureService,
} from "./MockApiServiceProvider";

/**
 * The columns every dialog shows whatever opened it. Owned by is not among them: it shows for
 * Feature rows only, so the specs name it wherever they expect it.
 */
export const FIXED_COLUMNS: readonly string[] = [
	"referenceId",
	"name",
	"type",
	"state",
];

export const STORY_POINTS = "Story Points";

export const estimateOf = (
	value: number,
	unit: string | null = STORY_POINTS,
	displayValue = String(value),
): IWorkItemEstimate => ({ value, displayValue, unit });

/** Estimation is set up for the owner, and this item has nothing the chart could place. */
export const noUsableEstimate = (
	unit: string | null = STORY_POINTS,
): IWorkItemEstimate => ({ value: null, displayValue: null, unit });

/** A calendar day at midday, so it is the same day in every zone the suite runs in. */
export const day = (isoDay: string): Date => new Date(`${isoDay}T12:00:00`);

let nextRowId = 1000;

export function aWorkItem(
	overrides: Partial<IWorkItem> & { referenceId: string },
): IWorkItem {
	nextRowId += 1;
	return {
		id: nextRowId,
		name: `Work on ${overrides.referenceId}`,
		state: "Done",
		stateCategory: "Done",
		type: "User Story",
		url: null,
		startedDate: day("2026-09-01"),
		closedDate: day("2026-09-20"),
		cycleTime: 20,
		workItemAge: 0,
		parentWorkItemReference: "",
		isBlocked: false,
		...overrides,
	};
}

export function aFeature(
	overrides: Partial<IFeature> & { referenceId: string },
): IFeature {
	return {
		...aWorkItem({ type: "Epic", ...overrides }),
		lastUpdated: day("2026-10-01"),
		isUsingDefaultFeatureSize: false,
		size: 10,
		owningTeam: "Team Zenith",
		remainingWork: {},
		totalWork: {},
		projects: [],
		forecasts: [],
		getRemainingWorkForFeature: () => 0,
		getRemainingWorkForTeam: () => 0,
		getTotalWorkForFeature: () => 0,
		getTotalWorkForTeam: () => 0,
		...overrides,
	} as IFeature;
}

export type DialogOptions = Partial<WorkItemsDialogProps> & {
	items: IWorkItem[];
	featureService?: IFeatureService;
};

function dialogTree(
	options: DialogOptions,
	queryClient: QueryClient,
): React.ReactElement {
	const { featureService = createMockFeatureService(), ...props } = options;
	return (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ featureService })}
			>
				<WorkItemsDialog
					title="Work Items behind the point"
					open
					onClose={() => {}}
					{...props}
				/>
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
}

export interface OpenedDialog {
	unmount: () => void;
	/** The same dialog handed other rows or another context while it stays mounted. */
	showInstead: (next: DialogOptions) => void;
}

/** Opens the dialog the way a chart, a widget or a Feature list does, with a fresh query cache. */
export function openTheDialog(options: DialogOptions): OpenedDialog {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const view = render(dialogTree(options, queryClient));
	return {
		unmount: view.unmount,
		showInstead: (next: DialogOptions) =>
			view.rerender(dialogTree(next, queryClient)),
	};
}

export const theDialog = () => screen.getByRole("dialog");

export const columnsShown = (): string[] =>
	within(theDialog())
		.queryAllByRole("columnheader")
		.map((header) => header.getAttribute("data-field") ?? "");

/** The columns shown besides the fixed ones, in the order they stand. */
export const contextColumnsShown = (): string[] =>
	columnsShown().filter((field) => !FIXED_COLUMNS.includes(field));

export const columnHeader = (field: string): HTMLElement => {
	const header = within(theDialog())
		.queryAllByRole("columnheader")
		.find((candidate) => candidate.getAttribute("data-field") === field);
	if (!header) {
		assert.fail(`No ${field} column; shown: ${columnsShown().join(", ")}`);
	}
	return header;
};

export const headerText = (field: string): string =>
	columnHeader(field)
		.querySelector(".MuiDataGrid-columnHeaderTitle")
		?.textContent?.trim() ?? "";

export const sortedDescendingBy = (): string | null =>
	within(theDialog())
		.queryAllByRole("columnheader")
		.find((header) => header.getAttribute("aria-sort") === "descending")
		?.getAttribute("data-field") ?? null;

const rowsShown = (): HTMLElement[] => [
	...theDialog().querySelectorAll<HTMLElement>(".MuiDataGrid-row"),
];

export const rowOrder = (): string[] =>
	rowsShown().map(
		(row) =>
			row.querySelector('[data-field="referenceId"]')?.textContent?.trim() ??
			"",
	);

export const cellIn = (field: string, referenceId: string): HTMLElement => {
	const row = rowsShown().find(
		(candidate) =>
			candidate.querySelector('[data-field="referenceId"]')?.textContent ===
			referenceId,
	);
	const cell = row?.querySelector<HTMLElement>(`[data-field="${field}"]`);
	if (!cell) {
		assert.fail(`No ${field} cell on the row of ${referenceId}`);
	}
	return cell;
};

export const cellText = (field: string, referenceId: string): string =>
	cellIn(field, referenceId).textContent?.trim() ?? "";

/** Where the grid stands: the dialog by default, or the page for a grid shown on its own. */
export type GridScope = () => HTMLElement;

export async function openManageColumns(
	user: UserEvent,
	scope: GridScope = theDialog,
): Promise<void> {
	await user.click(within(scope()).getByLabelText("Name column menu"));
	await user.click(
		await screen.findByRole("menuitem", { name: "Manage columns" }),
	);
}

/** What Manage columns lists, in its order, and whether each is on. */
export const columnsOffered = (): { field: string; shown: boolean }[] =>
	screen
		.queryAllByRole("checkbox")
		.map((box) => ({
			field: box.getAttribute("name") ?? "",
			shown: (box as HTMLInputElement).checked,
		}))
		.filter(({ field }) => field !== "" && field !== "Show/Hide All");

export const catalogueOffered = () =>
	columnsOffered().filter(({ field }) => !FIXED_COLUMNS.includes(field));

export async function turnColumn(
	user: UserEvent,
	field: string,
	shown: boolean,
	scope: GridScope = theDialog,
): Promise<void> {
	await openManageColumns(user, scope);
	const box = screen
		.getAllByRole("checkbox")
		.find((candidate) => candidate.getAttribute("name") === field);
	if (!box) {
		assert.fail(`Manage columns does not offer ${field}`);
	}
	if ((box as HTMLInputElement).checked !== shown) {
		await user.click(box);
	}
	await user.keyboard("{Escape}");
}

export async function resetTheLayout(
	user: UserEvent,
	scope: GridScope = theDialog,
): Promise<void> {
	await user.click(within(scope()).getByTestId("reset-layout-button"));
}
