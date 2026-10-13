import { Breadcrumbs, Link, Typography } from "@mui/material";
import type React from "react";
import { Link as RouterLink } from "react-router";

interface ThenAndNowHeaderProps {
	reportsPath: string;
	/** Unknown until the report has been read; the way back is there from the start. */
	name?: string;
}

const ThenAndNowHeader: React.FC<Readonly<ThenAndNowHeaderProps>> = ({
	reportsPath,
	name,
}) => (
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
);

export default ThenAndNowHeader;
