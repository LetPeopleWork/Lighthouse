import CancelIcon from "@mui/icons-material/Cancel";
import TimelineIcon from "@mui/icons-material/Timeline";
import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { useCallback, useContext, useEffect, useState } from "react";
import { useRbac } from "../../../hooks/useRbac";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import type {
	IUpdateTask,
	UpdateTaskType,
} from "../../../services/UpdateSubscriptionService";
import { formatElapsed } from "../../../utils/date/formatElapsed";

const ACTIVITY_LABEL = "Activity";

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

const TaskManagerIcon = () => {
	const { isSystemAdmin } = useRbac();
	const { updateSubscriptionService } = useContext(ApiServiceContext);
	const kindOf = useKindOf();

	const [tasks, setTasks] = useState<IUpdateTask[]>([]);
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);

	const refresh = useCallback(async () => {
		try {
			setTasks(await updateSubscriptionService.getRunningTasks());
		} catch {
			// An instance that cannot say what it is doing is not an instance doing nothing, so the list is
			// left as it was rather than being emptied into a confident "nothing is running".
		}
	}, [updateSubscriptionService]);

	/**
	 * The list is re-read rather than edited in place: what actually stopped is the instance's answer, not
	 * this component's guess, and a cancel that was refused or arrived too late would otherwise leave a row
	 * showing a state the server never agreed to.
	 */
	const cancel = useCallback(
		async (task: IUpdateTask) => {
			try {
				await updateSubscriptionService.cancelTask(task.updateType, task.id);
			} catch {
				// Nothing is claimed on the strength of a failed ask.
			}

			await refresh();
		},
		[refresh, updateSubscriptionService],
	);

	useEffect(() => {
		if (!isSystemAdmin) {
			return;
		}

		let cancelled = false;
		const reread = () => {
			if (!cancelled) {
				void refresh();
			}
		};

		reread();
		void updateSubscriptionService.subscribeToAllUpdates(reread);

		return () => {
			cancelled = true;
			void updateSubscriptionService.unsubscribeFromAllUpdates();
		};
	}, [isSystemAdmin, refresh, updateSubscriptionService]);

	if (!isSystemAdmin) {
		return null;
	}

	return (
		<>
			<Tooltip title={ACTIVITY_LABEL}>
				<IconButton
					aria-label={ACTIVITY_LABEL}
					color="inherit"
					onClick={(event) => setAnchor(event.currentTarget)}
				>
					<Badge badgeContent={tasks.length} color="primary">
						<TimelineIcon />
					</Badge>
				</IconButton>
			</Tooltip>

			<Popover
				open={anchor !== null}
				anchorEl={anchor}
				onClose={() => setAnchor(null)}
				anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
				transformOrigin={{ vertical: "top", horizontal: "right" }}
				slotProps={{ paper: { sx: { p: 2, minWidth: 320 } } }}
			>
				<Typography variant="subtitle2" gutterBottom>
					{ACTIVITY_LABEL}
				</Typography>

				{tasks.length === 0 ? (
					<Typography variant="body2" color="text.secondary">
						Nothing is being refreshed right now.
					</Typography>
				) : (
					tasks.map((task) => (
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
								{isDelete(task.updateType) ? " (removal)" : ""} —{" "}
								{describeStatus(task)}
							</Typography>

							<Tooltip title={`Stop refreshing ${task.name}`}>
								<IconButton
									aria-label={`Stop refreshing ${task.name}`}
									size="small"
									onClick={() => void cancel(task)}
								>
									<CancelIcon fontSize="small" />
								</IconButton>
							</Tooltip>
						</Box>
					))
				)}
			</Popover>
		</>
	);
};

export default TaskManagerIcon;
