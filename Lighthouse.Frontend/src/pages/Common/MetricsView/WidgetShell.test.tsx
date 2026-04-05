import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import WidgetShell from "./WidgetShell";

describe("WidgetShell", () => {
	it("renders children with correct testid", () => {
		render(
			<WidgetShell widgetKey="test-widget">
				<div data-testid="child-content">Hello</div>
			</WidgetShell>,
		);
		expect(screen.getByTestId("widget-shell-test-widget")).toBeInTheDocument();
		expect(screen.getByTestId("child-content")).toBeInTheDocument();
	});

	it("renders title when provided", () => {
		render(
			<WidgetShell widgetKey="test-widget" title="My Chart">
				<div>Content</div>
			</WidgetShell>,
		);
		expect(screen.getByText("My Chart")).toBeInTheDocument();
		expect(
			screen.getByTestId("widget-shell-header-test-widget"),
		).toBeInTheDocument();
	});

	it("omits title zone when title is not provided", () => {
		render(
			<WidgetShell widgetKey="test-widget">
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-shell-header-test-widget"),
		).not.toBeInTheDocument();
	});

	it("renders footer with RAG chip when showTips is true", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={true}
				footer={{ ragStatus: "red", tipText: "Action needed" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.getByTestId("widget-shell-footer-test-widget"),
		).toBeInTheDocument();
		expect(screen.getByTestId("widget-rag-test-widget")).toBeInTheDocument();
		expect(screen.getByText("Action needed")).toBeInTheDocument();
	});

	it("hides footer when showTips is false", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={false}
				footer={{ ragStatus: "green", tipText: "All good" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-shell-footer-test-widget"),
		).not.toBeInTheDocument();
	});

	it("omits RAG chip when ragStatus is none", () => {
		render(
			<WidgetShell
				widgetKey="test-widget"
				showTips={true}
				footer={{ ragStatus: "none", tipText: "Info only" }}
			>
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.getByTestId("widget-shell-footer-test-widget"),
		).toBeInTheDocument();
		expect(
			screen.queryByTestId("widget-rag-test-widget"),
		).not.toBeInTheDocument();
		expect(screen.getByText("Info only")).toBeInTheDocument();
	});

	it("omits footer when no footer prop is provided", () => {
		render(
			<WidgetShell widgetKey="test-widget">
				<div>Content</div>
			</WidgetShell>,
		);
		expect(
			screen.queryByTestId("widget-shell-footer-test-widget"),
		).not.toBeInTheDocument();
	});
});
