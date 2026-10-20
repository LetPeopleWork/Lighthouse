import CloseIcon from "@mui/icons-material/Close";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Snackbar from "@mui/material/Snackbar";
import React, {
	useCallback,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";

interface SnackbarErrorHandlerProps {
	children: React.ReactNode;
}

/** A way to act on a message, offered beside it, such as Retry after an export that made no PDF. */
export interface SnackbarAction {
	readonly label: string;
	readonly onClick: () => void;
}

export interface ShowErrorOptions {
	readonly action?: SnackbarAction;
}

/** Takes the message it was handed back for away, and leaves any newer message standing. */
export type TakeTheMessageBack = () => void;

export const ErrorSnackbarContext = React.createContext<{
	showError: (msg: string, options?: ShowErrorOptions) => TakeTheMessageBack;
} | null>(null);

export const useErrorSnackbar = () => {
	const ctx = React.useContext(ErrorSnackbarContext);
	if (!ctx)
		throw new Error(
			"useErrorSnackbar must be used within a SnackbarErrorHandler",
		);
	return ctx;
};

/**
 * The browser raises this when a resize callback triggers another resize in the same frame. It is a
 * notice, not an exception: the observer defers the remaining work to the next frame and nothing is
 * lost. No user can act on it and no developer needs a toast for it, so it is dropped before it can
 * become one.
 *
 * The harsher sibling, "ResizeObserver loop limit exceeded", is deliberately NOT dropped — that one
 * means the loop never settled, which is a real runaway worth seeing.
 */
const BENIGN_RESIZE_NOTICE = "ResizeObserver loop completed with undelivered";

export function isBenignBrowserNotice(message: string): boolean {
	return message.startsWith(BENIGN_RESIZE_NOTICE);
}

interface ErrorBoundaryProps {
	onError: (msg: string) => void;
	children?: React.ReactNode;
}

interface ErrorBoundaryState {
	hasError: boolean;
}

class ErrorBoundary extends React.Component<
	ErrorBoundaryProps,
	ErrorBoundaryState
> {
	constructor(props: ErrorBoundaryProps) {
		super(props);
		this.state = { hasError: false };
	}

	static getDerivedStateFromError(): ErrorBoundaryState {
		return { hasError: true };
	}

	componentDidCatch(error: unknown) {
		const message = error instanceof Error ? error.message : String(error);
		this.props.onError?.(message);
	}

	render() {
		if (this.state.hasError) {
			return (
				<Alert severity="error" sx={{ m: 2 }}>
					This section could not be displayed.
				</Alert>
			);
		}

		return this.props.children ?? null;
	}
}

interface ShownMessage {
	readonly id: number;
	readonly text: string;
	readonly action?: SnackbarAction;
}

const MESSAGE_ALONE_HIDES_AFTER_MS = 6000;

const SnackbarErrorHandler: React.FC<SnackbarErrorHandlerProps> = ({
	children,
}) => {
	const [shown, setShown] = useState<ShownMessage | null>(null);
	const lastId = useRef(0);

	const closeIfShowing = useCallback((id: number) => {
		setShown((current) => (current?.id === id ? null : current));
	}, []);

	const showError = useCallback(
		(msg: string, options?: ShowErrorOptions): TakeTheMessageBack => {
			lastId.current += 1;
			const id = lastId.current;
			setShown({ id, text: msg, action: options?.action });
			return () => closeIfShowing(id);
		},
		[closeIfShowing],
	);

	const contextValue = useMemo(() => ({ showError }), [showError]);

	useEffect(() => {
		const onWindowError = (ev: ErrorEvent) => {
			const msg = ev.error instanceof Error ? ev.error.message : ev.message;

			if (isBenignBrowserNotice(msg)) {
				return;
			}

			showError(msg);
		};

		const onUnhandledRejection = (ev: PromiseRejectionEvent) => {
			const reason =
				ev?.reason ??
				(ev as unknown as { detail?: unknown })?.detail ??
				"Unhandled promise rejection";
			let msg: string;
			if (reason instanceof Error) {
				msg = reason.message;
			} else {
				msg = String(reason);
			}
			showError(msg);
		};

		globalThis.addEventListener("error", onWindowError);
		globalThis.addEventListener(
			"unhandledrejection",
			onUnhandledRejection as EventListener,
		);

		return () => {
			globalThis.removeEventListener("error", onWindowError);
			globalThis.removeEventListener(
				"unhandledrejection",
				onUnhandledRejection as EventListener,
			);
		};
	}, [showError]);

	const handleClose = (
		_event?: Event | React.SyntheticEvent | null,
		reason?: string,
	) => {
		if (reason === "clickaway") return;
		setShown(null);
	};

	const chooseTheAction = (message: ShownMessage, action: SnackbarAction) => {
		closeIfShowing(message.id);
		action.onClick();
	};

	const action = shown?.action;

	return (
		<ErrorSnackbarContext.Provider value={contextValue}>
			<ErrorBoundary onError={(m) => showError(m)}>{children}</ErrorBoundary>

			{/* Bug #5732: kept outside the boundary, or the error unmounts its own report. */}
			<Snackbar
				open={shown !== null}
				autoHideDuration={
					action === undefined ? MESSAGE_ALONE_HIDES_AFTER_MS : null
				}
				onClose={handleClose}
				anchorOrigin={{ vertical: "top", horizontal: "center" }}
			>
				<Alert
					onClose={handleClose}
					severity="error"
					sx={{ width: "100%" }}
					action={
						shown === null || action === undefined ? undefined : (
							<>
								<Button
									color="inherit"
									size="small"
									onClick={() => chooseTheAction(shown, action)}
								>
									{action.label}
								</Button>
								<IconButton
									aria-label="Close"
									title="Close"
									color="inherit"
									size="small"
									onClick={handleClose}
								>
									<CloseIcon fontSize="small" />
								</IconButton>
							</>
						)
					}
				>
					{shown?.text ?? ""}
				</Alert>
			</Snackbar>
		</ErrorSnackbarContext.Provider>
	);
};

export default SnackbarErrorHandler;
