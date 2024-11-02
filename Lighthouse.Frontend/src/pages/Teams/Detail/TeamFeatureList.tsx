import React from "react";
import { Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from "@mui/material";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import { Team } from "../../../models/Team/Team";
import { Link } from "react-router-dom";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";

interface FeatureListProps {
    team: Team
}

const TeamFeatureList: React.FC<FeatureListProps> = ({ team }) => {
    return (
        <TableContainer component={Paper}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell sx={{ width: '15%' }}>
                            <Typography variant="h6" component="div">Feature Name</Typography>
                        </TableCell>
                        <TableCell sx={{ width: '30%' }}>
                            <Typography variant="h6" component="div">Progress</Typography>
                        </TableCell>
                        <TableCell sx={{ width: '20%' }}>
                            <Typography variant="h6" component="div">Forecasts</Typography>
                        </TableCell>
                        <TableCell sx={{ width: '15%' }}>
                            <Typography variant="h6" component="div">Projects</Typography>
                        </TableCell>
                        <TableCell sx={{ width: '15%' }}>
                            <Typography variant="h6" component="div">Updated On</Typography>
                        </TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {team?.features.map((feature) => (
                        <TableRow key={feature.id}>
                            <TableCell>
                                <FeatureName
                                    name={feature.name}
                                    url={feature.url ?? ""}
                                    isUsingDefaultFeatureSize={feature.isUsingDefaultFeatureSize}
                                    teamsWorkIngOnFeature={team.featuresInProgress.includes(feature.featureReference) ? [team] : []}
                                />
                            </TableCell>
                            <TableCell>
                                <ProgressIndicator title="Total" progressableItem={{
                                    remainingWork: feature.getRemainingWorkForFeature(),
                                    totalWork: feature.getTotalWorkForFeature()
                                }} />

                                <ProgressIndicator title={team.name} progressableItem={{
                                    remainingWork: feature.getRemainingWorkForTeam(team.id),
                                    totalWork: feature.getTotalWorkForTeam(team.id)
                                }} />
                            </TableCell>
                            <TableCell>
                                <ForecastInfoList title={''} forecasts={feature.forecasts} />
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

export default TeamFeatureList;