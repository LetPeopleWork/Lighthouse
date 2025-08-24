import Alert from "@mui/material/Alert";
import Snackbar from "@mui/material/Snackbar";
import React, { useCallback, useEffect, useMemo, useState } from "react";

interface SnackbarErrorHandlerProps {
	children: React.ReactNode;
}

export const ErrorSnackbarContext = React.createContext<{
	showError: (msg: string) => void;
} | null>(null);

export const useErrorSnackbar = () => {
	const ctx = React.useContext(ErrorSnackbarContext);
	if (!ctx)
		throw new Error(
			"useErrorSnackbar must be used within a SnackbarErrorHandler",
		);
	return ctx;
};

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
		if (this.state.hasError) return null;
		return this.props.children ?? null;
	}
}

const SnackbarErrorHandler: React.FC<SnackbarErrorHandlerProps> = ({
	children,
}) => {
	const [open, setOpen] = useState(false);
	const [message, setMessage] = useState<string | null>(null);

	const showError = useCallback((msg: string) => {
		setMessage(msg);
		setOpen(true);
	}, []);

	const contextValue = useMemo(() => ({ showError }), [showError]);

	useEffect(() => {
		const onWindowError = (ev: ErrorEvent) => {
			const msg = ev.error instanceof Error ? ev.error.message : ev.message;
			showError(msg);
		};

		const onUnhandledRejection = (ev: PromiseRejectionEvent) => {
			const reason =
				ev?.reason ??
				(ev as unknown as { detail?: unknown })?.detail ??
				"Unhandled promise rejection";
			// Handle ApiError instances specifically
			let msg: string;
			if (reason instanceof Error) {
				msg = reason.message;
			} else {
				msg = String(reason);
			}
			showError(msg);
		};

		window.addEventListener("error", onWindowError);
		window.addEventListener(
			"unhandledrejection",
			onUnhandledRejection as EventListener,
		);

		return () => {
			window.removeEventListener("error", onWindowError);
			window.removeEventListener(
				"unhandledrejection",
				onUnhandledRejection as EventListener,
			);
		};
	}, [showError]);

	const handleClose = (
		_event?: Event | React.SyntheticEvent,
		reason?: string,
	) => {
		if (reason === "clickaway") return;
		setOpen(false);
		setMessage(null);
	};

	return (
		<ErrorSnackbarContext.Provider value={contextValue}>
			<ErrorBoundary onError={(m) => showError(m)}>
				{children}

				<Snackbar
					open={open}
					autoHideDuration={6000}
					onClose={handleClose}
					anchorOrigin={{ vertical: "top", horizontal: "center" }}
				>
					<Alert onClose={handleClose} severity="error" sx={{ width: "100%" }}>
						{message ?? ""}
					</Alert>
				</Snackbar>
			</ErrorBoundary>
		</ErrorSnackbarContext.Provider>
	);
};

export default SnackbarErrorHandler;
