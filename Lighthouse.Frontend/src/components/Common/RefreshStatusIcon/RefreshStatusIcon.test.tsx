import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import RefreshStatusIcon from "./RefreshStatusIcon";

describe("RefreshStatusIcon", () => {
	it("says the last refresh failed, in the reader's own words for the thing that was refreshed", () => {
		render(
			<RefreshStatusIcon
				isUpdating={false}
				hasFailed={true}
				failedLabel="Last Squad refresh failed"
			/>,
		);

		expect(screen.getByTitle("Last Squad refresh failed")).toBeInTheDocument();
	});

	it("says nothing about failure once a refresh is under way again", () => {
		render(
			<RefreshStatusIcon
				isUpdating={true}
				hasFailed={true}
				failedLabel="Last Squad refresh failed"
			/>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(
			screen.queryByTitle("Last Squad refresh failed"),
		).not.toBeInTheDocument();
	});

	it("shows nothing out of the ordinary when the last refresh worked", () => {
		render(
			<RefreshStatusIcon
				isUpdating={false}
				hasFailed={false}
				failedLabel="Last Squad refresh failed"
			/>,
		);

		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		expect(
			screen.queryByTitle("Last Squad refresh failed"),
		).not.toBeInTheDocument();
	});
});
