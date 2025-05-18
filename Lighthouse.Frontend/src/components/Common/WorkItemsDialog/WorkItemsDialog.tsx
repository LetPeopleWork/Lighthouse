import CloseIcon from "@mui/icons-material/Close";
import {
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	Link,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	Typography,
} from "@mui/material";
import type { IWorkItem } from "../../../models/WorkItem";

export type TimeMetric = "age" | "cycleTime";

interface WorkItemsDialogProps {
	title: string;
	items: IWorkItem[];
	open: boolean;
	onClose: () => void;
	timeMetric?: TimeMetric;
}

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	timeMetric = "age",
}) => {
	// Sort items by the specified metric (oldest/longest first)
	const sortedItems = [...items].sort((a, b) => {
		if (timeMetric === "age") {
			return b.workItemAge - a.workItemAge;
		}
		return b.cycleTime - a.cycleTime;
	});

	const formatTime = (days: number) => {
		return `${days} days`;
	};

	const getTimeColumnName = () => {
		return timeMetric === "age" ? "Age" : "Cycle Time";
	};

	const getTimeValue = (item: IWorkItem) => {
		return timeMetric === "age" ? item.workItemAge : item.cycleTime;
	};

	return (
		<Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
			<DialogTitle>
				{title}
				<IconButton
					onClick={onClose}
					sx={{ position: "absolute", right: 8, top: 8 }}
				>
					<CloseIcon />
				</IconButton>
			</DialogTitle>
			<DialogContent>
				{items.length > 0 ? (
					<Table>
						<TableHead>
							<TableRow>
								<TableCell>Name</TableCell>
								<TableCell>Type</TableCell>
								<TableCell>State</TableCell>
								<TableCell>{getTimeColumnName()}</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{sortedItems.map((item) => (
								<TableRow key={item.id}>
									<TableCell
										sx={{
											whiteSpace: "normal",
											wordBreak: "break-word",
											maxWidth: "300px",
										}}
									>
										{item.url ? (
											<Link
												href={item.url}
												target="_blank"
												rel="noopener noreferrer"
											>
												{item.name}
											</Link>
										) : (
											item.name
										)}
									</TableCell>
									<TableCell>{item.type}</TableCell>
									<TableCell>{item.state}</TableCell>
									<TableCell>{formatTime(getTimeValue(item))}</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				) : (
					<Typography variant="body2" color="text.secondary">
						No items to display
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default WorkItemsDialog;
