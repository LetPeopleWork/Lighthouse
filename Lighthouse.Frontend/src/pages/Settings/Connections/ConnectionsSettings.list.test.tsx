import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IWorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import {
	givenANarrowWindow,
	givenTextIsWiderThanItsColumn,
	resetTheCut,
	resetTheWindow,
	thenALongTextIsCutToItsColumn,
	thenHoveringOverItOpensNoTooltip,
	thenTheActionsColumnIsPinnedAndLast,
	thenTheColumnIsShownWhole,
	thenTheFullTextIsInATooltip,
	thenTheTableSaysColumnsAreOutOfSightOnItsRight,
	whenThePersonHoversOver,
	whenThePersonReachesWithTheKeyboard,
} from "../../../tests/SharedGridFitKit";
import ConnectionsSettings from "./ConnectionsSettings";

vi.mock("@mui/x-data-grid", async () =>
	(await import("../../../tests/SharedGridFitKit")).dataGridWithItsBoxStated(),
);

const northwind = () =>
	new WorkTrackingSystemConnection({
		id: 2,
		name: "Northwind ADO",
		workTrackingSystem: "AzureDevOps",
		options: [],
		authenticationMethodKey: "ado.pat",
	});

const renderSettings = (service: IWorkTrackingSystemService) =>
	render(
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					workTrackingSystemService: service,
				})}
			>
				<TerminologyProvider>
					<MemoryRouter>
						<ConnectionsSettings />
					</MemoryRouter>
				</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);

const serviceListing = (connections: WorkTrackingSystemConnection[]) => {
	const service = createMockWorkTrackingSystemService();
	vi.mocked(service.getConfiguredWorkTrackingSystems).mockResolvedValue(
		connections,
	);
	return service;
};

const openDeleteDialog = async () => {
	fireEvent.click(await screen.findByTestId("delete-connection-button"));
	return screen.findByRole("dialog");
};

describe("ConnectionsSettings", () => {
	it("says how to start when there is no connection, and shows no list", async () => {
		renderSettings(serviceListing([]));

		expect(await screen.findByTestId("no-connections-alert")).toHaveTextContent(
			"No Work Tracking System found. Load Demo Data or Check the documentation for more information.",
		);
		expect(
			screen.queryByTestId("connections-datagrid-container"),
		).not.toBeInTheDocument();
	});

	it("names what it is loading while the list is on its way", () => {
		const service = createMockWorkTrackingSystemService();
		vi.mocked(service.getConfiguredWorkTrackingSystems).mockReturnValue(
			new Promise(() => {}),
		);
		renderSettings(service);

		expect(
			screen.getByRole("progressbar", {
				name: "Loading the Work Tracking Systems",
			}),
		).toBeInTheDocument();
	});

	it("closes the dialog and reads the list again after a delete", async () => {
		const service = serviceListing([northwind()]);
		vi.mocked(service.deleteWorkTrackingSystemConnection).mockResolvedValue();
		renderSettings(service);

		const dialog = await openDeleteDialog();
		fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		await waitFor(() =>
			expect(service.getConfiguredWorkTrackingSystems).toHaveBeenCalledTimes(2),
		);
	});

	it("closes the dialog on Cancel without deleting", async () => {
		const service = serviceListing([northwind()]);
		renderSettings(service);

		const dialog = await openDeleteDialog();
		fireEvent.click(within(dialog).getByRole("button", { name: "Cancel" }));

		await waitFor(() =>
			expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
		);
		expect(service.deleteWorkTrackingSystemConnection).not.toHaveBeenCalled();
	});

	it("gives the general failure message when the server fails for another reason than a conflict", async () => {
		const service = serviceListing([northwind()]);
		vi.mocked(service.deleteWorkTrackingSystemConnection).mockRejectedValue(
			new ApiError(500, "Internal details nobody should read"),
		);
		renderSettings(service);

		const dialog = await openDeleteDialog();
		fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

		expect(
			await within(dialog).findByText(
				"Failed to delete Work Tracking System. Please try again.",
			),
		).toBeVisible();
		expect(
			within(dialog).queryByText("Internal details nobody should read"),
		).not.toBeInTheDocument();
	});
});

describe("the Connections table fits its columns, pins its actions and says when some are out of sight", () => {
	const LONG_NAME =
		"Northwind Azure DevOps organisation for the platform engineering department in the northern region";

	beforeEach(() => {
		givenANarrowWindow();
	});

	afterEach(() => {
		resetTheCut();
		resetTheWindow();
		localStorage.clear();
	});

	const whenTheConnectionsShow = async () => {
		const longNamed = new WorkTrackingSystemConnection({
			id: 3,
			name: LONG_NAME,
			workTrackingSystem: "AzureDevOps",
			options: [],
			authenticationMethodKey: "ado.pat",
		});
		const view = renderSettings(serviceListing([longNamed]));
		await screen.findByText(LONG_NAME);
		return view;
	};

	it("cuts a long connection name to its column and shows the type whole", async () => {
		givenTextIsWiderThanItsColumn();
		const { container } = await whenTheConnectionsShow();

		thenALongTextIsCutToItsColumn(container, "name");
		thenTheColumnIsShownWhole(container, "workTrackingSystem");
	});

	it("pins the actions column as the last column and says columns are out of sight", async () => {
		const { container } = await whenTheConnectionsShow();

		thenTheActionsColumnIsPinnedAndLast(container, "actions");
		thenTheTableSaysColumnsAreOutOfSightOnItsRight(container);
	});

	it("shows the full connection name in a tooltip when the person hovers over a cut name", async () => {
		givenTextIsWiderThanItsColumn();
		await whenTheConnectionsShow();

		whenThePersonHoversOver(screen.getByText(LONG_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});

	it("shows the full connection name in a tooltip when the person reaches a cut name with the keyboard", async () => {
		givenTextIsWiderThanItsColumn();
		await whenTheConnectionsShow();

		whenThePersonReachesWithTheKeyboard(screen.getByText(LONG_NAME));

		await thenTheFullTextIsInATooltip(LONG_NAME);
	});

	it("opens no tooltip over a connection name that fits its column", async () => {
		await whenTheConnectionsShow();

		await thenHoveringOverItOpensNoTooltip(screen.getByText(LONG_NAME));
	});
});
