import { Box, Card, CardContent, Typography, useTheme } from "@mui/material";
import { PieChart } from "@mui/x-charts";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { hexToRgba } from "../../../utils/theme/colors";
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
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
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

	// Generate colors based on theme
	const generateColors = (count: number): string[] => {
		const baseColor = theme.palette.primary.main;
		const colors: string[] = [];

		for (let i = 0; i < count; i++) {
			const opacity = 0.4 + (i / count) * 0.6; // Range from 0.4 to 1.0
			colors.push(hexToRgba(baseColor, opacity));
		}

		return colors;
	};

	const colors = generateColors(pieData.length);

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
							width: "100%",
							height: "100%",
						}}
					>
						<PieChart
							series={[
								{
									data: pieData.map((item, index) => ({
										id: item.id,
										value: item.value,
										label: item.label,
										color: colors[index],
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
				</CardContent>
			</Card>

			<WorkItemsDialog
				title={dialogTitle}
				items={selectedItems}
				open={dialogOpen}
				onClose={handleCloseDialog}
				additionalColumnTitle={cycleTimeTerm}
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.cycleTime}
			/>
		</>
	);
};

export default WorkDistributionChart;
