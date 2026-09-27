import CloseIcon from "@mui/icons-material/Close";
import {
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	IconButton,
	Stack,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useId } from "react";
import type { RealityCheckResult } from "../../../models/Forecasts/RealityCheckResult";
import RealityCheckLegend from "./RealityCheckLegend";
import RealityCheckSummary from "./RealityCheckSummary";
import RealityCheckTable from "./RealityCheckTable";
import { LOADING } from "./realityCheckCopy";

export type RealityCheckRun =
	| { state: "running" }
	| { state: "answered"; result: RealityCheckResult }
	| { state: "failed"; message: string };

interface RealityCheckDialogProps {
	open: boolean;
	run: RealityCheckRun;
	onRunAgain: () => void;
	onClose: () => void;
}

interface RealityCheckRunContentProps {
	run: RealityCheckRun;
}

const RealityCheckRunContent: React.FC<
	Readonly<RealityCheckRunContentProps>
> = ({ run }) => {
	if (run.state === "running") {
		return (
			<Stack
				role="status"
				direction="row"
				spacing={2}
				sx={{ alignItems: "center" }}
			>
				<CircularProgress size={24} aria-hidden />
				<Typography>{LOADING}</Typography>
			</Stack>
		);
	}
	if (run.state === "failed") {
		return <Typography role="alert">{run.message}</Typography>;
	}
	return (
		<Stack spacing={3}>
			<RealityCheckSummary result={run.result} />
			<RealityCheckTable result={run.result} />
			<RealityCheckLegend />
		</Stack>
	);
};

const RealityCheckDialog: React.FC<Readonly<RealityCheckDialogProps>> = ({
	open,
	run,
	onRunAgain,
	onClose,
}) => {
	const theme = useTheme();
	const isNarrowScreen = useMediaQuery(theme.breakpoints.down("sm"));
	const titleId = useId();

	return (
		<Dialog
			open={open}
			onClose={onClose}
			fullWidth
			maxWidth="lg"
			fullScreen={isNarrowScreen}
			aria-labelledby={titleId}
		>
			<DialogTitle id={titleId} sx={{ pr: 6 }}>
				Forecast reality check
			</DialogTitle>
			<IconButton
				aria-label="Close"
				onClick={onClose}
				sx={{ position: "absolute", right: 8, top: 8 }}
			>
				<CloseIcon />
			</IconButton>
			<DialogContent dividers>
				<RealityCheckRunContent run={run} />
			</DialogContent>
			<DialogActions>
				<Button onClick={onRunAgain} disabled={run.state === "running"}>
					Run again
				</Button>
				<Button onClick={onClose}>Close</Button>
			</DialogActions>
		</Dialog>
	);
};

export default RealityCheckDialog;
