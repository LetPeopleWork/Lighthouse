import React, { useContext, useEffect, useState } from 'react';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import DataOverviewTable from '../../../components/Common/DataOverviewTable/DataOverviewTable';
import DeleteConfirmationDialog from '../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog';
import { Team } from '../../../models/Team/Team';
import { IFeatureOwner } from '../../../models/IFeatureOwner';
import TutorialButton from '../../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import TeamOverviewTutorial from '../../../components/App/LetPeopleWork/Tutorial/Tutorials/TeamOverviewTutorial';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';

const TeamsOverview: React.FC = () => {
  const [teams, setTeams] = useState<Team[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [hasError, setHasError] = useState<boolean>(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null);

  const { teamService } = useContext(ApiServiceContext);

  const fetchData = async () => {
    try {
      setIsLoading(true);      
      const teamData = await teamService.getTeams();
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
        setIsLoading(true);

        await teamService.deleteTeam(selectedTeam.id);
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
      {teams.length === 0 ? (
        <TeamOverviewTutorial />
      ) : (
        <DataOverviewTable
          data={teams}
          api="teams"
          onDelete={handleDelete}
        />)}

      {selectedTeam && (
        <DeleteConfirmationDialog
          open={deleteDialogOpen}
          itemName={selectedTeam.name}
          onClose={handleDeleteConfirmation}
        />
      )}

      <TutorialButton
        tutorialComponent={<TeamOverviewTutorial />}
      />
    </LoadingAnimation>
  );
};

export default TeamsOverview;
