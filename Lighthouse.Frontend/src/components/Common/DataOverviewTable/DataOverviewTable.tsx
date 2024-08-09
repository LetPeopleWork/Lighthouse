import React, { useState } from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton, Tooltip, Typography, Container, Button } from '@mui/material';

import { Link, useNavigate } from 'react-router-dom';
import { IFeatureOwner } from '../../../models/IFeatureOwner';

import InfoIcon from '@mui/icons-material/Info';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import FilterBar from '../FilterBar/FilterBar';
import ProgressIndicator from '../ProgressIndicator/ProgressIndicator';

const iconColor = 'rgba(48, 87, 78, 1)';

interface DataOverviewTableProps<IFeatureOwner> {
    data: IFeatureOwner[];
    api: string;
    onDelete: (item: IFeatureOwner) => void;
}

const DataOverviewTable: React.FC<DataOverviewTableProps<IFeatureOwner>> = ({ data, api, onDelete }) => {
    const [filterText, setFilterText] = useState('');
    const navigate = useNavigate();

    const filteredData = data.filter(item =>
        isMatchingFilterText(item.name)
    );

    function isMatchingFilterText(textToCheck: string) {
        if (!filterText) {
            return true;
        }

        return textToCheck.toLowerCase().includes(filterText.toLowerCase());
    }

    const handleRedirect = () => {
        navigate(`new`);
    };

    return (
        <Container>
            <Button sx={{ marginTop: 2, marginBottom: 2 }} variant="contained" color="primary" onClick={handleRedirect}>Add New</Button>
            <FilterBar filterText={filterText} onFilterTextChange={setFilterText} data-testid="filter-bar" />
            {filteredData.length === 0 ? (
                <Typography variant="h6" align="center" data-testid="no-items-message">
                    No items found matching the filter.
                </Typography>
            ) : (
                <TableContainer component={Paper} data-testid="table-container">
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>
                                    <Typography variant="h6" component="div">Name</Typography>
                                </TableCell>
                                <TableCell>
                                    <Typography variant="h6" component="div">Progress</Typography>
                                </TableCell>
                                <TableCell>
                                    <Typography variant="h6" component="div">Features</Typography>
                                </TableCell>
                                <TableCell></TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {filteredData.map((item: IFeatureOwner) => (
                                <TableRow key={item.id} data-testid={`table-row-${item.id}`}>
                                    <TableCell>
                                        <Link to={`/${api}/${item.id}`} style={{ textDecoration: 'none', color: iconColor }}>
                                            <Typography variant="body1" component="span" style={{ fontWeight: 'bold' }}>
                                                {item.name}
                                            </Typography>
                                        </Link>
                                    </TableCell>
                                    <TableCell>
                                        <ProgressIndicator progressableItem={item} />
                                    </TableCell>
                                    <TableCell>{item.remainingFeatures}</TableCell>
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
                </TableContainer>)}
        </Container>
    );
}

export default DataOverviewTable;