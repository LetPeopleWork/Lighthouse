import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import { useNavigate } from "react-router";
import type { IConnectionHealth } from "../../../../services/Api/ConnectionHealthService";
import { CONNECTION_STATE_WORDING, isBroken } from "./connectionHealthWording";

interface ConnectionsSectionProps {
	connections: IConnectionHealth[];
	onTest: (connection: IConnectionHealth) => void;
}

const ConnectionsSection = ({
	connections,
	onTest,
}: ConnectionsSectionProps) => {
	const navigate = useNavigate();

	if (connections.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				No connections are configured.
			</Typography>
		);
	}

	return connections.map((connection) => (
		<Box key={connection.connectionId} sx={{ py: 0.5 }}>
			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				<Typography
					data-testid={`connection-health-row-${connection.connectionId}`}
					variant="body2"
					sx={{ flexGrow: 1 }}
				>
					{connection.connectionName} —{" "}
					{CONNECTION_STATE_WORDING[connection.state]}
				</Typography>

				<Button
					size="small"
					onClick={() => onTest(connection)}
					aria-label={`Test connection ${connection.connectionName}`}
				>
					Test connection
				</Button>

				<Button
					size="small"
					onClick={() =>
						navigate(`/connections/${connection.connectionId}/edit`)
					}
					aria-label={`Edit connection ${connection.connectionName}`}
				>
					Edit
				</Button>
			</Box>

			{/* The state says something is wrong; this is the half that says what to do about it, and it
			    is the difference between reissuing the right credential and the wrong one. Shown rather
			    than tucked into a tooltip: nobody hovers a row to find out whether they need to. */}
			{isBroken(connection) && connection.message ? (
				<Typography
					data-testid={`connection-health-explanation-${connection.connectionId}`}
					variant="caption"
					color="text.secondary"
					sx={{ display: "block" }}
				>
					{connection.message}
				</Typography>
			) : null}
		</Box>
	));
};

export default ConnectionsSection;
