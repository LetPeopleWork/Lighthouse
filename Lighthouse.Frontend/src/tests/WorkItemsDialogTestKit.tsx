import { waitFor, within } from "@testing-library/react";
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
import { renderWithQueryClient } from "./renderWithQueryClient";

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
	context: WorkItemsDialogProps["context"];
	featureService?: IFeatureService;
};

function dialogTree(options: DialogOptions): React.ReactElement {
	const {
		featureService = createMockFeatureService(),
		ownerKind = "team",
		...props
	} = options;
	return (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ featureService })}
		>
			<WorkItemsDialog
				title="Work Items behind the point"
				open
				onClose={() => {}}
				ownerKind={ownerKind}
				{...props}
			/>
		</ApiServiceContext.Provider>
	);
}

export interface OpenedDialog {
	unmount: () => void;
	/** The same dialog handed other rows or another context while it stays mounted. */
	showInstead: (next: DialogOptions) => void;
}

/** Opens the dialog the way a chart, a widget or a Feature list does, with a fresh query cache. */
export function openTheDialog(options: DialogOptions): OpenedDialog {
	const view = renderWithQueryClient(dialogTree(options));
	return {
		unmount: view.unmount,
		showInstead: (next: DialogOptions) => view.rerender(dialogTree(next)),
	};
}

/**
 * The dialog, the grid and its menus are found by their role attribute rather than by role
 * queries: a role query asks jsdom for the computed style of every match and its ancestors, and
 * the stylesheet the grid brings makes each of those lookups cost milliseconds.
 */
const byRole = (scope: ParentNode, role: string): HTMLElement[] => [
	...scope.querySelectorAll<HTMLElement>(`[role="${role}"]`),
];

export const theDialog = (): HTMLElement => {
	const dialogs = byRole(document, "dialog");
	if (dialogs.length !== 1) {
		assert.fail(`Expected one dialog, found ${dialogs.length}`);
	}
	return dialogs[0];
};

const headersShown = (): HTMLElement[] => byRole(theDialog(), "columnheader");

export const columnsShown = (): string[] =>
	headersShown().map((header) => header.dataset.field ?? "");

/** The columns shown besides the fixed ones, in the order they stand. */
export const contextColumnsShown = (): string[] =>
	columnsShown().filter((field) => !FIXED_COLUMNS.includes(field));

export const columnHeader = (field: string): HTMLElement => {
	const header = headersShown().find(
		(candidate) => candidate.dataset.field === field,
	);
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
	headersShown().find(
		(header) => header.getAttribute("aria-sort") === "descending",
	)?.dataset.field ?? null;

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
	const manageColumns = await waitFor(() => {
		const item = byRole(document, "menuitem").find(
			(candidate) => candidate.textContent?.trim() === "Manage columns",
		);
		if (!item) {
			throw new Error("The column menu offers no Manage columns");
		}
		return item;
	});
	await user.click(manageColumns);
}

/** A switch is a checkbox input too, but it never stands for a column. */
const checkboxesOffered = (): HTMLInputElement[] => [
	...document.querySelectorAll<HTMLInputElement>(
		'input[type="checkbox"]:not([role])',
	),
];

/** What Manage columns lists, in its order, and whether each is on. */
export const columnsOffered = (): { field: string; shown: boolean }[] =>
	checkboxesOffered()
		.map((box) => ({
			field: box.getAttribute("name") ?? "",
			shown: box.checked,
		}))
		.filter(({ field }) => field !== "" && field !== "Show/Hide All");

export const catalogueOffered = () =>
	columnsOffered().filter(({ field }) => !FIXED_COLUMNS.includes(field));

/** Turns several columns on or off in one visit to Manage columns, in the order given. */
export async function turnColumns(
	user: UserEvent,
	changes: Record<string, boolean>,
	scope: GridScope = theDialog,
): Promise<void> {
	await openManageColumns(user, scope);
	for (const [field, shown] of Object.entries(changes)) {
		const box = checkboxesOffered().find(
			(candidate) => candidate.getAttribute("name") === field,
		);
		if (!box) {
			assert.fail(`Manage columns does not offer ${field}`);
		}
		if (box.checked !== shown) {
			await user.click(box);
		}
	}
	await user.keyboard("{Escape}");
}

export async function turnColumn(
	user: UserEvent,
	field: string,
	shown: boolean,
	scope: GridScope = theDialog,
): Promise<void> {
	await turnColumns(user, { [field]: shown }, scope);
}

export async function resetTheLayout(
	user: UserEvent,
	scope: GridScope = theDialog,
): Promise<void> {
	await user.click(within(scope()).getByTestId("reset-layout-button"));
}
