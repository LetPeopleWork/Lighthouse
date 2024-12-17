import React, { useState } from 'react';
import { Checkbox, FormControlLabel, TextField } from '@mui/material';
import { ITeam } from '../../../models/Team/Team';
import InputGroup from '../InputGroup/InputGroup';
import Grid from '@mui/material/Grid2';

interface TeamsListProps {
    allTeams: ITeam[];
    selectedTeams: number[];
    onSelectionChange: (selectedTeamIds: number[]) => void;
}

const TeamsList: React.FC<TeamsListProps> = ({ allTeams, selectedTeams, onSelectionChange }) => {
    const [searchTerm, setSearchTerm] = useState('');

    const filteredTeams = allTeams.filter(team =>
        team.name.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const handleCheckboxChange = (teamId: number) => {
        if (selectedTeams.includes(teamId)) {
            onSelectionChange(selectedTeams.filter(id => id !== teamId));
        } else {
            onSelectionChange([...selectedTeams, teamId]);
        }
    };

    return (
        <InputGroup title={'Involved Teams'}>
            <TextField
                label="Search Teams"
                variant="outlined"
                fullWidth
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                margin="normal"
            />
            <Grid container spacing={2} sx={{ maxHeight: 300, overflowY: 'auto' }}>
                {filteredTeams.map(team => (
                    <Grid size={{ xs: 12, sm: 6, md: 4 }} key={team.id}>
                        <FormControlLabel
                            control={
                                <Checkbox
                                    checked={selectedTeams.includes(team.id)}
                                    onChange={() => handleCheckboxChange(team.id)}
                                />
                            }
                            label={team.name}
                        />
                    </Grid>
                ))}
            </Grid>
        </InputGroup>
    );
};

export default TeamsList;