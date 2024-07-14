import React from 'react';
import { Link } from 'react-router-dom';
import { TextField, Typography } from '@mui/material';
import { ITeam } from '../../../models/Team';

interface InvolvedTeamsListProps {
    teams: ITeam[];
}

const InvolvedTeamsList: React.FC<InvolvedTeamsListProps> = ({ teams }) => {
    if (teams.length === 0) {
        return null;
    }

    return (
        <>
            <Typography variant='h6'>Feature WIP</Typography>
            {teams.map((team) => (
                <TextField
                    variant='outlined'
                    margin='normal'
                    label={<Link to={`/teams/${team.id}`}>{team.name}</Link>}
                    type='number'
                    disabled
                    defaultValue={team.featureWip}
                />
            ))}
        </>
    );
}

export default InvolvedTeamsList;
