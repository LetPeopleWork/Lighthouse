import { Link, Typography } from "@mui/material";
import type React from "react";
import { Link as RouterLink } from "react-router";
import { useRbac } from "../../hooks/useRbac";
import { connectionsTabPath } from "../../models/Settings/settingsAreas";

/** Says where to connect the first connection, under the Add buttons it keeps disabled. */
const ConnectFirstHint: React.FC<{
	hasConnections: boolean;
	connectionTerm: string;
}> = ({ hasConnections, connectionTerm }) => {
	const { isLoading, isSystemAdmin } = useRbac();
	if (isLoading || !isSystemAdmin || hasConnections) {
		return null;
	}
	return (
		<Typography variant="body2" color="text.secondary">
			{`Connect a ${connectionTerm} first: `}
			<Link component={RouterLink} to={connectionsTabPath()}>
				Settings › System › Connections
			</Link>
		</Typography>
	);
};

export default ConnectFirstHint;
