import React from "react";
import { IProgressable } from "../../../models/IProgressable";
import { LinearProgress, Typography } from "@mui/material";

interface ProgressIndicatorProps {
    title: React.ReactNode;  // Title can now be any React element
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
                    }}
                >
                    {progressableItem.totalWork > 0 ? (`${completionPercentage}% (${completedItems}/${progressableItem.totalWork})`) : ('')}
                </Typography>
            </div>
        </div>
    );
}

export default ProgressIndicator;
