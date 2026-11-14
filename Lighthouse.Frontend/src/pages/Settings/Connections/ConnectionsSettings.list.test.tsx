import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IWorkTrackingSystemService } from "../../../services/Api/WorkTrackingSystemService";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import ConnectionsSettings from "./ConnectionsSettings";

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
