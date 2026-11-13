import { render, screen } from "@testing-library/react";
import {
	MemoryRouter,
	Routes,
	useLocation,
	useNavigationType,
} from "react-router";
import { describe, expect, it, vi } from "vitest";
import { settingsRoutes } from "./settingsRoutes";

vi.mock("../Connections/Edit/EditConnection", () => ({
	default: () => <div data-testid="edit-connection-page" />,
}));

const WhereWeAre = () => (
	<>
		<div data-testid="where">{useLocation().pathname}</div>
		<div data-testid="how">{useNavigationType()}</div>
	</>
);

const whenTheyOpen = (address: string) =>
	render(
		<MemoryRouter initialEntries={[address]}>
			<WhereWeAre />
			<Routes>{settingsRoutes()}</Routes>
		</MemoryRouter>,
	);

describe("old connection addresses", () => {
	it("keep the id as it was written, even when it is not a number", async () => {
		whenTheyOpen("/connections/abc/edit");

		expect(await screen.findByTestId("edit-connection-page")).toBeVisible();
		expect(screen.getByTestId("where")).toHaveTextContent(
			"/settings/system/connections/abc/edit",
		);
		expect(screen.getByTestId("how")).toHaveTextContent("REPLACE");
	});

	it("keep the id of a connection nobody has", async () => {
		whenTheyOpen("/connections/9999/edit");

		await screen.findByTestId("edit-connection-page");
		expect(screen.getByTestId("where")).toHaveTextContent(
			"/settings/system/connections/9999/edit",
		);
	});
});
