import React from "react";
import { IProgressable } from "../../../models/IProgressable";
import { LinearProgress, Typography, Tooltip, IconButton } from "@mui/material";
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';

interface ProgressIndicatorProps {
    title: React.ReactNode;
    progressableItem: IProgressable;
}

const ProgressIndicator: React.FC<ProgressIndicatorProps> = ({ title, progressableItem }) => {
    const completedItems = progressableItem.totalWork - progressableItem.remainingWork;
    const completionPercentage = parseFloat(((100 / progressableItem.totalWork) * completedItems).toFixed(2));

    return (
        <div>
            {title}
            
            <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
                <LinearProgress
                    variant="determinate"
                    value={completionPercentage}
                    style={{ width: '100%', height: '24px' }}
                />
                
                <Typography
                    variant="body2"
                    style={{
                        position: 'absolute',
                        left: '50%',
                        transform: 'translateX(-50%)',
                        color: '#fff',
                        fontWeight: 'bold',
                        fontSize: '0.75rem', 
                        whiteSpace: 'nowrap',
                        display: 'flex',
                        alignItems: 'center',
                    }}
                >
                    {progressableItem.totalWork > 0 ? (
                        `${completionPercentage}% (${completedItems}/${progressableItem.totalWork})`
                    ) : (
                        <>
                            Coult not determine Work
                            <Tooltip title="The remaining and total work could not be determined. This can happen if the work was added to a team in your work tracking system, but you have not defined this team yet in Lighthouse." arrow>
                                <IconButton
                                    size="small"
                                    style={{
                                        marginLeft: '4px',
                                        padding: '0px',
                                        color: '#fff',
                                    }}
                                >
                                    <InfoOutlinedIcon fontSize="small" />
                                </IconButton>
                            </Tooltip>
                        </>
                    )}
                </Typography>
            </div>
        </div>
    );
}

export default ProgressIndicator;
