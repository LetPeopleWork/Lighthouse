import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockRbacService,
} from "../../../tests/MockApiServiceProvider";
import { SettingsMenu } from "./SettingsMenu";

const WhereWeAre = () => (
	<div data-testid="where">{useLocation().pathname}</div>
);

const renderMenuAt = (address: string) => {
	const rbacService = createMockRbacService();
	rbacService.getAuthorizationSummary = vi.fn().mockResolvedValue({
		isRbacEnabled: true,
		isSystemAdmin: true,
		canCreateTeam: true,
		canCreatePortfolio: true,
	});
	return render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ rbacService })}
		>
			<MemoryRouter initialEntries={[address]}>
				<WhereWeAre />
				<Routes>
					<Route path="*" element={<SettingsMenu />} />
				</Routes>
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);
};

describe("SettingsMenu", () => {
	it("opens the menu, goes to the chosen area and closes again", async () => {
		renderMenuAt("/overview");
		const button = screen.getByTestId("settings-menu");
		expect(button).toHaveAttribute("aria-expanded", "false");

		fireEvent.click(button);
		expect(button).toHaveAttribute("aria-expanded", "true");
		fireEvent.click(await screen.findByTestId("settings-area-system"));

		expect(screen.getByTestId("where")).toHaveTextContent("/settings/system");
		expect(button).toHaveAttribute("aria-expanded", "false");
		await waitFor(() =>
			expect(screen.queryByRole("menu")).not.toBeInTheDocument(),
		);
	});

	it("closes on Escape without going anywhere", async () => {
		renderMenuAt("/overview");
		fireEvent.click(screen.getByTestId("settings-menu"));

		fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });

		await waitFor(() =>
			expect(screen.queryByRole("menu")).not.toBeInTheDocument(),
		);
		expect(screen.getByTestId("where")).toHaveTextContent("/overview");
	});

	it("is marked as the current page only on a settings page", () => {
		const { unmount } = renderMenuAt("/settings/system/connections");
		expect(screen.getByTestId("settings-menu")).toHaveStyle({
			fontWeight: "700",
			borderBottom: "2px solid rgb(25, 118, 210)",
		});
		unmount();

		renderMenuAt("/overview");
		expect(screen.getByTestId("settings-menu")).toHaveStyle({
			fontWeight: "400",
			borderBottom: "2px solid rgba(0, 0, 0, 0)",
		});
	});
});
