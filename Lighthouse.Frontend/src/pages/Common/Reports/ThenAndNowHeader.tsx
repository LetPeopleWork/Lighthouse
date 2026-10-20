import {
	Box,
	Breadcrumbs,
	Button,
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
	/** Given once the report has been read and can be printed. */
	onExport?: () => void;
	/** Given only to somebody who may edit the report, once it has been read. */
	onEdit?: () => void;
	/** Given only to somebody who may delete the report, once it has been read. */
	onDelete?: () => void;
}

const ThenAndNowHeader: React.FC<Readonly<ThenAndNowHeaderProps>> = ({
	reportsPath,
	name,
	onExport,
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
				<Button variant="outlined" onClick={onExport}>
					Export PDF
				</Button>
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
