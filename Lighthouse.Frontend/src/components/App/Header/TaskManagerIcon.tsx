import AutorenewIcon from "@mui/icons-material/Autorenew";
import HubIcon from "@mui/icons-material/Hub";
import TimelineIcon from "@mui/icons-material/Timeline";
import Badge from "@mui/material/Badge";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import Tooltip from "@mui/material/Tooltip";
import { useCallback, useContext, useEffect, useState } from "react";
import { useRbac } from "../../../hooks/useRbac";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IConnectionHealth } from "../../../services/Api/ConnectionHealthService";
import type { IRecentProblem } from "../../../services/Api/LogService";
import { useTerminology } from "../../../services/TerminologyContext";
import type { IUpdateTask } from "../../../services/UpdateSubscriptionService";
import ActivitySection, { taskKey } from "./TaskManager/ActivitySection";
import ConnectionsSection from "./TaskManager/ConnectionsSection";
import {
	ACTIVITY_LABEL,
	badgeColourFor,
	describeHeaderState,
	isBroken,
} from "./TaskManager/connectionHealthWording";
import RecentProblemsSection from "./TaskManager/RecentProblemsSection";
import SectionHeading from "./TaskManager/SectionHeading";

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

/**
 * A row the browser asked to stop stops being one the moment the instance stops reporting the work. It
 * is dropped rather than kept, so a key that comes back — because the ask arrived after the refresh had
 * already been requeued — reads as running again rather than as forever stopping.
 *
 * The same set is returned when there is nothing to drop, so an unremarkable re-read does not make the
 * popover render again.
 */
const forgetWhatTheInstanceNoLongerReports = (
	asked: ReadonlySet<string>,
	stillListed: IUpdateTask[],
): ReadonlySet<string> => {
	if (asked.size === 0) {
		return asked;
	}

	const present = new Set(stillListed.map(taskKey));
	const kept = [...asked].filter((key) => present.has(key));

	return kept.length === asked.size ? asked : new Set(kept);
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
	const [stopAsked, setStopAsked] = useState<ReadonlySet<string>>(new Set());

	const refresh = useCallback(async () => {
		await readAndKeepWhatWasThereIfItFails(
			() => updateSubscriptionService.getRunningTasks(),
			(running) => {
				setTasks(running);
				setStopAsked((asked) =>
					forgetWhatTheInstanceNoLongerReports(asked, running),
				);
			},
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
	 * The row says the stop was asked for straight away, because the connector will not notice until its
	 * next checkpoint and a control that does nothing visible for eleven seconds teaches the reader it is
	 * broken. What it does not say is that the work stopped: removing the row here would claim an outcome
	 * nobody agreed to, and it would come back on the next read whenever the ask arrived too late.
	 *
	 * The list is re-read rather than edited in place, so what is on screen is always the instance's answer
	 * rather than this component's guess.
	 */
	const cancel = useCallback(
		async (task: IUpdateTask) => {
			const key = taskKey(task);
			setStopAsked((asked) => new Set(asked).add(key));

			try {
				await updateSubscriptionService.cancelTask(task.updateType, task.id);
			} catch {
				// The instance refused the ask or never heard it, so the row must stop saying it is stopping.
				// A deletion is refused by design, and a row stuck on a word that will never come true is
				// how the word stops meaning anything.
				setStopAsked((asked) => {
					const withoutIt = new Set(asked);
					withoutIt.delete(key);
					return withoutIt;
				});
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
					onClick={(event) => {
						setAnchor(event.currentTarget);

						// The only other thing that provokes a read is refresh activity, so a work tracking
						// system added a minute ago is missing from this box until the page is reloaded.
						// Opening it is the moment somebody wants the answer to be current, and the only moment
						// worth spending a read on - a box nobody opened needs no fresh answer.
						void refresh();
					}}
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
				<SectionHeading
					testId="task-manager-section-activity"
					icon={<AutorenewIcon fontSize="small" color="action" />}
				>
					{ACTIVITY_LABEL}
				</SectionHeading>

				<ActivitySection
					tasks={tasks}
					stopAsked={stopAsked}
					onCancel={(task) => void cancel(task)}
				/>

				<Divider sx={{ my: 1.5 }} />

				<SectionHeading
					testId="task-manager-section-connections"
					icon={<HubIcon fontSize="small" color="action" />}
				>
					{getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS)}
				</SectionHeading>

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
