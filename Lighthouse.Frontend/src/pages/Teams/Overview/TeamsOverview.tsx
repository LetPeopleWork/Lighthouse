import React, { useEffect, useState } from 'react';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import DataOverviewTable from '../../../components/Common/DataOverviewTable/DataOverviewTable';
import DeleteConfirmationDialog from '../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog';
import { Team } from '../../../models/Team';
import { IApiService } from '../../../services/Api/IApiService';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IFeatureOwner } from '../../../models/IFeatureOwner';

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
      console.error('Error fetching team data:', error);
      setHasError(true);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleDelete = (team: IFeatureOwner) => {
    setSelectedTeam(team as Team);
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
      <DataOverviewTable
        data={teams}
        api="teams"        
        onDelete={handleDelete}
      />
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
