import React from 'react';
import { Link } from 'react-router-dom';
import { TextField } from '@mui/material';
import Grid from '@mui/material/Grid2'
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import { ITeamSettings } from '../../../models/Team/TeamSettings';

interface InvolvedTeamsListProps {
    teams: ITeamSettings[];
    initiallyExpanded?: boolean;
    onTeamUpdated: (updatedTeam: ITeamSettings) => Promise<void>;
}

const InvolvedTeamsList: React.FC<InvolvedTeamsListProps> = ({ teams, onTeamUpdated, initiallyExpanded = false }) => {
    if (teams.length === 0) {
        return null;
    }

    const onTeamFeatureWIPUpdated = async (teamSetting: ITeamSettings, newFeatureWip: number) => {
        teamSetting.featureWIP = newFeatureWip;
        await onTeamUpdated(teamSetting);
    }

    return (
        <InputGroup title={'Involved Teams (Feature WIP)'} initiallyExpanded={initiallyExpanded}>
            <Grid container spacing={2}>
                {teams.map((team) => (
                    <Grid  size={{ xs: 3}} key={team.id}>
                        <TextField
                            variant='outlined'
                            margin='normal'
                            label={<Link to={`/teams/${team.id}`}>{team.name}</Link>}
                            type='number'
                            onChange={(e) => onTeamFeatureWIPUpdated(team, parseInt(e.target.value, 10))}
                            defaultValue={team.featureWIP}
                            slotProps={{
                                htmlInput: {
                                    min: 1
                                }
                            }}
                        />
                    </Grid>
                ))}
            </Grid>
        </InputGroup>
    );
}

export default InvolvedTeamsList;
