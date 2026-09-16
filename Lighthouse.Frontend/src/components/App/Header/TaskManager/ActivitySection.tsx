import CancelIcon from "@mui/icons-material/Cancel";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import type {
	IUpdateTask,
	UpdateTaskType,
} from "../../../../services/UpdateSubscriptionService";

const RUNNING = "Running";

const WAITING = "Queued";

/**
 * What the row says between the click and the instance agreeing the work has stopped. Cancellation is
 * cooperative: the connector only notices at its next checkpoint, which has been measured at eleven
 * seconds against a real tracker, and for that whole window an unchanged row reads as a dead control.
 */
const STOPPING = "Stopping…";

/** What identifies one piece of work across a re-read, so the browser can remember asking to stop it. */
export const taskKey = (task: IUpdateTask): string =>
	`${task.updateType}-${task.id}`;

/**
 * What an operator is told a piece of work is. The update type is the instance's own vocabulary; this
 * is the reader's, so a tenant who renamed Team to Squad never meets the seeded default here.
 */
const useKindOf = () => {
	const { getTerm } = useTerminology();

	return (updateType: UpdateTaskType): string => {
		switch (updateType) {
			case "Team":
			case "TeamDelete":
				return getTerm(TERMINOLOGY_KEYS.TEAM);
			default:
				return getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
		}
	};
};

const describeState = (task: IUpdateTask, stopping: boolean): string => {
	if (stopping) {
		return STOPPING;
	}

	switch (task.status) {
		case "InProgress":
			return RUNNING;
		case "Queued":
			return task.waitingBehind
				? `${WAITING} behind ${task.waitingBehind}`
				: WAITING;
		default:
			return task.status;
	}
};

/**
 * Something turning means something is happening, and there is only ever one of those: the queue runs a
 * single piece of work at a time. Three spinners in a column would say three things are under way when
 * one is, so what is waiting gets a mark that means waiting instead.
 *
 * Drawn rather than spelled out, and the word is still in the row beside it — the drawing is what a
 * reader takes in without reading, not a replacement for what it says.
 */
const RowProgress = ({
	task,
	stopping,
}: {
	task: IUpdateTask;
	stopping: boolean;
}) => {
	if (stopping) {
		return <CircularProgress size={16} aria-label={STOPPING} />;
	}

	if (task.status === "InProgress") {
		return <CircularProgress size={16} aria-label={RUNNING} />;
	}

	if (task.status === "Queued") {
		return (
			<HourglassEmptyIcon
				fontSize="small"
				color="disabled"
				titleAccess={WAITING}
			/>
		);
	}

	return null;
};

const isDelete = (updateType: UpdateTaskType): boolean =>
	updateType === "TeamDelete" || updateType === "PortfolioDelete";

interface ActivitySectionProps {
	tasks: IUpdateTask[];
	/**
	 * The work this browser has asked the instance to stop. It is remembered here rather than reported by
	 * the instance because the instance has nowhere to keep it: the store is not advanced until the work
	 * actually stops. The operator who clicked is the one who needs the answer, and a ten-second window
	 * does not need to survive a reload.
	 */
	stopAsked: ReadonlySet<string>;
	onCancel: (task: IUpdateTask) => void;
}

const ActivitySection = ({
	tasks,
	stopAsked,
	onCancel,
}: ActivitySectionProps) => {
	const kindOf = useKindOf();

	if (tasks.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				Nothing is being refreshed right now.
			</Typography>
		);
	}

	return tasks.map((task) => {
		const stopping = stopAsked.has(taskKey(task));

		return (
			<Box
				key={taskKey(task)}
				data-task-row=""
				tabIndex={-1}
				sx={{ display: "flex", alignItems: "center", gap: 1, py: 0.5 }}
			>
				<RowProgress task={task} stopping={stopping} />

				<Typography
					data-testid={`task-manager-row-${task.updateType}-${task.id}`}
					variant="body2"
					sx={{ flexGrow: 1 }}
				>
					{kindOf(task.updateType)} '{task.name}'
					{isDelete(task.updateType) ? " (removal)" : ""} —{" "}
					{describeState(task, stopping)}
				</Typography>

				<Tooltip title={`Stop refreshing ${task.name}`}>
					{/* A disabled button is not an event target, so the tooltip needs something of its own to
					    hang on while the row is waiting for the instance to agree it stopped. */}
					<span>
						<IconButton
							aria-label={`Stop refreshing ${task.name}`}
							size="small"
							disabled={stopping}
							onClick={(event) => {
								// This control is about to stop accepting input, and a disabled element receives
								// no key presses - so leaving focus on it would leave a keyboard reader unable to
								// close the popover it is standing in. Focus moves onto the row instead, which
								// also means the next thing a screen reader announces is the row's new state.
								event.currentTarget
									.closest<HTMLElement>("[data-task-row]")
									?.focus();
								onCancel(task);
							}}
						>
							<CancelIcon fontSize="small" />
						</IconButton>
					</span>
				</Tooltip>
			</Box>
		);
	});
};

export default ActivitySection;
