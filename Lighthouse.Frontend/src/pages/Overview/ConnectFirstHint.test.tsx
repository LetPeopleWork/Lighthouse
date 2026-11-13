import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ConnectFirstHint from "./ConnectFirstHint";

let isSystemAdmin = true;
vi.mock("../../hooks/useRbac", () => ({
	useRbac: () => ({ isLoading: false, isSystemAdmin }),
}));

const theHint = (props: {
	hasConnections?: boolean;
	connectionsRefused?: boolean;
}) =>
	render(
		<MemoryRouter>
			<ConnectFirstHint
				hasConnections={props.hasConnections ?? false}
				connectionsRefused={props.connectionsRefused ?? false}
				connectionTerm="Data Source"
			/>
		</MemoryRouter>,
	);

describe("the line saying where to connect the first connection", () => {
	beforeEach(() => {
		isSystemAdmin = true;
	});

	it("points a System Admin to System, Connections while none exists", () => {
		theHint({});

		expect(
			screen.getByRole("link", {
				name: /Settings\s*›\s*System\s*›\s*Connections/,
			}),
		).toHaveAttribute("href", "/settings/system/connections");
		expect(screen.getByText(/Connect a Data Source first:/)).toBeVisible();
	});

	it("is silent when the list of connections was refused, because nobody knows there are none", () => {
		theHint({ connectionsRefused: true });

		expect(screen.queryByText(/Connect a Data Source first/)).toBeNull();
	});

	it("is silent once a connection exists", () => {
		theHint({ hasConnections: true });

		expect(screen.queryByText(/Connect a Data Source first/)).toBeNull();
	});

	it("is silent for a reader, who cannot open System, Connections", () => {
		isSystemAdmin = false;
		theHint({});

		expect(screen.queryByText(/Connect a Data Source first/)).toBeNull();
	});
});
