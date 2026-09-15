import CancelIcon from "@mui/icons-material/Cancel";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import type {
	IUpdateTask,
	UpdateTaskType,
} from "../../../../services/UpdateSubscriptionService";
import { formatElapsed } from "../../../../utils/date/formatElapsed";

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

const describeState = (task: IUpdateTask): string => {
	switch (task.status) {
		case "InProgress":
			return "Running";
		case "Queued":
			return task.waitingBehind
				? `Queued behind ${task.waitingBehind}`
				: "Queued";
		default:
			return task.status;
	}
};

/**
 * The duration is the instance's own measurement, rendered as it arrived. Counting locally instead
 * would be wrong after a reload, wrong for a refresh that began before the tab was opened, and wrong
 * by however far this machine's clock has drifted - which is most of the occasions somebody opens
 * this list. A row whose moment the instance never recorded keeps its state and loses only the
 * duration; that is an ordinary mid-upgrade state, not a fault worth showing.
 */
const describeStatus = (task: IUpdateTask): string => {
	const state = describeState(task);

	return task.elapsedMs == null
		? state
		: `${state} for ${formatElapsed(task.elapsedMs)}`;
};

const isDelete = (updateType: UpdateTaskType): boolean =>
	updateType === "TeamDelete" || updateType === "PortfolioDelete";

interface ActivitySectionProps {
	tasks: IUpdateTask[];
	onCancel: (task: IUpdateTask) => void;
}

const ActivitySection = ({ tasks, onCancel }: ActivitySectionProps) => {
	const kindOf = useKindOf();

	if (tasks.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				Nothing is being refreshed right now.
			</Typography>
		);
	}

	return tasks.map((task) => (
		<Box
			key={`${task.updateType}-${task.id}`}
			sx={{ display: "flex", alignItems: "center", gap: 1, py: 0.5 }}
		>
			<Typography
				data-testid={`task-manager-row-${task.updateType}-${task.id}`}
				variant="body2"
				sx={{ flexGrow: 1 }}
			>
				{kindOf(task.updateType)} '{task.name}'
				{isDelete(task.updateType) ? " (removal)" : ""} — {describeStatus(task)}
			</Typography>

			<Tooltip title={`Stop refreshing ${task.name}`}>
				<IconButton
					aria-label={`Stop refreshing ${task.name}`}
					size="small"
					onClick={() => onCancel(task)}
				>
					<CancelIcon fontSize="small" />
				</IconButton>
			</Tooltip>
		</Box>
	));
};

export default ActivitySection;
