import CloseIcon from "@mui/icons-material/Close";
import {
	Chip,
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
import {
	certainColor,
	confidentColor,
	getStateColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

export type TimeMetric = "age" | "cycleTime";

interface WorkItemsDialogProps {
	title: string;
	items: IWorkItem[];
	open: boolean;
	onClose: () => void;
	timeMetric?: TimeMetric;
	sle?: number;
}

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	timeMetric = "age",
	sle,
}) => {
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

	const getTimeColor = (timeValue: number) => {
		if (!sle) return undefined;

		const seventyPercentSLE = sle * 0.7;
		const fiftyPercentSLE = sle * 0.5;

		if (timeValue > sle) {
			return riskyColor;
		}
		if (timeValue >= seventyPercentSLE) {
			return realisticColor;
		}
		if (timeValue >= fiftyPercentSLE) {
			return confidentColor;
		}
		return certainColor;
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
									<TableCell>
										<Chip
											size="small"
											label={item.state}
											color={getStateColor(item.stateCategory)}
											variant="outlined"
										/>
									</TableCell>
									<TableCell
										sx={{
											color: getTimeColor(getTimeValue(item)),
											fontWeight: sle ? "bold" : "normal",
										}}
									>
										{formatTime(getTimeValue(item))}
									</TableCell>
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
