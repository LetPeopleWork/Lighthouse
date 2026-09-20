import AutorenewIcon from "@mui/icons-material/Autorenew";
import CloseIcon from "@mui/icons-material/Close";
import HubIcon from "@mui/icons-material/Hub";
import TimelineIcon from "@mui/icons-material/Timeline";
import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import { useTheme } from "@mui/material/styles";
import Tooltip from "@mui/material/Tooltip";
import { useState } from "react";
import { useRbac } from "../../../hooks/useRbac";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import ActivitySection from "./TaskManager/ActivitySection";
import ConnectionsSection from "./TaskManager/ConnectionsSection";
import {
	ACTIVITY_LABEL,
	badgeColourFor,
	describeHeaderState,
	isBroken,
} from "./TaskManager/connectionHealthWording";
import RecentProblemsSection from "./TaskManager/RecentProblemsSection";
import SectionHeading from "./TaskManager/SectionHeading";
import { useTaskManagerPopover } from "./TaskManager/useTaskManagerPopover";

const TaskManagerIcon = () => {
	const theme = useTheme();
	const { isSystemAdmin } = useRbac();
	const { getTerm } = useTerminology();

	const { tasks, connections, problems, stopAsked, refresh, cancel, test } =
		useTaskManagerPopover(isSystemAdmin);

	const [anchor, setAnchor] = useState<HTMLElement | null>(null);

	const showWhatIsHappeningNow = (opener: HTMLElement) => {
		setAnchor(opener);

		// The only other thing that provokes a read is refresh activity, so a work tracking system added
		// a minute ago is missing from this box until the page is reloaded. Opening it is the moment
		// somebody wants the answer to be current, and the only moment worth spending a read on - a box
		// nobody opened needs no fresh answer.
		void refresh();
	};

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
					onClick={(event) => showWhatIsHappeningNow(event.currentTarget)}
				>
					<Badge
						badgeContent={tasks.length + connections.filter(isBroken).length}
						color={badgeColourFor(connections)}
					>
						<TimelineIcon style={{ color: theme.palette.primary.main }} />
					</Badge>
				</IconButton>
			</Tooltip>

			<Popover
				open={anchor !== null}
				anchorEl={anchor}
				onClose={() => setAnchor(null)}
				anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
				transformOrigin={{ vertical: "top", horizontal: "right" }}
				// A popover is already bounded to the window's height and scrolls. Its width is not bounded
				// by anything but its widest line, and one refused Jira query is several hundred unbroken
				// characters - enough to push the box, and everything in it, off the side of the screen.
				slotProps={{ paper: { sx: { p: 2, minWidth: 320, maxWidth: 520 } } }}
			>
				<Box
					sx={{
						display: "flex",
						justifyContent: "flex-end",
						mt: -1.5,
						mr: -1.5,
					}}
				>
					{/* Clicking beside the box is the only way out MUI gives away for free, and it is the
					    first thing to go when the box fills the screen. */}
					<IconButton
						aria-label="Close"
						size="small"
						onClick={() => setAnchor(null)}
					>
						<CloseIcon fontSize="small" />
					</IconButton>
				</Box>

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
