import { render } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { type ReactElement, useState } from "react";

/**
 * Renders one controlled part of a Team's Refinement settings on its own, handing it a value and an
 * `onChange` as the settings form does, and returns the `user` that edits it and `latest()`, the
 * value the host holds now. It wraps the section in no providers, so a test that needs the Team's
 * own words has to mock the terminology, or the section quietly shows the product's own words.
 */
export const renderRefinementSection = <TValue,>(
	initial: TValue,
	section: (value: TValue, onChange: (next: TValue) => void) => ReactElement,
) => {
	let held = initial;

	// A controlled section shows nothing new unless it is handed its change back, and a second
	// edit has to build on the first one's result, so the value lives in state.
	const Host = () => {
		const [value, setValue] = useState(initial);
		const onChange = (next: TValue) => {
			held = next;
			setValue(next);
		};
		return section(value, onChange);
	};

	render(<Host />);

	return { user: userEvent.setup(), latest: () => held };
};
