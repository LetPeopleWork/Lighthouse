import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import { ReportCreationOutcomesSchema } from "../../models/Reports/ReportsAcrossOwners";
import answer from "../../tests/bulkCreationAnswer.json";
import BulkResult from "./BulkResult";

// The answer is the server's own: a backend acceptance test fails when this copy stops matching what it sends.
const theOutcomesOnTheWire =
	ReportCreationOutcomesSchema.parse(answer).outcomes;

const renderTheResult = () =>
	render(
		<MemoryRouter>
			<BulkResult
				count={theOutcomesOnTheWire.length}
				minimumLengthDays={14}
				run={{ status: "success", outcomes: theOutcomesOnTheWire }}
				pickedNames={new Map()}
				onDone={() => {}}
			/>
		</MemoryRouter>,
	);

describe("The bulk creation result, read from the server's own answer", () => {
	it("names each owner as it is called, its kind shown by an icon rather than a word before the name", () => {
		renderTheResult();

		const zenith = screen.getByText("Zenith", { exact: true }).closest("li");
		const apollo = screen.getByText("Apollo", { exact: true }).closest("li");
		expect(zenith).not.toBeNull();
		expect(apollo).not.toBeNull();
		expect(
			within(zenith as HTMLElement).getByTitle("Team"),
		).toBeInTheDocument();
		expect(
			within(apollo as HTMLElement).getByTitle("Portfolio"),
		).toBeInTheDocument();
		expect(screen.queryByText("Team Zenith", { exact: true })).toBeNull();
	});

	it("keeps a not-created owner's reason on its row", () => {
		renderTheResult();

		expect(
			screen.getByText(
				"Not created: its kept history starts 30 Jul 2026. Keep more history under Closed Items Cutoff in its settings.",
				{ exact: true },
			),
		).toBeInTheDocument();
	});
});
