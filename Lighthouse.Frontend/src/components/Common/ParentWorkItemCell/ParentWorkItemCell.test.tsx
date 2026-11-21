import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import ParentWorkItemCell, { parentWorkItemText } from "./ParentWorkItemCell";

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
		expect(screen.getByText("PARENT-1: Parent Feature")).toBeInTheDocument();
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
		expect(screen.getByText(/PARENT-1/)).toBeInTheDocument();
	});

	it("should show the absent-parent text in the muted secondary colour", () => {
		render(
			<ParentWorkItemCell
				parentReference={null}
				parentMap={new Map<string, ParentWorkItem>()}
			/>,
		);

		expect(screen.getByText("No Parent")).toHaveStyle({
			color: "rgba(0, 0, 0, 0.6)",
		});
	});

	it("should give the text 'No Parent' for an absent reference, whatever the map holds", () => {
		const parentMap = new Map<string, ParentWorkItem>([
			["", { referenceId: "", name: "Blank", url: "http://example.com/x" }],
		]);

		expect(parentWorkItemText(null, parentMap)).toBe("No Parent");
		expect(parentWorkItemText(undefined, parentMap)).toBe("No Parent");
		expect(parentWorkItemText("", parentMap)).toBe("No Parent");
	});
});
