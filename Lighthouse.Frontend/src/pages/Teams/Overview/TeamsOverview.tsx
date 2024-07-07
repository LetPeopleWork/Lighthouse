import React, { useEffect, useState } from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, IconButton, Tooltip, Typography } from '@mui/material';
import { Team } from '../../../models/Team';
import { IApiService } from '../../../services/Api/IApiService';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { Link } from 'react-router-dom';
import InfoIcon from '@mui/icons-material/Info';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import DeleteConfirmationDialog from '../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog';

const iconColor = 'rgba(48, 87, 78, 1)';

const TeamsOverview: React.FC = () => {
  const [teams, setTeams] = useState<Team[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [hasError, setHasError] = useState<boolean>(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null);

  
  const fetchData = async () => {
    try {
      setIsLoading(true);
      const apiService: IApiService = ApiServiceProvider.getApiService();
      const teamData = await apiService.getTeams();
      setTeams(teamData);
      setIsLoading(false);
    } catch (error) {
      console.error('Error fetching project overview data:', error);
      setHasError(true);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleDelete = (team: Team) => {
    setSelectedTeam(team);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirmation = async (confirmed: boolean) => {
    if (confirmed && selectedTeam) {
      try {
        const apiService: IApiService = ApiServiceProvider.getApiService();
        setIsLoading(true);

        await apiService.deleteTeam(selectedTeam.id);        
        await fetchData();

      } catch (error) {
        console.error('Error deleting team:', error);
        setHasError(true);
      }
    }

    setDeleteDialogOpen(false);
    setSelectedTeam(null);
  };

  return (
    <LoadingAnimation hasError={hasError} isLoading={isLoading}>
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>
                <Typography variant="h6" component="div">Team Name</Typography>
              </TableCell>
              <TableCell>
                <Typography variant="h6" component="div">Remaining Work</Typography>
              </TableCell>
              <TableCell>
                <Typography variant="h6" component="div">Features</Typography>
              </TableCell>
              <TableCell>
                <Typography variant="h6" component="div">Projects</Typography>
              </TableCell>
              <TableCell></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {teams.map((team) => (
              <TableRow key={team.id}>
                <TableCell>
                  <Link to={`/teams/${team.id}`} style={{ textDecoration: 'none', color: iconColor }}>
                    <Typography variant="body1" component="span" style={{ fontWeight: 'bold' }}>
                      {team.name}
                    </Typography>
                  </Link>
                </TableCell>
                <TableCell>{team.remainingWork}</TableCell>
                <TableCell>{team.features}</TableCell>
                <TableCell>{team.projects}</TableCell>
                <TableCell>
                  <Tooltip title="Details">
                    <IconButton component={Link} to={`/teams/${team.id}`} style={{ color: iconColor }}>
                      <InfoIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Edit">
                    <IconButton component={Link} to={`/teams/edit/${team.id}`} style={{ color: iconColor }}>
                      <EditIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Delete">
                    <IconButton onClick={() => handleDelete(team)} style={{ color: iconColor }}>
                      <DeleteIcon data-testid="delete-team-button" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      {selectedTeam && (
        <DeleteConfirmationDialog
          open={deleteDialogOpen}
          itemName={selectedTeam.name}
          onClose={handleDeleteConfirmation}
        />
      )}
    </LoadingAnimation>
  );
};

export default TeamsOverview;
