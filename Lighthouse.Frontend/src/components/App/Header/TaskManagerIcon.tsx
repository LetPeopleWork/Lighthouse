import TimelineIcon from "@mui/icons-material/Timeline";
import Badge from "@mui/material/Badge";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { useCallback, useContext, useEffect, useState } from "react";
import { useRbac } from "../../../hooks/useRbac";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IConnectionHealth } from "../../../services/Api/ConnectionHealthService";
import type { IRecentProblem } from "../../../services/Api/LogService";
import { useTerminology } from "../../../services/TerminologyContext";
import type { IUpdateTask } from "../../../services/UpdateSubscriptionService";
import ActivitySection from "./TaskManager/ActivitySection";
import ConnectionsSection from "./TaskManager/ConnectionsSection";
import {
	ACTIVITY_LABEL,
	badgeColourFor,
	describeHeaderState,
	isBroken,
} from "./TaskManager/connectionHealthWording";
import RecentProblemsSection from "./TaskManager/RecentProblemsSection";

/**
 * An instance that cannot say what it is doing is not an instance doing nothing. A read that failed
 * therefore leaves the list exactly as it was, rather than emptying it into a confident "nothing is
 * running", "every credential is fine" or "nothing has gone wrong" - all three of which are claims
 * nobody made.
 *
 * It lives out here rather than inside the component so that each new thing the popover reads costs
 * the component one line and no extra nesting.
 */
const readAndKeepWhatWasThereIfItFails = async <T,>(
	read: () => Promise<T>,
	keep: (answer: T) => void,
) => {
	try {
		keep(await read());
	} catch {
		// Nothing is claimed on the strength of a failed ask.
	}
};

const TaskManagerIcon = () => {
	const { isSystemAdmin } = useRbac();
	const { updateSubscriptionService, connectionHealthService, logService } =
		useContext(ApiServiceContext);
	const { getTerm } = useTerminology();

	const [tasks, setTasks] = useState<IUpdateTask[]>([]);
	const [connections, setConnections] = useState<IConnectionHealth[]>([]);
	const [problems, setProblems] = useState<IRecentProblem[] | null>(null);
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);

	const refresh = useCallback(async () => {
		await readAndKeepWhatWasThereIfItFails(
			() => updateSubscriptionService.getRunningTasks(),
			setTasks,
		);

		await readAndKeepWhatWasThereIfItFails(
			() => connectionHealthService.getHealth(),
			setConnections,
		);

		await readAndKeepWhatWasThereIfItFails(
			() => logService.getRecentProblems(),
			setProblems,
		);
	}, [connectionHealthService, logService, updateSubscriptionService]);

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

	/**
	 * The answer comes back from the instance and replaces the row wholesale. Deciding locally what the
	 * test must have meant would let the header disagree with the popover it was opened from.
	 */
	const test = useCallback(
		async (connection: IConnectionHealth) => {
			try {
				const verdict = await connectionHealthService.testConnection(
					connection.connectionId,
				);

				// Which row the answer belongs to is read here rather than inside the updater below: React
				// runs that during a later render, so anything it throws lands outside this try and takes the
				// header down instead of leaving the row as it was.
				const answeredFor = verdict.connectionId;

				setConnections((current) =>
					current.map((candidate) =>
						candidate.connectionId === answeredFor ? verdict : candidate,
					),
				);
			} catch {
				// Nothing is claimed on the strength of a failed ask.
			}
		},
		[connectionHealthService],
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

	const headerState = describeHeaderState(connections);

	return (
		<>
			<Tooltip title={headerState}>
				<IconButton
					aria-label={headerState}
					color="inherit"
					onClick={(event) => setAnchor(event.currentTarget)}
				>
					<Badge
						badgeContent={tasks.length + connections.filter(isBroken).length}
						color={badgeColourFor(connections)}
					>
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

				<ActivitySection tasks={tasks} onCancel={(task) => void cancel(task)} />

				<Divider sx={{ my: 1.5 }} />

				<Typography variant="subtitle2" gutterBottom>
					{getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS)}
				</Typography>

				<ConnectionsSection
					connections={connections}
					onTest={(connection) => void test(connection)}
				/>

				<RecentProblemsSection problems={problems} />
			</Popover>
		</>
	);
};

export default TaskManagerIcon;
