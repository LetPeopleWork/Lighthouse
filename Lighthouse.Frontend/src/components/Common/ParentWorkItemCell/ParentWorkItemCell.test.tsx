import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import ParentWorkItemCell from "./ParentWorkItemCell";

describe("ParentWorkItemCell", () => {
	it("should display 'No Parent' when parentReference is null", () => {
		const parentMap = new Map<string, ParentWorkItem>();

		render(<ParentWorkItemCell parentReference={null} parentMap={parentMap} />);

		expect(screen.getByText("No Parent")).toBeInTheDocument();
	});

	it("should display 'No Parent' when parentReference is undefined", () => {
		const parentMap = new Map<string, ParentWorkItem>();

		render(
			<ParentWorkItemCell parentReference={undefined} parentMap={parentMap} />,
		);

		expect(screen.getByText("No Parent")).toBeInTheDocument();
	});

	it("should display 'No Parent' when parentReference is empty string", () => {
		const parentMap = new Map<string, ParentWorkItem>();

		render(<ParentWorkItemCell parentReference="" parentMap={parentMap} />);

		expect(screen.getByText("No Parent")).toBeInTheDocument();
	});

	it("should display parent info with link when parent is found in map", () => {
		const parentMap = new Map<string, ParentWorkItem>();
		parentMap.set("PARENT-1", {
			referenceId: "PARENT-1",
			name: "Parent Feature",
			url: "http://example.com/parent",
		});

		render(
			<ParentWorkItemCell parentReference="PARENT-1" parentMap={parentMap} />,
		);

		const link = screen.getByRole("link");
		expect(link).toHaveAttribute("href", "http://example.com/parent");
		expect(link).toHaveAttribute("target", "_blank");
		expect(link).toHaveAttribute("rel", "noopener noreferrer");
		expect(screen.getByText("PARENT-1 - Parent Feature")).toBeInTheDocument();
	});

	it("should display reference ID when parent is not found in map", () => {
		const parentMap = new Map<string, ParentWorkItem>();

		render(
			<ParentWorkItemCell parentReference="PARENT-1" parentMap={parentMap} />,
		);

		expect(screen.getByText("PARENT-1")).toBeInTheDocument();
		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("should handle parent with empty name gracefully", () => {
		const parentMap = new Map<string, ParentWorkItem>();
		parentMap.set("PARENT-1", {
			referenceId: "PARENT-1",
			name: "",
			url: "http://example.com/parent",
		});

		render(
			<ParentWorkItemCell parentReference="PARENT-1" parentMap={parentMap} />,
		);

		const link = screen.getByRole("link");
		expect(link).toBeInTheDocument();
		expect(screen.getByText(/PARENT-1 -/)).toBeInTheDocument();
	});
});
