import React from "react";
import { Team } from "../../models/Team";

interface TeamLinkProps {
    team: Team
}

const TeamLink: React.FC<TeamLinkProps> = ({ team }) => {

    return (
        <div key={team.id}>
            <a href={`/teams/${team.id}`}>{team.name}</a>
        </div>
    );

};

export default TeamLink;