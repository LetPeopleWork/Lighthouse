import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, useLocation } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { UserAuthorizationSummary } from "../../models/Authorization/RbacModels";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../../tests/MockApiServiceProvider";
import { settingsRoutes } from "./settingsRoutes";
// The routes load this page lazily; importing it here keeps that load out of the timed tests.
import "./SettingsAreaPage";

const WhereWeAre = () => (
	<div data-testid="where">{useLocation().pathname}</div>
);

const whenTheyOpen = (
	address: string,
	summary: Promise<UserAuthorizationSummary>,
) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn(() => summary);
	render(
		<QueryClientProvider client={new QueryClient()}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ rbacService })}
			>
				<MemoryRouter initialEntries={[address]}>
					<WhereWeAre />
					<Routes>{settingsRoutes()}</Routes>
				</MemoryRouter>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

const systemAdmin = (): Promise<UserAuthorizationSummary> =>
	Promise.resolve({
		isRbacEnabled: true,
		isSystemAdmin: true,
		canCreateTeam: true,
		canCreatePortfolio: true,
	});

describe("the settings address", () => {
	it("moves an old ?tab= key on the bare address to its tab", async () => {
		whenTheyOpen("/settings?tab=SystemInfo", systemAdmin());

		expect(
			await screen.findByText("/settings/system/system-info"),
		).toBeInTheDocument();
	});

	it("moves the bare address without ?tab= to the first tab", async () => {
		whenTheyOpen("/settings", systemAdmin());

		expect(
			await screen.findByText("/settings/organization/terminology"),
		).toBeInTheDocument();
	});

	it("ignores ?tab= on an area's address", async () => {
		whenTheyOpen("/settings/system?tab=rbac", systemAdmin());

		expect(
			await screen.findByText("/settings/system/connections"),
		).toBeInTheDocument();
	});

	it("shows a spinner and stays put while access is loading", () => {
		whenTheyOpen("/settings/system", new Promise(() => {}));

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(screen.getByTestId("where")).toHaveTextContent("/settings/system");
	});
});
