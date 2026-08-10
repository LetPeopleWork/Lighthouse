import Alert from "@mui/material/Alert";
import AlertTitle from "@mui/material/AlertTitle";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import React from "react";

interface AppErrorBoundaryProps {
	readonly children: React.ReactNode;
}

interface AppErrorBoundaryState {
	readonly message: string | null;
}

// Bug #5732: nothing wrapped the app shell, so one render-time throw unmounted the whole
// tree and left users staring at a white page with no way to tell what happened.
// The fallback deliberately uses no app context (terminology, RBAC) — it has to render
// when those are the things that broke.
class AppErrorBoundary extends React.Component<
	AppErrorBoundaryProps,
	AppErrorBoundaryState
> {
	constructor(props: AppErrorBoundaryProps) {
		super(props);
		this.state = { message: null };
	}

	static getDerivedStateFromError(error: unknown): AppErrorBoundaryState {
		return {
			message: error instanceof Error ? error.message : String(error),
		};
	}

	private readonly handleReload = () => {
		globalThis.location.reload();
	};

	render() {
		if (this.state.message === null) {
			return this.props.children;
		}

		return (
			<Box
				sx={{
					display: "flex",
					justifyContent: "center",
					alignItems: "center",
					minHeight: "100vh",
					p: 3,
				}}
			>
				<Alert
					severity="error"
					sx={{ maxWidth: 640 }}
					action={
						<Button color="inherit" size="small" onClick={this.handleReload}>
							Reload
						</Button>
					}
				>
					<AlertTitle>Something went wrong</AlertTitle>
					Lighthouse could not display this page. Reloading usually fixes it. If
					it keeps happening, reload with Ctrl+Shift+R to make sure you are
					running the latest version.
					<Box component="p" sx={{ mt: 1, mb: 0, fontFamily: "monospace" }}>
						{this.state.message}
					</Box>
				</Alert>
			</Box>
		);
	}
}

export default AppErrorBoundary;
