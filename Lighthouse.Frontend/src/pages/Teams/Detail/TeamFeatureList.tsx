import React from "react";
import { Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from "@mui/material";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import { Team } from "../../../models/Team";
import { Link } from "react-router-dom";

interface FeatureListProps {
    team: Team
}

const TeamFeatureList: React.FC<FeatureListProps> = ({ team }) => {
    return (
        <TableContainer component={Paper}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>
                            <Typography variant="h6" component="div">Feature Name</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Remaining Work (Team / Total)</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Forecasts</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Updated On</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Project</Typography>
                        </TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {team?.features.map((feature) => (
                        <TableRow key={feature.id}>
                            <TableCell>{feature.name}</TableCell>
                            <TableCell>
                                {feature.getRemainingWorkForTeam(team.id)} / {feature.getAllRemainingWork()}
                            </TableCell>
                            <TableCell>
                                <ForecastInfoList title={''} forecasts={feature.forecasts} />
                            </TableCell>
                            <TableCell>
                                <LocalDateTimeDisplay utcDate={feature.lastUpdated} showTime={true} />
                            </TableCell>
                            <TableCell>
                                {Object.entries(feature.projects).map(([projectId, projectName]) => (
                                    <div key={projectId}>
                                        <Link to={`/projects/${projectId}`}>
                                            {projectName}
                                        </Link>
                                    </div>
                                ))}
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    )
}

export default TeamFeatureList;