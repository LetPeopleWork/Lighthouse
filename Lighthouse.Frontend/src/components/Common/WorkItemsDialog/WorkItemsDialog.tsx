import BlockIcon from "@mui/icons-material/Block";
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
	Tooltip,
	Typography,
} from "@mui/material";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	certainColor,
	confidentColor,
	getStateColor,
	hexToRgba,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

export type TimeMetric = "age" | "cycleTime" | "ageCycleTime";

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
	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	const sortedItems = [...items].sort((a, b) => {
		if (timeMetric === "ageCycleTime") {
			// For combined mode, sort by state category first (active items before done)
			if (a.stateCategory !== b.stateCategory) {
				if (a.stateCategory === "Done") return 1;
				if (b.stateCategory === "Done") return -1;
			}

			// Then sort by the appropriate time metric for each category
			if (a.stateCategory === "Done" && b.stateCategory === "Done") {
				return b.cycleTime - a.cycleTime;
			}
			return b.workItemAge - a.workItemAge;
		}

		// Original sorting for non-combined modes
		if (timeMetric === "age") {
			return b.workItemAge - a.workItemAge;
		}
		return b.cycleTime - a.cycleTime;
	});

	const formatTime = (days: number) => {
		return `${days} days`;
	};

	const getTimeColumnName = () => {
		if (timeMetric === "age") return workItemAgeTerm;
		if (timeMetric === "cycleTime") return cycleTimeTerm;
		return `${workItemAgeTerm}/${cycleTimeTerm}`;
	};

	const getTimeValue = (item: IWorkItem) => {
		if (timeMetric === "age") return item.workItemAge;
		if (timeMetric === "cycleTime") return item.cycleTime;
		// For combined mode, use cycle time for "Done" items and age for others
		return item.stateCategory === "Done" ? item.cycleTime : item.workItemAge;
	};

	const getTimeColor = (timeValue: number) => {
		if (!sle) return undefined;

		const seventyPercentSLE = sle * 0.7;
		const fiftyPercentSLE = sle * 0.5;

		// Using updated forecast colors with better contrast
		if (timeValue > sle) {
			return riskyColor; // Enhanced red
		}
		if (timeValue >= seventyPercentSLE) {
			return realisticColor; // Enhanced orange
		}
		if (timeValue >= fiftyPercentSLE) {
			return confidentColor; // Enhanced light green
		}
		return certainColor; // Enhanced green
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
								<TableCell>ID</TableCell>
								<TableCell>Name</TableCell>
								<TableCell>Type</TableCell>
								<TableCell>State</TableCell>
								<TableCell>{getTimeColumnName()}</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{sortedItems.map((item) => (
								<TableRow
									key={item.id}
									sx={{
										"&:hover": {
											backgroundColor: (theme) =>
												theme.palette.mode === "dark"
													? "rgba(255, 255, 255, 0.08)"
													: "rgba(0, 0, 0, 0.04)",
										},
									}}
								>
									<TableCell>
										{!item.name?.toLowerCase().includes("unparented")
											? item.referenceId
											: ""}
									</TableCell>
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
									<TableCell>
										<Typography
											variant="body2"
											sx={{
												color: getTimeColor(getTimeValue(item)),
												fontWeight: sle ? "bold" : "normal",
												padding: "4px 8px",
												borderRadius: 1,
												display: "inline-flex",
												alignItems: "center",
												backgroundColor: (theme) => {
													const timeColor = getTimeColor(getTimeValue(item));
													return timeColor
														? hexToRgba(
																timeColor ?? theme.palette.text.primary,
																0.1,
															)
														: "transparent";
												},
												// Ensure text remains readable on hover by increasing contrast
												"tr:hover &": {
													boxShadow: "0 0 0 1px rgba(0, 0, 0, 0.05)",
													position: "relative",
													zIndex: 1,
												},
											}}
										>
											{formatTime(getTimeValue(item))}
											{timeMetric === "ageCycleTime" && (
												<Typography
													variant="caption"
													sx={{ ml: 1, fontStyle: "italic" }}
												>
													{item.stateCategory === "Done"
														? `(${cycleTimeTerm})`
														: `(${workItemAgeTerm})`}
												</Typography>
											)}
											{item.isBlocked && (
												<Tooltip
													title={`This ${workItemTerm} is ${blockedTerm}`}
												>
													<BlockIcon
														sx={{
															color: "error.main",
															fontSize: "1rem",
															ml: 1,
														}}
													/>
												</Tooltip>
											)}
										</Typography>
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
