import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
	IconButton,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
} from "@mui/material";
import type React from "react";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface ConnectionDetailTableProps {
	workTrackingSystemConnections: IWorkTrackingSystemConnection[];
	onEditConnectionButtonClicked: (
		system: IWorkTrackingSystemConnection,
	) => void;
	handleDeleteConnection: (system: IWorkTrackingSystemConnection) => void;
}

const ConnectionDetailTable: React.FC<ConnectionDetailTableProps> = ({
	workTrackingSystemConnections,
	onEditConnectionButtonClicked,
	handleDeleteConnection,
}) => {
	return (
		<TableContainer>
			<Table>
				<TableHead>
					<TableRow>
						<TableCell>Name</TableCell>
						<TableCell>Actions</TableCell>
					</TableRow>
				</TableHead>
				<TableBody>
					{workTrackingSystemConnections.map((system) => (
						<TableRow key={system.id}>
							<TableCell>{system.name}</TableCell>
							<TableCell>
								<IconButton
									onClick={() => onEditConnectionButtonClicked(system)}
								>
									<EditIcon />
								</IconButton>
								<IconButton
									onClick={() => handleDeleteConnection(system)}
									disabled={system.dataSourceType === "File"}
								>
									<DeleteIcon />
								</IconButton>
							</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>
		</TableContainer>
	);
};

export default ConnectionDetailTable;
