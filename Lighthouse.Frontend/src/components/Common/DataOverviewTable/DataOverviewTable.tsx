import React from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton, Tooltip, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import { IData } from '../../../models/IData';

import InfoIcon from '@mui/icons-material/Info';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';

const iconColor = 'rgba(48, 87, 78, 1)';

interface DataOverviewTableProps<IData> {
  data: IData[];
  api: string;
  onDelete: (item: IData) => void;
}

const DataOverviewTable: React.FC<DataOverviewTableProps<IData>> = ({ data, api, onDelete }) => {
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>
              <Typography variant="h6" component="div">Name</Typography>
            </TableCell>
            <TableCell>
              <Typography variant="h6" component="div">Remaining Work</Typography>
            </TableCell>
            <TableCell>
              <Typography variant="h6" component="div">Features</Typography>
            </TableCell>            
            <TableCell></TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.map((item: IData) => (
            <TableRow key={item.id}>
              <TableCell>
                <Link to={`/${api}/${item.id}`} style={{ textDecoration: 'none', color: iconColor }}>
                  <Typography variant="body1" component="span" style={{ fontWeight: 'bold' }}>
                    {item.name}
                  </Typography>
                </Link>
              </TableCell>
              <TableCell>{item.remainingWork}</TableCell>
              <TableCell>
                <Tooltip title="Details">
                  <IconButton component={Link} to={`/${api}/${item.id}`} style={{ color: iconColor }}>
                    <InfoIcon />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Edit">
                  <IconButton component={Link} to={`/${api}/edit/${item.id}`} style={{ color: iconColor }}>
                    <EditIcon />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Delete">
                  <IconButton onClick={() => onDelete(item)} style={{ color: iconColor }}>
                    <DeleteIcon data-testid="delete-item-button" />
                  </IconButton>
                </Tooltip>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export default DataOverviewTable;
