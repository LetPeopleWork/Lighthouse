import React from "react";
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, IconButton } from "@mui/material";
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface ConnectionDetailTableProps {
    workTrackingSystemConnections: IWorkTrackingSystemConnection[];
    onEditConnectionButtonClicked: (system: IWorkTrackingSystemConnection) => void;
    handleDeleteConnection: (system: IWorkTrackingSystemConnection) => void;
}

const ConnectionDetailTable: React.FC<ConnectionDetailTableProps> = ({ workTrackingSystemConnections, onEditConnectionButtonClicked, handleDeleteConnection }) => {
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
                    {workTrackingSystemConnections.map(system => (
                        <TableRow key={system.id}>
                            <TableCell>{system.name}</TableCell>
                            <TableCell>
                                <IconButton onClick={() => onEditConnectionButtonClicked(system)}>
                                    <EditIcon />
                                </IconButton>
                                <IconButton onClick={() => handleDeleteConnection(system)}>
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
