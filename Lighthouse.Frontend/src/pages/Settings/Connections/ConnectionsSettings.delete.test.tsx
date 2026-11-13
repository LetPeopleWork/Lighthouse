import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import ConnectionsSettings from "./ConnectionsSettings";

const STILL_IN_USE = "Northwind ADO is still used by 2 Teams.";

describe("deleting a connection the server refuses", () => {
	it("keeps the dialog open with the server's reason and the connection listed", async () => {
		const workTrackingSystemService = createMockWorkTrackingSystemService();
		vi.mocked(
			workTrackingSystemService.getConfiguredWorkTrackingSystems,
		).mockResolvedValue([
			new WorkTrackingSystemConnection({
				id: 2,
				name: "Northwind ADO",
				workTrackingSystem: "AzureDevOps",
				options: [],
				authenticationMethodKey: "ado.pat",
			}),
		]);
		vi.mocked(
			workTrackingSystemService.deleteWorkTrackingSystemConnection,
		).mockRejectedValue(new ApiError(409, STILL_IN_USE));
		const terminologyService = createMockTerminologyService();
		vi.mocked(terminologyService.getAllTerminology).mockResolvedValue([]);
		render(
			<QueryClientProvider client={new QueryClient()}>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({
						workTrackingSystemService,
						terminologyService,
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

		fireEvent.click(await screen.findByTestId("delete-connection-button"));
		const dialog = await screen.findByRole("dialog");
		fireEvent.click(within(dialog).getByRole("button", { name: "Delete" }));

		expect(await within(dialog).findByText(STILL_IN_USE)).toBeVisible();
		expect(
			// The open dialog hides the page behind it from the accessibility tree.
			screen.getByRole("link", { name: "Northwind ADO", hidden: true }),
			"A connection the server refused to delete stays on the list",
		).toBeInTheDocument();
	});
});
