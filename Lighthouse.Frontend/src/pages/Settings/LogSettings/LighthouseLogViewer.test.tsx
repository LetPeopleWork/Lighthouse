import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@melloware/react-logviewer", () => ({
	LazyLog: (props: { text: string; follow?: boolean }) => (
		<div data-testid="log-viewer" data-follow={String(props.follow === true)}>
			{props.text}
		</div>
	),
}));

import LighthouseLogViewer from "./LighthouseLogViewer";

describe("LighthouseLogViewer", () => {
	it("shows the log it was given", () => {
		render(<LighthouseLogViewer data="Sample log data" />);

		expect(screen.getByTestId("log-viewer")).toHaveTextContent(
			"Sample log data",
		);
	});

	// Bug #6020. What an operator came for is the line written a moment ago, and the viewer opened at
	// the top of a file with thousands of them - so every attempt at anything ended in a long scroll.
	it("sits at the newest line rather than at the oldest", () => {
		render(<LighthouseLogViewer data="Sample log data" />);

		expect(screen.getByTestId("log-viewer")).toHaveAttribute(
			"data-follow",
			"true",
		);
	});
});
