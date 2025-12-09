import {
	Box,
	Card,
	CardContent,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	Typography,
	useTheme,
} from "@mui/material";
import { PieChart } from "@mui/x-charts";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getColorMapForKeys, hexToRgba } from "../../../utils/theme/colors";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

interface WorkDistributionChartProps {
	workItems: IWorkItem[];
	title?: string;
}

interface WorkDistributionData {
	id: string;
	value: number;
	label: string;
	items: IWorkItem[];
}

const WorkDistributionChart: React.FC<WorkDistributionChartProps> = ({
	workItems,
	title = "Work Distribution by Parent",
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();

	const workItemsTerm =
		workItems.length > 0 && "owningTeam" in workItems[0]
			? getTerm(TERMINOLOGY_KEYS.FEATURES)
			: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const { featureService } = useContext(ApiServiceContext);

	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogTitle, setDialogTitle] = useState<string>("");
	const [parentNames, setParentNames] = useState<Map<string, string>>(
		new Map(),
	);

	// Fetch parent work item names
	useEffect(() => {
		const fetchParentNames = async () => {
			// Get unique parent references (excluding "No Parent")
			const parentRefs = Array.from(
				new Set(
					workItems
						.map((item) => item.parentWorkItemReference)
						.filter((ref) => ref && ref.trim() !== ""),
				),
			);

			if (parentRefs.length === 0) {
				return;
			}

			try {
				const features =
					await featureService.getFeaturesByReferences(parentRefs);
				const nameMap = new Map<string, string>();

				for (const feature of features) {
					nameMap.set(feature.referenceId, feature.name);
				}

				setParentNames(nameMap);
			} catch (error) {
				console.error("Failed to fetch parent work item names:", error);
			}
		};

		fetchParentNames();
	}, [workItems, featureService]);

	// Group work items by parent reference
	const groupedData = workItems.reduce(
		(acc, item) => {
			const parent = item.parentWorkItemReference || "No Parent";
			if (!acc[parent]) {
				acc[parent] = [];
			}
			acc[parent].push(item);
			return acc;
		},
		{} as Record<string, IWorkItem[]>,
	);

	// Convert to pie chart data format with parent names
	const pieData: WorkDistributionData[] = Object.entries(groupedData).map(
		([parent, items]) => {
			// Use parent name if available, otherwise fall back to reference ID
			const displayName =
				parent === "No Parent"
					? "No Parent"
					: parentNames.get(parent) || parent;

			return {
				id: parent,
				value: items.length,
				label: displayName,
				items: items,
			};
		},
	);

	// Sort by value descending for better visualization
	pieData.sort((a, b) => b.value - a.value);

	// Generate a deterministic color map for the parents using the helper
	// Memoize to ensure the color mapping stays stable across re-renders
	const colorMap = useMemo(() => {
		const map = pieData.map((p) => p.id);
		return getColorMapForKeys(map);
	}, [pieData]);

	const handlePieClick = (
		_event: React.MouseEvent<SVGPathElement>,
		itemIdentifier: { dataIndex: number },
	) => {
		const dataIndex = itemIdentifier.dataIndex;
		if (dataIndex !== undefined && pieData[dataIndex]) {
			const selectedData = pieData[dataIndex];
			setDialogTitle(
				`${workItemsTerm} for ${selectedData.label} (${selectedData.value} items)`,
			);
			setSelectedItems(selectedData.items);
			setDialogOpen(true);
		}
	};

	const handleCloseDialog = () => {
		setDialogOpen(false);
	};

	const handleTableRowClick = (dataIndex: number) => {
		if (dataIndex !== undefined && pieData[dataIndex]) {
			const selectedData = pieData[dataIndex];
			setDialogTitle(
				`${workItemsTerm} for ${selectedData.label} (${selectedData.value} items)`,
			);
			setSelectedItems(selectedData.items);
			setDialogOpen(true);
		}
	};

	if (workItems.length === 0) {
		return (
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent>
					<Typography variant="h6" gutterBottom>
						{title}
					</Typography>
					<Typography variant="body2" color="text.secondary">
						No work items to display
					</Typography>
				</CardContent>
			</Card>
		);
	}

	return (
		<>
			<Card sx={{ p: 1, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{
						height: "100%",
						display: "flex",
						flexDirection: "column",
						p: 0,
						"&:last-child": { pb: 0 },
					}}
				>
					<Typography variant="h6" gutterBottom sx={{ px: 1, pt: 1, pb: 0.5 }}>
						{title}
					</Typography>
					<Box
						sx={{
							flex: 1,
							minHeight: 0,
							display: "flex",
							flexDirection: "row",
							gap: 2,
							width: "100%",
							height: "100%",
						}}
					>
						{/* Pie Chart */}
						<Box
							sx={{
								flex: 1,
								minWidth: 0,
								minHeight: 0,
								display: "flex",
								width: "100%",
								height: "100%",
							}}
						>
							<PieChart
								series={[
									{
										data: pieData.map((item) => ({
											id: item.id,
											value: item.value,
											label: item.label,
											color:
												colorMap[item.id] ||
												hexToRgba(theme.palette.primary.main, 1),
										})),
										highlightScope: { fade: "global", highlight: "item" },
										faded: {
											innerRadius: 30,
											additionalRadius: -30,
											color: "gray",
										},
										valueFormatter: (item: { value: number }) => {
											const percentage = (
												(item.value / workItems.length) *
												100
											).toFixed(1);
											return `${item.value} items (${percentage}%)`;
										},
									},
								]}
								onItemClick={handlePieClick}
								margin={{ top: 5, bottom: 5, left: 5, right: 5 }}
								hideLegend={true}
								sx={{
									cursor: "pointer",
									width: "100%",
									height: "100%",
									"& .MuiPieArc-root": {
										cursor: "pointer",
									},
								}}
							/>
						</Box>

						{/* Table View */}
						<Box
							sx={{
								flex: 1,
								minWidth: 0,
								overflow: "auto",
								maxHeight: "100%",
							}}
						>
							<Table size="small" sx={{ minWidth: 200 }}>
								<TableHead>
									<TableRow>
										<TableCell sx={{ py: 1, fontWeight: 600 }}>Name</TableCell>
										<TableCell align="right" sx={{ py: 1, fontWeight: 600 }}>
											%
										</TableCell>
										<TableCell align="right" sx={{ py: 1, fontWeight: 600 }}>
											{workItemsTerm}
										</TableCell>
									</TableRow>
								</TableHead>
								<TableBody>
									{pieData.map((item, index) => {
										const percentage = (
											(item.value / workItems.length) *
											100
										).toFixed(1);
										return (
											<TableRow
												key={item.id}
												onClick={() => handleTableRowClick(index)}
												sx={{
													cursor: "pointer",
													"&:hover": {
														backgroundColor: theme.palette.action.hover,
													},
												}}
											>
												<TableCell
													sx={{
														py: 1,
														display: "flex",
														alignItems: "center",
														gap: 1,
													}}
												>
													<Box
														sx={{
															width: 12,
															height: 12,
															borderRadius: "50%",
															backgroundColor:
																colorMap[item.id] ||
																hexToRgba(theme.palette.primary.main, 1),
															flexShrink: 0,
														}}
														data-testid={`color-box-${index}`}
													/>
													<Typography
														variant="body2"
														sx={{
															fontWeight: 500,
															overflow: "hidden",
															textOverflow: "ellipsis",
														}}
													>
														{item.label}
													</Typography>
												</TableCell>
												<TableCell
													align="right"
													sx={{ py: 1, whiteSpace: "nowrap" }}
												>
													<Typography variant="body2" color="text.secondary">
														{percentage}%
													</Typography>
												</TableCell>
												<TableCell
													align="right"
													sx={{ py: 1, whiteSpace: "nowrap" }}
												>
													<Typography variant="body2">{item.value}</Typography>
												</TableCell>
											</TableRow>
										);
									})}
								</TableBody>
							</Table>
						</Box>
					</Box>
				</CardContent>
			</Card>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				additionalColumnTitle={`${cycleTimeTerm}/${workItemAgeTerm}`}
				additionalColumnDescription="days"
				additionalColumnContent={(item) =>
					item.cycleTime > 0 ? item.cycleTime : item.workItemAge
				}
			/>
		</>
	);
};

export default WorkDistributionChart;
