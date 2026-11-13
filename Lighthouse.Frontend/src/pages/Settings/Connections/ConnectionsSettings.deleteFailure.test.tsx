import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { WorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import ConnectionsSettings from "./ConnectionsSettings";

describe("deleting a connection fails for a reason the server does not give", () => {
	it("says so in this instance's own word for a Work Tracking System and keeps the dialog open", async () => {
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
		).mockRejectedValue(new Error("network down"));
		const terminologyService = createMockTerminologyService();
		vi.mocked(terminologyService.getAllTerminology).mockResolvedValue([
			{
				id: 1,
				key: TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM,
				defaultValue: "Work Tracking System",
				description: "",
				value: "Data Source",
			},
		]);
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

		expect(
			await within(dialog).findByText(
				"Failed to delete Data Source. Please try again.",
			),
		).toBeVisible();
	});
});
