import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import CircleOutlinedIcon from "@mui/icons-material/CircleOutlined";
import ErrorIcon from "@mui/icons-material/Error";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { useNavigate } from "react-router";
import type { IConnectionHealth } from "../../../../services/Api/ConnectionHealthService";
import {
	CONNECTION_STATE_DRAWING,
	CONNECTION_STATE_WORDING,
	isBroken,
} from "./connectionHealthWording";

/**
 * Four connections used to be four sentences to read and compare. Drawn, they are one glance.
 *
 * The three drawings differ in shape and fill, not only in colour, so a reader who gets nothing from
 * green-versus-grey can still tell a ring from a tick. An outlined ring for a connection nobody has
 * asked about is the point rather than the decoration: a muted tick there would let an absence of
 * evidence read as a verdict, which is the whole defect this state was introduced to remove.
 *
 * The word the row used to carry is the icon's accessible name and its tooltip, so nothing is lost to
 * a screen reader or to anyone unsure what a drawing means. It is drawn beside the row rather than
 * inside it, because an icon's title counts as part of its parent's text and a state tucked inside the
 * row would still be spelled out there as far as anything reading the row could tell.
 */
const ConnectionState = ({ connection }: { connection: IConnectionHealth }) => {
	const wording = CONNECTION_STATE_WORDING[connection.state];
	const drawing = CONNECTION_STATE_DRAWING[connection.state];

	if (drawing === "confirmed") {
		return (
			<Tooltip title={wording}>
				<CheckCircleIcon
					fontSize="small"
					color="success"
					titleAccess={wording}
				/>
			</Tooltip>
		);
	}

	if (drawing === "alarm") {
		return (
			<Tooltip title={wording}>
				<ErrorIcon fontSize="small" color="error" titleAccess={wording} />
			</Tooltip>
		);
	}

	return (
		<Tooltip title={wording}>
			<CircleOutlinedIcon
				fontSize="small"
				color="disabled"
				titleAccess={wording}
			/>
		</Tooltip>
	);
};

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
		<Box
			key={connection.connectionId}
			data-testid={`connection-health-${connection.connectionId}`}
			sx={{ py: 0.5 }}
		>
			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				<ConnectionState connection={connection} />

				<Typography
					data-testid={`connection-health-row-${connection.connectionId}`}
					variant="body2"
					sx={{ flexGrow: 1 }}
				>
					{connection.connectionName}
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
