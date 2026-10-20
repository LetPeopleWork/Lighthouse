import {
	Box,
	Breadcrumbs,
	Button,
	CircularProgress,
	Link,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { Link as RouterLink } from "react-router";

interface ThenAndNowHeaderProps {
	reportsPath: string;
	/** Unknown until the report has been read; the way back is there from the start. */
	name?: string;
	/** Given while the report is on its way and once it has been read and can be printed. */
	onExport?: () => void;
	/** The report is still on its way: Export PDF shows where it will be, but nothing is there to print yet. */
	exportWaiting?: boolean;
	/** Its PDF is being prepared: Export PDF shows the waiting spinner and ignores a second choice, but keeps the focus. */
	exportPreparing?: boolean;
	/** Given only to somebody who may edit the report, once it has been read. */
	onEdit?: () => void;
	/** Given only to somebody who may delete the report, once it has been read. */
	onDelete?: () => void;
}

// Disabled only in look while preparing: a truly disabled button would throw a keyboard reader's focus to the page.
const PREPARING_LOOK = {
	color: "action.disabled",
	borderColor: "action.disabledBackground",
	cursor: "default",
};

const ExportPdfButton: React.FC<{
	readonly onExport: () => void;
	readonly waiting: boolean;
	readonly preparing: boolean;
}> = ({ onExport, waiting, preparing }) => (
	<Button
		variant="outlined"
		onClick={preparing ? undefined : onExport}
		disabled={waiting}
		aria-disabled={preparing ? "true" : undefined}
		aria-busy={preparing ? "true" : undefined}
		startIcon={preparing ? <CircularProgress size={20} /> : undefined}
		sx={preparing ? PREPARING_LOOK : undefined}
	>
		Export PDF
	</Button>
);

const ThenAndNowHeader: React.FC<Readonly<ThenAndNowHeaderProps>> = ({
	reportsPath,
	name,
	onExport,
	exportWaiting = false,
	exportPreparing = false,
	onEdit,
	onDelete,
}) => (
	<Box
		sx={{
			display: "flex",
			alignItems: "center",
			justifyContent: "space-between",
			gap: 2,
		}}
	>
		<Breadcrumbs aria-label="breadcrumb">
			<Link
				component={RouterLink}
				to={reportsPath}
				underline="hover"
				variant="body1"
			>
				Reports
			</Link>
			{name !== undefined && (
				<Typography variant="body1" color="text.primary">
					{name}
				</Typography>
			)}
		</Breadcrumbs>
		<Stack direction="row" spacing={1}>
			{onExport !== undefined && (
				<ExportPdfButton
					onExport={onExport}
					waiting={exportWaiting}
					preparing={exportPreparing}
				/>
			)}
			{onEdit !== undefined && (
				<Button variant="outlined" onClick={onEdit}>
					Edit report
				</Button>
			)}
			{onDelete !== undefined && (
				<Button variant="outlined" color="error" onClick={onDelete}>
					Delete
				</Button>
			)}
		</Stack>
	</Box>
);

export default ThenAndNowHeader;
