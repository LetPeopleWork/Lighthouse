import { useCallback, useContext, useEffect, useState } from "react";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { IConnectionHealth } from "../../../../services/Api/ConnectionHealthService";
import type { IRecentProblem } from "../../../../services/Api/LogService";
import type { IUpdateTask } from "../../../../services/UpdateSubscriptionService";
import { isStoppedByCancelAll, taskKey } from "./ActivitySection";

/**
 * An instance that cannot say what it is doing is not an instance doing nothing. A read that failed
 * therefore leaves what was there exactly as it was, rather than emptying it into a confident "nothing
 * is running", "every credential is fine" or "nothing has gone wrong" - all three of which are claims
 * nobody made.
 */
const readAndKeepWhatWasThereIfItFails = async <T>(
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

const alsoRemembering = (
	asked: ReadonlySet<string>,
	key: string,
): ReadonlySet<string> => new Set(asked).add(key);

const noLongerRemembering = (
	asked: ReadonlySet<string>,
	...keys: string[]
): ReadonlySet<string> => {
	const remaining = new Set(asked);
	for (const key of keys) {
		remaining.delete(key);
	}

	return remaining;
};

/**
 * Everything the Task Manager popover knows and everything it can ask the instance to do. It lives
 * apart from the drawing because the two change for different reasons: what this surface reads and how
 * it reacts to a failed ask is the instance's business, and which mark sits beside which word is the
 * reader's.
 *
 * `enabled` is false for anybody who is not a System Administrator. Nothing is read at all in that
 * case - the list names every entity on the instance, so not asking is part of the guard rather than a
 * shortcut.
 */
export const useTaskManagerPopover = (enabled: boolean) => {
	const { updateSubscriptionService, connectionHealthService, logService } =
		useContext(ApiServiceContext);

	const [tasks, setTasks] = useState<IUpdateTask[]>([]);
	const [connections, setConnections] = useState<IConnectionHealth[]>([]);
	const [problems, setProblems] = useState<IRecentProblem[] | null>(null);
	const [stopAsked, setStopAsked] = useState<ReadonlySet<string>>(new Set());
	const [cancellingAll, setCancellingAll] = useState(false);
	const [cancelAllFailed, setCancelAllFailed] = useState(false);

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
	 * broken. What it does not say is that the work stopped: dropping the row here would claim an outcome
	 * nobody agreed to, and it would come back on the next read whenever the ask arrived too late.
	 *
	 * The list is re-read rather than edited in place, so what is on screen is always the instance's
	 * answer rather than this component's guess.
	 */
	const cancel = useCallback(
		async (task: IUpdateTask) => {
			const key = taskKey(task);
			setStopAsked((asked) => alsoRemembering(asked, key));

			try {
				await updateSubscriptionService.cancelTask(task.updateType, task.id);
			} catch {
				// The instance refused the ask or never heard it, so the row must stop saying it is stopping.
				// A deletion is refused by design, and a row stuck on a word that will never come true is
				// how the word stops meaning anything.
				setStopAsked((asked) => noLongerRemembering(asked, key));
			}

			await refresh();
		},
		[refresh, updateSubscriptionService],
	);

	/**
	 * Every row it can stop says so at once, for the same reason a single stop does. Only the rows this
	 * click marked are put back if the instance refuses: a row somebody stopped on its own a moment
	 * earlier is still being stopped by that earlier ask.
	 */
	const cancelAll = useCallback(async () => {
		const marked = tasks
			.filter(isStoppedByCancelAll)
			.map(taskKey)
			.filter((key) => !stopAsked.has(key));

		setCancelAllFailed(false);
		setCancellingAll(true);
		setStopAsked((asked) => new Set([...asked, ...marked]));

		try {
			await updateSubscriptionService.cancelAllTasks();
		} catch {
			setStopAsked((asked) => noLongerRemembering(asked, ...marked));
			setCancelAllFailed(true);
		} finally {
			setCancellingAll(false);
		}

		await refresh();
	}, [refresh, stopAsked, tasks, updateSubscriptionService]);

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
		if (!enabled) {
			return;
		}

		let gone = false;
		const reread = () => {
			if (!gone) {
				void refresh();
			}
		};

		reread();
		void updateSubscriptionService.subscribeToAllUpdates(reread);

		return () => {
			gone = true;
			void updateSubscriptionService.unsubscribeFromAllUpdates();
		};
	}, [enabled, refresh, updateSubscriptionService]);

	return {
		tasks,
		connections,
		problems,
		stopAsked,
		cancellingAll,
		cancelAllFailed,
		refresh,
		cancel,
		cancelAll,
		test,
	};
};
