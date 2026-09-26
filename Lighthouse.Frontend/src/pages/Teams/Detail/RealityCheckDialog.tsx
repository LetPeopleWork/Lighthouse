import CloseIcon from "@mui/icons-material/Close";
import {
	Button,
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
import RealityCheckTable from "./RealityCheckTable";
import RealityCheckVerdict from "./RealityCheckVerdict";

export type RealityCheckRun =
	| { state: "running" }
	| { state: "answered"; result: RealityCheckResult }
	| { state: "failed"; message: string };

interface RealityCheckDialogProps {
	open: boolean;
	teamName: string;
	run: RealityCheckRun;
	onClose: () => void;
}

interface RealityCheckRunContentProps {
	teamName: string;
	run: RealityCheckRun;
}

const RealityCheckRunContent: React.FC<
	Readonly<RealityCheckRunContentProps>
> = ({ teamName, run }) => {
	if (run.state === "running") {
		return (
			<Typography role="status">
				Checking {teamName}'s forecasts against what happened…
			</Typography>
		);
	}
	if (run.state === "failed") {
		return <Typography>{run.message}</Typography>;
	}
	return (
		<Stack spacing={3}>
			<RealityCheckVerdict result={run.result} />
			<RealityCheckTable result={run.result} />
		</Stack>
	);
};

const RealityCheckDialog: React.FC<Readonly<RealityCheckDialogProps>> = ({
	open,
	teamName,
	run,
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
				<RealityCheckRunContent teamName={teamName} run={run} />
			</DialogContent>
			<DialogActions>
				<Button onClick={onClose}>Close</Button>
			</DialogActions>
		</Dialog>
	);
};

export default RealityCheckDialog;
