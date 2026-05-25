import { Box, Card, CardContent, Chip, Typography } from "@mui/material";
import { useState } from "react";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";

export interface InProgressEntry {
	title: string;
	items: IWorkItem[];
	idealWip?: number;
	sle?: number;
}

interface ItemsInProgressProps {
	entries: InProgressEntry[];
}

const ItemsInProgress: React.FC<ItemsInProgressProps> = ({ entries = [] }) => {
	const [openIndex, setOpenIndex] = useState<number | null>(null);

	const { getTerm } = useTerminology();
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	const getChipColor = (count: number, ideal?: number) => {
		if (ideal == null) return "default";
		if (count === ideal) return "success";
		if (count < ideal) return "warning";
		return "error";
	};

	const handleOpen = (idx: number) => setOpenIndex(idx);
	const handleClose = () => setOpenIndex(null);

	let layoutMode: "center" | "two" | "stretch" = "stretch";
	if (entries.length === 1) layoutMode = "center";
	else if (entries.length === 2) layoutMode = "two";

	let justifyContentValue: "stretch" | "center" | "space-evenly" = "stretch";
	if (layoutMode === "center") justifyContentValue = "center";
	return (
		<>
			{entries.length > 0 && (
				<Card
					key="items-in-progress-single-card"
					sx={{
						m: 0,
						p: 0,
						borderRadius: 2,
						width: "100%",
						height: "100%",
						minHeight: 0,
						display: "flex",
						flexDirection: "column",
						boxSizing: "border-box",
						overflow: "hidden",
					}}
				>
					<CardContent
						sx={{
							// use flex column so each row can be a flex child and center its content
							display: "flex",
							flexDirection: "column",
							gap: 0,
							width: "100%",
							minWidth: 0,
							flex: "1 1 0",
							p: 0,
							boxSizing: "border-box",
							// when there is 1 row center it, 2 rows place space-between, 3+ stretch rows equally
							justifyContent: justifyContentValue,
							// ensure a minimum height so two rows can visibly split into halves
							minHeight: { xs: 120, sm: 160 },
						}}
					>
						{entries.map((entry, idx) => {
							const count = entry.items?.length ?? 0;
							// compute flex for each row: stretch => fill equally; two => each row is 50% height; otherwise keep initial
							const rowFlex = (() => {
								if (layoutMode === "stretch") return 1;
								if (layoutMode === "two") return "1 1 50%";
								return "initial";
							})();
							return (
								<Box
									key={entry.title || `items-in-progress-${idx}`}
									onClick={() => handleOpen(idx)}
									sx={(theme) => ({
										// make each row a flex child that grows equally so its children can center vertically
										display: "flex",
										alignItems: "center",
										gap: 1,
										width: "100%",
										minWidth: 0,
										p: 2,
										boxSizing: "border-box",
										cursor: "pointer",
										flex: rowFlex,
										// draw a 1px bottom separator using background so it doesn't affect layout
										backgroundImage:
											idx < entries.length - 1
												? `linear-gradient(to bottom, rgba(0,0,0,0) calc(100% - 1px), ${theme.palette.divider} 1px)`
												: "none",
										backgroundRepeat: "no-repeat",
										backgroundPosition: "bottom",
										backgroundSize: "100% 1px",
									})}
								>
									{/* Title column - flexible (vertically centered) */}
									<Box
										sx={{
											flex: 1,
											minWidth: 0,
											overflow: "hidden",
											display: "flex",
											alignItems: "center",
										}}
									>
										<Typography
											variant="h6"
											noWrap
											sx={{ overflow: "hidden", textOverflow: "ellipsis" }}
											style={{ fontSize: "clamp(0.9rem, 1.6vh, 1.1rem)" }}
										>
											{entry.title}
										</Typography>
									</Box>

									{/* Count column - fixed width to align numbers */}
									<Box
										sx={{
											width: { xs: 40, sm: 56 },
											display: "flex",
											justifyContent: "center",
											alignItems: "center",
											flexShrink: 0,
										}}
									>
										<Typography
											variant="h4"
											sx={{ lineHeight: 1 }}
											style={{ fontSize: "clamp(1.1rem, 3.4vh, 2rem)" }}
										>
											{count}
										</Typography>
									</Box>

									{/* Chip column - fixed width so count stays aligned when chip missing */}
									<Box
										sx={{
											width: { xs: 44, sm: 80 },
											display: "flex",
											justifyContent: "flex-start",
											alignItems: "center",
											flexShrink: 0,
										}}
									>
										{entry.idealWip != null && entry.idealWip >= 0 && (
											<Chip
												label={`Goal: ${entry.idealWip}`}
												color={getChipColor(count, entry.idealWip)}
												size="small"
												sx={{
													ml: 1,
													flexShrink: 0,
													"& .MuiChip-label": {
														fontSize: "clamp(0.6rem, 1.2vh, 0.85rem)",
													},
												}}
											/>
										)}
									</Box>
								</Box>
							);
						})}
					</CardContent>
				</Card>
			)}

			{openIndex !== null && (
				<WorkItemsDialog
					title={entries[openIndex].title}
					items={entries[openIndex].items}
					open={openIndex !== null}
					onClose={handleClose}
					highlightColumn={{
						title: workItemAgeTerm,
						description: "days",
						valueGetter: (item) => item.workItemAge,
					}}
					sle={entries[openIndex].sle}
				/>
			)}
		</>
	);
};

export default ItemsInProgress;
