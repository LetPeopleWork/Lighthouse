import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import InputGroup from "./InputGroup";

describe("InputGroup", () => {
	it("renders the title correctly", () => {
		const title = "Test Title";
		render(<InputGroup title={title}>Test Child</InputGroup>);

		expect(screen.getByText(title)).toBeInTheDocument();
	});

	it("renders its children correctly", () => {
		const title = "Test Title";
		const childrenText = "Test Child";
		render(
			<InputGroup title={title}>
				<div>{childrenText}</div>
			</InputGroup>,
		);

		expect(screen.getByText(childrenText)).toBeInTheDocument();
	});

	it("has the correct structure and layout", () => {
		const title = "Test Title";
		const childrenText1 = "Test Child";
		const childrenText2 = "Another Child";
		render(
			<InputGroup title={title}>
				<div>{childrenText1}</div>
				<div>{childrenText2}</div>
			</InputGroup>,
		);

		const cardHeader = screen.getByText(title);
		expect(cardHeader).toBeInTheDocument();

		const gridItems = screen.getAllByText(/(Test Child|Another Child)/);
		expect(gridItems.length).toBe(2);
		expect(gridItems[0]).toHaveTextContent(childrenText1);
		expect(gridItems[1]).toHaveTextContent(childrenText2);
	});
});
