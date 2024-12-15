import React from "react";
import { Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from "@mui/material";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import { Project } from "../../../models/Project/Project";
import { Link } from "react-router-dom";
import ForecastLikelihood from "../../../components/Common/Forecasts/ForecastLikelihood";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";

interface ProjectFeatureListProps {
    project: Project
}

const ProjectFeatureList: React.FC<ProjectFeatureListProps> = ({ project }) => {
    const currentOrFutureMilestones = project.milestones.filter(
        (milestone) => {
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            
            const milestoneDate = new Date(milestone.date);
            milestoneDate.setHours(0, 0, 0, 0);
    
            return milestoneDate >= today;
        }
    );   

    return (
        <TableContainer component={Paper}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>
                            <Typography variant="h6" component="div">Feature Name</Typography>
                        </TableCell>
                        <TableCell sx={{ width: '25%' }}>
                            <Typography variant="h6" component="div">Progress</Typography>
                        </TableCell>
                        <TableCell>
                            <Typography variant="h6" component="div">Forecasts</Typography>
                        </TableCell>
                        {currentOrFutureMilestones.map((milestone) => (
                            <TableCell key={milestone.id}>
                                <Typography variant="h6" component="div">{milestone.name} (<LocalDateTimeDisplay utcDate={milestone.date} />)</Typography>
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
                            <TableCell>
                                <FeatureName
                                    name={feature.name}
                                    url={feature.url ?? ""}
                                    isUsingDefaultFeatureSize={feature.isUsingDefaultFeatureSize}
                                    teamsWorkIngOnFeature={project.involvedTeams.filter(team =>
                                        team.featuresInProgress.includes(feature.featureReference)
                                    )}
                                />
                            </TableCell>
                            <TableCell>
                                <ProgressIndicator title="Overall Progress" progressableItem={{
                                    remainingWork: feature.getRemainingWorkForFeature(),
                                    totalWork: feature.getTotalWorkForFeature()
                                }} />

                                {project.involvedTeams
                                    .filter(team => feature.getTotalWorkForTeam(team.id) > 0)
                                    .map((team) => (
                                        <div key={team.id}>
                                            <ProgressIndicator title={
                                                <Link to={`/teams/${team.id}`}>
                                                    {`${team.name}`}
                                                </Link>
                                            } progressableItem={{
                                                remainingWork: feature.getRemainingWorkForTeam(team.id),
                                                totalWork: feature.getTotalWorkForTeam(team.id)
                                            }} />
                                        </div>
                                    ))}
                            </TableCell>
                            <TableCell>
                                <ForecastInfoList title={''} forecasts={feature.forecasts} />
                            </TableCell>
                            {currentOrFutureMilestones.map((milestone) => (
                                <TableCell key={milestone.id}>
                                    <ForecastLikelihood
                                        remainingItems={feature.getRemainingWorkForFeature()}
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
    );
};

export default ProjectFeatureList;