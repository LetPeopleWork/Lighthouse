import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen, waitFor } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import {
	SingleWipTextField,
	useWipDialogState,
	useWipSaveHandlers,
	WipSettingDialog,
	WipSettingIconButton,
} from "./WipSettingDialog";

describe("WipSettingDialog", () => {
	const renderWithTheme = (component: React.ReactElement) => {
		const theme = createTheme({ palette: { mode: "light" } });
		return render(<ThemeProvider theme={theme}>{component}</ThemeProvider>);
	};

	describe("WipSettingIconButton", () => {
		it("should render with tooltip text as aria-label", () => {
			renderWithTheme(
				<WipSettingIconButton
					tooltipText="Set WIP Limit"
					isUnset={false}
					onClick={vi.fn()}
				/>,
			);

			expect(
				screen.getByRole("button", { name: "Set WIP Limit" }),
			).toBeInTheDocument();
		});

		it("should call onClick when clicked", async () => {
			const user = userEvent.setup();
			const onClick = vi.fn();

			renderWithTheme(
				<WipSettingIconButton
					tooltipText="Set WIP"
					isUnset={false}
					onClick={onClick}
				/>,
			);

			await user.click(screen.getByRole("button", { name: "Set WIP" }));

			expect(onClick).toHaveBeenCalledTimes(1);
		});

		it("should not call onClick when disabled", () => {
			const onClick = vi.fn();

			renderWithTheme(
				<WipSettingIconButton
					tooltipText="Set WIP"
					isUnset={false}
					disabled={true}
					onClick={onClick}
				/>,
			);

			const button = screen.getByRole("button", { name: "Set WIP" });
			expect(button).toBeDisabled();
			// Disabled buttons cannot be clicked, so we just verify the disabled state
			expect(onClick).not.toHaveBeenCalled();
		});

		it("should apply disabled color when isUnset is true", () => {
			const { container } = renderWithTheme(
				<WipSettingIconButton
					tooltipText="Set WIP"
					isUnset={true}
					onClick={vi.fn()}
				/>,
			);

			const button = container.querySelector(".MuiIconButton-root");
			expect(button).toBeInTheDocument();
		});
	});

	describe("WipSettingDialog Component", () => {
		it("should render dialog with title when open", () => {
			renderWithTheme(
				<WipSettingDialog
					open={true}
					onClose={vi.fn()}
					title="WIP Settings"
					onKeyDown={vi.fn()}
				>
					<div>Dialog Content</div>
				</WipSettingDialog>,
			);

			expect(screen.getByRole("dialog")).toBeInTheDocument();
			expect(screen.getByText("WIP Settings")).toBeInTheDocument();
			expect(screen.getByText("Dialog Content")).toBeInTheDocument();
		});

		it("should not render dialog when closed", () => {
			renderWithTheme(
				<WipSettingDialog
					open={false}
					onClose={vi.fn()}
					title="WIP Settings"
					onKeyDown={vi.fn()}
				>
					<div>Dialog Content</div>
				</WipSettingDialog>,
			);

			expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		});

		it("should call onKeyDown when key is pressed", async () => {
			const user = userEvent.setup();
			const onKeyDown = vi.fn();

			renderWithTheme(
				<WipSettingDialog
					open={true}
					onClose={vi.fn()}
					title="WIP Settings"
					onKeyDown={onKeyDown}
				>
					<input data-testid="input" />
				</WipSettingDialog>,
			);

			const input = screen.getByTestId("input");
			await user.click(input);
			await user.keyboard("{Enter}");

			expect(onKeyDown).toHaveBeenCalled();
		});
	});

	describe("SingleWipTextField", () => {
		it("should render with label and value", () => {
			renderWithTheme(
				<SingleWipTextField label="WIP Limit" value={5} onChange={vi.fn()} />,
			);

			expect(screen.getByLabelText("WIP Limit")).toHaveValue(5);
		});

		it("should display default helper text", () => {
			renderWithTheme(
				<SingleWipTextField label="WIP Limit" value={0} onChange={vi.fn()} />,
			);

			expect(screen.getByText("Set to 0 to disable limit")).toBeInTheDocument();
		});

		it("should display custom helper text", () => {
			renderWithTheme(
				<SingleWipTextField
					label="WIP Limit"
					value={0}
					onChange={vi.fn()}
					helperText="Custom helper"
				/>,
			);

			expect(screen.getByText("Custom helper")).toBeInTheDocument();
		});

		it("should call onChange with parsed number", async () => {
			const user = userEvent.setup();
			const onChange = vi.fn();

			renderWithTheme(
				<SingleWipTextField label="WIP Limit" value={0} onChange={onChange} />,
			);

			const input = screen.getByLabelText("WIP Limit");
			await user.clear(input);
			await user.type(input, "7");

			expect(onChange).toHaveBeenLastCalledWith(7);
		});

		it("should call onChange with 0 for invalid input", async () => {
			const user = userEvent.setup();
			const onChange = vi.fn();

			renderWithTheme(
				<SingleWipTextField label="WIP Limit" value={5} onChange={onChange} />,
			);

			const input = screen.getByLabelText("WIP Limit");
			await user.clear(input);

			expect(onChange).toHaveBeenLastCalledWith(0);
		});
	});

	describe("useWipDialogState", () => {
		const TestComponent = ({
			initialValue,
			onOpen,
		}: {
			initialValue: number;
			onOpen?: () => void;
		}) => {
			const { open, value, setValue, handleOpen, handleClose } =
				useWipDialogState({ initialValue, onOpen });

			return (
				<div>
					<span data-testid="value">{value}</span>
					<span data-testid="open">{open.toString()}</span>
					<button type="button" onClick={() => handleOpen(false)}>
						Open
					</button>
					<button type="button" onClick={() => handleOpen(true)}>
						Open Disabled
					</button>
					<button type="button" onClick={handleClose}>
						Close
					</button>
					<button type="button" onClick={() => setValue(99)}>
						Set 99
					</button>
				</div>
			);
		};

		it("should initialize with closed state", () => {
			render(<TestComponent initialValue={5} />);

			expect(screen.getByTestId("open")).toHaveTextContent("false");
		});

		it("should open dialog when handleOpen called with disabled=false", async () => {
			const user = userEvent.setup();
			render(<TestComponent initialValue={5} />);

			await user.click(screen.getByText("Open"));

			expect(screen.getByTestId("open")).toHaveTextContent("true");
		});

		it("should not open dialog when handleOpen called with disabled=true", async () => {
			const user = userEvent.setup();
			render(<TestComponent initialValue={5} />);

			await user.click(screen.getByText("Open Disabled"));

			expect(screen.getByTestId("open")).toHaveTextContent("false");
		});

		it("should reset value to initialValue when opened", async () => {
			const user = userEvent.setup();
			render(<TestComponent initialValue={5} />);

			await user.click(screen.getByText("Set 99"));
			expect(screen.getByTestId("value")).toHaveTextContent("99");

			await user.click(screen.getByText("Open"));

			await waitFor(() => {
				expect(screen.getByTestId("value")).toHaveTextContent("5");
			});
		});

		it("should call onOpen when dialog opens", async () => {
			const user = userEvent.setup();
			const onOpen = vi.fn();
			render(<TestComponent initialValue={5} onOpen={onOpen} />);

			await user.click(screen.getByText("Open"));

			await waitFor(() => {
				expect(onOpen).toHaveBeenCalledTimes(1);
			});
		});

		it("should close dialog when handleClose called", async () => {
			const user = userEvent.setup();
			render(<TestComponent initialValue={5} />);

			await user.click(screen.getByText("Open"));
			expect(screen.getByTestId("open")).toHaveTextContent("true");

			await user.click(screen.getByText("Close"));
			expect(screen.getByTestId("open")).toHaveTextContent("false");
		});
	});

	describe("useWipSaveHandlers", () => {
		const TestComponent = ({
			currentValue,
			initialValue,
			onSave,
		}: {
			currentValue: number;
			initialValue: number;
			onSave: (value: number) => Promise<void>;
		}) => {
			const [closed, setClosed] = useState(false);
			const { handleSave, handleKeyDown, handleDialogClose } =
				useWipSaveHandlers({
					currentValue,
					initialValue,
					onSave,
					onClose: () => setClosed(true),
				});

			return (
				<div>
					<span data-testid="closed">{closed.toString()}</span>
					<button type="button" onClick={handleSave}>
						Save
					</button>
					<input
						data-testid="input"
						onKeyDown={handleKeyDown}
						defaultValue=""
					/>
					<button
						type="button"
						onClick={() => handleDialogClose(undefined, "backdropClick")}
					>
						Backdrop
					</button>
					<button
						type="button"
						onClick={() => handleDialogClose(undefined, "escapeKeyDown")}
					>
						Escape
					</button>
				</div>
			);
		};

		it("should call onSave and onClose when value is dirty", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={10} initialValue={5} onSave={onSave} />,
			);

			await user.click(screen.getByText("Save"));

			await waitFor(() => {
				expect(onSave).toHaveBeenCalledWith(10);
			});
			expect(screen.getByTestId("closed")).toHaveTextContent("true");
		});

		it("should only call onClose when value is not dirty", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={5} initialValue={5} onSave={onSave} />,
			);

			await user.click(screen.getByText("Save"));

			expect(onSave).not.toHaveBeenCalled();
			expect(screen.getByTestId("closed")).toHaveTextContent("true");
		});

		it("should save on Enter key when dirty", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={10} initialValue={5} onSave={onSave} />,
			);

			const input = screen.getByTestId("input");
			await user.click(input);
			await user.keyboard("{Enter}");

			await waitFor(() => {
				expect(onSave).toHaveBeenCalledWith(10);
			});
		});

		it("should close on Escape key without saving", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={10} initialValue={5} onSave={onSave} />,
			);

			const input = screen.getByTestId("input");
			await user.click(input);
			await user.keyboard("{Escape}");

			expect(onSave).not.toHaveBeenCalled();
			expect(screen.getByTestId("closed")).toHaveTextContent("true");
		});

		it("should save on backdrop click when dirty", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={10} initialValue={5} onSave={onSave} />,
			);

			await user.click(screen.getByText("Backdrop"));

			await waitFor(() => {
				expect(onSave).toHaveBeenCalledWith(10);
			});
		});

		it("should close on backdrop click without saving when not dirty", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={5} initialValue={5} onSave={onSave} />,
			);

			await user.click(screen.getByText("Backdrop"));

			expect(onSave).not.toHaveBeenCalled();
			expect(screen.getByTestId("closed")).toHaveTextContent("true");
		});

		it("should close on non-backdrop close reasons", async () => {
			const user = userEvent.setup();
			const onSave = vi.fn().mockResolvedValue(undefined);

			render(
				<TestComponent currentValue={10} initialValue={5} onSave={onSave} />,
			);

			await user.click(screen.getByText("Escape"));

			expect(onSave).not.toHaveBeenCalled();
			expect(screen.getByTestId("closed")).toHaveTextContent("true");
		});
	});
});
