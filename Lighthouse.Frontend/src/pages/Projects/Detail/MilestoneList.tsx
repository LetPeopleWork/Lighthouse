import React from 'react';
import { Typography } from '@mui/material';
import { IMilestone } from '../../../models/Milestone';
import { DatePicker, LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import dayjs from 'dayjs';

interface MilestoneListProps {
    milestones: IMilestone[];
}

const MilestoneList: React.FC<MilestoneListProps> = ({ milestones }) => {
    if (milestones.length === 0) {
        return null;
    }

    return (
        <>
            <Typography variant='h6'>Milestones</Typography>
            {milestones.map((milestone) => (
                <LocalizationProvider key={milestone.id} dateAdapter={AdapterDayjs} >
                    <DatePicker
                        label={`${milestone.name} Target Date`}
                        value={dayjs(milestone.date)}
                        disabled
                    />
                </LocalizationProvider>
            ))}
        </>
    );
}

export default MilestoneList;
