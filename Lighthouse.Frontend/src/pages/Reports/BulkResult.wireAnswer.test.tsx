import { render, screen } from "@testing-library/react";
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
	it("names each owner after its kind, whether or not its report was created", () => {
		renderTheResult();

		expect(
			screen.getByText("Team Zenith", { exact: true }),
		).toBeInTheDocument();
		expect(
			screen.getByText("Portfolio Apollo", { exact: true }),
		).toBeInTheDocument();
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
