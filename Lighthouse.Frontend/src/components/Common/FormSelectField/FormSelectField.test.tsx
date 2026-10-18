import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import FormSelectField from "./FormSelectField";

describe("FormSelectField", () => {
	const defaultOptions = [
		{ id: 1, label: "Option One" },
		{ id: 2, label: "Option Two" },
		{ id: 3, label: "Option Three" },
	];

	describe("Basic Rendering", () => {
		it("should render with label", () => {
			const { container } = render(
				<FormSelectField
					label="Test Label"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			const label = container.querySelector(".MuiInputLabel-root");
			expect(label).toBeInTheDocument();
			expect(label).toHaveTextContent("Test Label");
		});

		it("should render as combobox", () => {
			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			expect(screen.getByRole("combobox")).toBeInTheDocument();
		});

		it("should display selected value", () => {
			render(
				<FormSelectField
					label="Test"
					value={2}
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			expect(screen.getByRole("combobox")).toHaveTextContent("Option Two");
		});
	});

	describe("None Option", () => {
		it("should show None option by default", () => {
			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			expect(listbox.getByText("None")).toBeInTheDocument();
		});

		it("should use custom noneLabel when provided", () => {
			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
					noneLabel="Clear Selection"
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			expect(listbox.getByText("Clear Selection")).toBeInTheDocument();
			expect(listbox.queryByText("None")).not.toBeInTheDocument();
		});

		it("should not show None option when allowNone is false", () => {
			render(
				<FormSelectField
					label="Test"
					value={1}
					onChange={vi.fn()}
					options={defaultOptions}
					allowNone={false}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			expect(listbox.queryByText("None")).not.toBeInTheDocument();
		});

		it("should call onChange with null when None is selected", () => {
			const onChange = vi.fn();

			render(
				<FormSelectField
					label="Test"
					value={1}
					onChange={onChange}
					options={defaultOptions}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			fireEvent.click(listbox.getByText("None"));

			expect(onChange).toHaveBeenCalledWith(null);
		});
	});

	describe("Option Selection", () => {
		it("should call onChange with option id when option is selected", () => {
			const onChange = vi.fn();

			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={onChange}
					options={defaultOptions}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			fireEvent.click(listbox.getByText("Option Two"));

			expect(onChange).toHaveBeenCalledWith(2);
		});

		it("should display all options in dropdown", () => {
			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			expect(listbox.getByText("Option One")).toBeInTheDocument();
			expect(listbox.getByText("Option Two")).toBeInTheDocument();
			expect(listbox.getByText("Option Three")).toBeInTheDocument();
		});

		it("should work with string ids", () => {
			const stringOptions = [
				{ id: "a", label: "Alpha" },
				{ id: "b", label: "Beta" },
			];
			const onChange = vi.fn();

			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={onChange}
					options={stringOptions}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			fireEvent.click(listbox.getByText("Beta"));

			expect(onChange).toHaveBeenCalledWith("b");
		});
	});

	describe("Layout Props", () => {
		it("should apply fullWidth by default", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			const formControl = container.querySelector(".MuiFormControl-root");
			expect(formControl).toHaveClass("MuiFormControl-fullWidth");
		});

		it("should not apply fullWidth when set to false", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
					fullWidth={false}
				/>,
			);

			const formControl = container.querySelector(".MuiFormControl-root");
			expect(formControl).not.toHaveClass("MuiFormControl-fullWidth");
		});

		it("should apply normal margin by default", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			const formControl = container.querySelector(".MuiFormControl-root");
			expect(formControl).toHaveClass("MuiFormControl-marginNormal");
		});

		it("should apply dense margin when specified", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
					margin="dense"
				/>,
			);

			const formControl = container.querySelector(".MuiFormControl-root");
			expect(formControl).toHaveClass("MuiFormControl-marginDense");
		});
	});

	describe("Empty Options", () => {
		it("should render with empty options list", () => {
			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={[]}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = within(screen.getByRole("listbox"));
			// Only None option should be present
			expect(listbox.getByText("None")).toBeInTheDocument();
		});

		it("should show only None when options empty and allowNone true", () => {
			render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={[]}
					allowNone={true}
				/>,
			);

			fireEvent.mouseDown(screen.getByRole("combobox"));

			const listbox = screen.getByRole("listbox");
			const menuItems = within(listbox).getAllByRole("option");
			expect(menuItems).toHaveLength(1);
		});
	});

	describe("an entry above None", () => {
		const theLabel = (container: HTMLElement) =>
			container.querySelector(".MuiInputLabel-root") as HTMLElement;

		it("without one, an empty field shows no None and keeps its label inside", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			expect(screen.getByRole("combobox")).not.toHaveTextContent("None");
			expect(theLabel(container).dataset.shrink).toBe("false");
		});

		it("with one, a picked None stays readable and the label sits above it", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
					firstOption={{ id: 9, label: "Connection default (Option One)" }}
				/>,
			);

			expect(screen.getByRole("combobox")).toHaveTextContent("None");
			expect(theLabel(container).dataset.shrink).toBe("true");
		});
	});

	describe("Helper text", () => {
		it("shows the helper text beneath the field", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
					helperText="Set on the Jira connection."
				/>,
			);

			expect(
				container.querySelector(".MuiFormHelperText-root"),
			).toHaveTextContent("Set on the Jira connection.");
		});

		it("shows no helper line when there is no helper text", () => {
			const { container } = render(
				<FormSelectField
					label="Test"
					value=""
					onChange={vi.fn()}
					options={defaultOptions}
				/>,
			);

			expect(
				container.querySelector(".MuiFormHelperText-root"),
			).not.toBeInTheDocument();
		});
	});
});
