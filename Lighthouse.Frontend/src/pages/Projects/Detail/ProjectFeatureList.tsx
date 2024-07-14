import React from "react";
import { Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from "@mui/material";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import { Project } from "../../../models/Project";
import { Link } from "react-router-dom";
import ForecastLikelihood from "../../../components/Common/Forecasts/ForecastLikelihood";

interface ProjectFeatureListProps {
    project: Project
}

const ProjectFeatureList: React.FC<ProjectFeatureListProps> = ({ project }) => {
    return (
        <TableContainer component={Paper}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>
                            <Typography variant="h6" component="div">Feature Name</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Remaining Work</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Forecasts</Typography>
                        </TableCell>
                        {project.milestones.map((milestone) => (
                            <TableCell key={milestone.id}>
                                <Typography variant="h6" component="div">{milestone.name}</Typography>
                            </TableCell>
                        ))}
                        <TableCell>
                            <Typography variant="h6" component="div">Updated On</Typography>
                        </TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {project?.features.map((feature) => (
                        <TableRow key={feature.id}>
                            <TableCell>{feature.name}</TableCell>
                            <TableCell>
                                <div><strong>Total: {feature.getAllRemainingWork()}</strong></div>
                                {project.involvedTeams
                                    .filter(team => feature.getRemainingWorkForTeam(team.id) > 0)
                                    .map((team) => (
                                        <div key={team.id}>
                                            <Link to={`/teams/${team.id}`}>
                                                {`${team.name}: ${feature.getRemainingWorkForTeam(team.id)}`}
                                            </Link>
                                        </div>
                                    ))}
                            </TableCell>
                            <TableCell>
                                <ForecastInfoList title={''} forecasts={feature.forecasts} />
                            </TableCell>
                            {project.milestones.map((milestone) => (
                                <TableCell key={milestone.id}>
                                    <ForecastLikelihood
                                        remainingItems={feature.getAllRemainingWork()}
                                        targetDate={milestone.date}
                                        likelihood={feature.getMilestoneLikelihood(milestone.id)}
                                        showText={false}
                                    />
                                </TableCell>
                            ))}
                            <TableCell>
                                <LocalDateTimeDisplay utcDate={feature.lastUpdated} showTime={true} />
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    )
}

export default ProjectFeatureList;