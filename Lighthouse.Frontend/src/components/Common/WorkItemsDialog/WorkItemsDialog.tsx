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
import type { IFeature } from "../../../models/Feature";
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

export interface WorkItemsDialogProps {
	title: string;
	items: IWorkItem[];
	open: boolean;
	onClose: () => void;
	additionalColumnTitle: string;
	additionalColumnDescription: string;
	additionalColumnContent: (workItem: IWorkItem) => number;
	sle?: number;
}

const WorkItemsDialog: React.FC<WorkItemsDialogProps> = ({
	title,
	items,
	open,
	onClose,
	additionalColumnTitle,
	additionalColumnDescription,
	additionalColumnContent,
	sle,
}) => {
	const { getTerm } = useTerminology();
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	// Check if items are Features with owning team
	const isFeature = (item: IWorkItem): item is IFeature => {
		return 'owningTeam' in item;
	};

	const hasOwningTeams = items.length > 0 && items.some(item => 
		isFeature(item) && item.owningTeam && item.owningTeam.trim() !== ''
	);

	const sortedItems = [...items].sort((a, b) => {
		return additionalColumnContent(b) - additionalColumnContent(a);
	});

	const getColumnColor = (value: number) => {
		if (!sle) return undefined;

		const seventyPercentSLE = sle * 0.7;
		const fiftyPercentSLE = sle * 0.5;

		// Using updated forecast colors with better contrast
		if (value > sle) {
			return riskyColor; // Enhanced red
		}
		if (value >= seventyPercentSLE) {
			return realisticColor; // Enhanced orange
		}
		if (value >= fiftyPercentSLE) {
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
								{hasOwningTeams && <TableCell>Owned by</TableCell>}
								<TableCell>
									{additionalColumnTitle}

									<Typography
										variant="caption"
										sx={{ ml: 1, fontStyle: "italic" }}
									>
										({additionalColumnDescription})
									</Typography>
								</TableCell>
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
									{hasOwningTeams && (
										<TableCell>
											{isFeature(item) ? item.owningTeam : ''}
										</TableCell>
									)}
									<TableCell>
										<Typography
											variant="body2"
											data-testid="additionalColumnContent"
											sx={{
												color: getColumnColor(additionalColumnContent(item)),
												fontWeight: sle ? "bold" : "normal",
												padding: "4px 8px",
												borderRadius: 1,
												display: "inline-flex",
												alignItems: "center",
												backgroundColor: (theme) => {
													const timeColor = getColumnColor(
														additionalColumnContent(item),
													);
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
											{additionalColumnContent(item)}
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
