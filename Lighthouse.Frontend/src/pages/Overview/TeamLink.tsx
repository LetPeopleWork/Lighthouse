import Diversity3Icon from "@mui/icons-material/Diversity3";
import type React from "react";
import type { Team } from "../../models/Team/Team";
import StyleCardNavLink from "./StyleCardNavLink";

interface TeamLinkProps {
	team: Team;
}

const TeamLink: React.FC<TeamLinkProps> = ({ team }) => {
	return (
		<StyleCardNavLink
			text={team.name}
			link={`/teams/${team.id}`}
			icon={Diversity3Icon}
		/>
	);
};

export default TeamLink;
