import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type React from "react";
import { describe, expect, it } from "vitest";
import type { RbacState } from "../../hooks/useRbac";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	aReportOn,
	confirmLineOf,
	GRAVITY,
	notAdminOfSome,
	type Owner,
	PERMISSIONS_COULD_NOT_LOAD,
	PULSAR,
	permissionsOfAdminOf,
	QUARTER,
	removeButton,
	VOYAGER,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import { aReportService } from "../../tests/ReportsTestKit";
import type { RowPicks } from "./useRowPicks";
import { useSelectionRemoval } from "./useSelectionActions";

/** Remove on the Reports toolbar: when it is held back and why, and that pressing it asks before removing. */

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 801 });
const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 802 });
const pulsarsQuarter = aReportOn(PULSAR, QUARTER, { id: 804 });
const voyagersQuarter = aReportOn(VOYAGER, QUARTER, { id: 805 });

interface Permissions {
	readonly adminOf?: readonly Owner[];
	readonly isLoading?: boolean;
	readonly loadFailed?: boolean;
}

/** What the permissions hook answers; when they could not be loaded it answers as if she were Admin everywhere. */
const rbacOf = ({
	adminOf = [],
	isLoading = false,
	loadFailed = false,
}: Permissions): RbacState => {
	const isAdminOf = (kind: Owner["kind"], id: number) =>
		loadFailed ||
		adminOf.some((owner) => owner.kind === kind && owner.id === id);
	return {
		isLoading,
		loadFailed,
		retry: () => undefined,
		isRbacEnabled: true,
		isSystemAdmin: false,
		canCreateTeam: false,
		canCreatePortfolio: false,
		isTeamAdmin: (id) => isAdminOf("team", id),
		isPortfolioAdmin: (id) => isAdminOf("portfolio", id),
		summary: permissionsOfAdminOf(adminOf),
	};
};

const picksOf = (picked: IReportAcrossOwners[]): RowPicks => ({
	picking: {
		selectedIds: picked.map((report) => report.id),
		onSelectionChange: () => undefined,
		tableOrder: { current: undefined },
	},
	picked,
	inTableOrder: () => picked,
	clear: () => undefined,
	unpick: () => undefined,
});

const Toolbar: React.FC<
	Readonly<{ picks: RowPicks; permissions: RbacState }>
> = ({ picks, permissions }) => {
	const { button, dialog } = useSelectionRemoval(picks, permissions);
	return (
		<>
			{button}
			{dialog}
		</>
	);
};

const renderTheToolbar = (
	picked: IReportAcrossOwners[],
	permissions: Permissions,
) => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const services = createMockApiServiceContext({
		reportService: aReportService(),
	});
	render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={services}>
				<Toolbar picks={picksOf(picked)} permissions={rbacOf(permissions)} />
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
	return userEvent.setup();
};

const removeButtonFor = (count: number) =>
	screen.getByRole("button", { name: removeButton(count) });

const theReasonOnHover = async (
	user: ReturnType<typeof userEvent.setup>,
	remove: HTMLElement,
) => {
	await user.hover(remove.parentElement ?? remove);
	return screen.findByRole("tooltip");
};

describe("Removing the picked reports from the toolbar", () => {
	it("with one picked report on an owner she only reads, Remove is disabled and says why", async () => {
		const user = renderTheToolbar([zenithsQuarter, pulsarsQuarter], {
			adminOf: [ZENITH],
		});

		const remove = removeButtonFor(2);
		expect(remove).toBeDisabled();
		expect(await theReasonOnHover(user, remove)).toHaveTextContent(
			new RegExp(`^${notAdminOfSome(1, 2).replaceAll(".", "\\.")}$`),
		);
	});

	it("with two picked reports on owners she only reads, the reason says they are", async () => {
		const user = renderTheToolbar(
			[zenithsQuarter, pulsarsQuarter, voyagersQuarter],
			{ adminOf: [ZENITH] },
		);

		const remove = removeButtonFor(3);
		expect(remove).toBeDisabled();
		expect(await theReasonOnHover(user, remove)).toHaveTextContent(
			new RegExp(`^${notAdminOfSome(2, 3).replaceAll(".", "\\.")}$`),
		);
	});

	it("when her permissions could not be loaded, Remove is disabled and says to reload", async () => {
		const user = renderTheToolbar([zenithsQuarter], { loadFailed: true });

		const remove = removeButtonFor(1);
		expect(remove).toBeDisabled();
		expect(await theReasonOnHover(user, remove)).toHaveTextContent(
			new RegExp(`^${PERMISSIONS_COULD_NOT_LOAD.replaceAll(".", "\\.")}$`),
		);
	});

	it("while her permissions are loading, Remove is disabled", () => {
		renderTheToolbar([zenithsQuarter], { isLoading: true });

		expect(removeButtonFor(1)).toBeDisabled();
	});

	it("pressing an enabled Remove opens the confirmation listing the picked reports", async () => {
		const user = renderTheToolbar([zenithsQuarter, gravitysQuarter], {
			adminOf: [ZENITH, GRAVITY],
		});

		await user.click(removeButtonFor(2));

		expect(
			within(screen.getByRole("dialog"))
				.getAllByRole("listitem")
				.map((item) => item.textContent),
		).toEqual([
			confirmLineOf(QUARTER, ZENITH),
			confirmLineOf(QUARTER, GRAVITY),
		]);
	});

	it("pressing a disabled Remove asks nothing", () => {
		renderTheToolbar([zenithsQuarter, gravitysQuarter], { loadFailed: true });

		fireEvent.click(removeButtonFor(2));

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});
});
